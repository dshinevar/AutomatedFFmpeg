﻿using AutoEncodeClient.Communication;
using AutoEncodeClient.Communication.Interfaces;
using AutoEncodeClient.Config;
using AutoEncodeClient.Factories;
using AutoEncodeClient.Models;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels;
using AutoEncodeClient.ViewModels.EncodingJob;
using AutoEncodeClient.ViewModels.EncodingJob.Interfaces;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeClient.ViewModels.SourceFile;
using AutoEncodeClient.ViewModels.SourceFile.Interfaces;
using AutoEncodeClient.Views;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Logger;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using NetMQ;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoEncodeClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private enum StartupStep
    {
        Startup = 0,
        ConfigLoad = -1,
        LoggerInit = -2,
        ViewStartup = -3
    }

    private const string LOG_STARTUP_NAME = "STARTUP";
    private const string LOG_FILENAME = "aeclient.log";
    private ILogger Logger = null;
    private WindsorContainer _container;
    private ICommunicationMessageHandler _communicatorMessageHandler;

    private void AEClient_Startup(object sender, StartupEventArgs e)
    {
#if DEBUG
        if (System.Diagnostics.Debugger.IsAttached)
        {
            System.Threading.Thread.Sleep(10000);
        }
#endif

        StartupStep startupStep = StartupStep.Startup;

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        // Container Standup
        _container = new();
        RegisterContainerComponents(_container);

        // CONFIG LOAD
        startupStep = StartupStep.ConfigLoad;
        try
        {
            using StreamReader reader = new(Lookups.ConfigFileLocation);
            string str = reader.ReadToEnd();
            var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

            ClientConfig clientConfig = deserializer.Deserialize<ClientConfig>(str);
            State.LoadFromConfig(clientConfig);
        }
        catch (Exception ex)
        {
            HelperMethods.DebugLog($"({startupStep}) Error loading config from {Lookups.ConfigFileLocation}{Environment.NewLine}{ex.Message}", LOG_STARTUP_NAME);
            Environment.Exit((int)startupStep);
        }

        HelperMethods.DebugLog("Config file loaded.", LOG_STARTUP_NAME);

        // LOGGER STARTUP
        startupStep = StartupStep.LoggerInit;
        Logger = _container.Resolve<ILogger>();

        string logFileDirectory = State.LoggerSettings.LogFileDirectory;
        if (Logger.Initialize(logFileDirectory, LOG_FILENAME, State.LoggerSettings.MaxFileSizeInBytes, State.LoggerSettings.BackupFileCount) is false)
        {
            // If initialization failed for logger, attempt backup logger location
            if (Logger.Initialize(Lookups.LogBackupFileLocation, LOG_FILENAME, State.LoggerSettings.MaxFileSizeInBytes, State.LoggerSettings.BackupFileCount) is false)
            {
                HelperMethods.DebugLog($"({startupStep}) Failed to initialize logger. Shutting down.", LOG_STARTUP_NAME);
                Environment.Exit((int)startupStep);
            }
        }

        if (Logger.CheckAndDoRollover() is false)
        {
            // If rollover fails, just exit
            HelperMethods.DebugLog($"({startupStep}) Error occurred when checking log file for rollover. Exiting as logging will not function.", LOG_STARTUP_NAME);
            Environment.Exit((int)startupStep);
        }

        Current.Dispatcher.UnhandledException += Dispatcher_UnhandledException;

        // Build and show view
        startupStep = StartupStep.ViewStartup;

        try
        {
            _communicatorMessageHandler = _container.Resolve<ICommunicationMessageHandler>();
            _communicatorMessageHandler.Initialize();

            IAutoEncodeClientViewModel viewModel = _container.Resolve<IAutoEncodeClientViewModel>();
            viewModel.Initialize();

            AutoEncodeClientView view = new(viewModel)
            {
                Title = $"AutoEncodeClient - {Assembly.GetExecutingAssembly().GetName().Version}"
            };
            Application.Current.MainWindow = view;
            view.Show();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"({startupStep}) Failed to startup application / show view.", Lookups.LoggerThreadName);
            Application.Current.Shutdown((int)startupStep);
        }
    }

    private void AEClient_Exit(object sender, ExitEventArgs e)
    {
        try
        {
            _container.Release(_communicatorMessageHandler);

            NetMQConfig.Cleanup();            
        }
        catch (Exception ex)
        {
            Logger?.LogException(ex, "Error on AutoEncodeClient exit.", Lookups.LoggerThreadName);
            Environment.Exit(-99);
        }
    }

    private static void RegisterContainerComponents(WindsorContainer container)
    {
        // Top Level Setup
        container.Install(FromAssembly.This());

        // Register Container to be accessed later
        container.Register(Component.For<IWindsorContainer>()
            .Instance(container));

        // Register Components

        // Factories
        container.AddFacility<TypedFactoryFacility>()
            .Register(Component.For<ISourceFileFactory>().AsFactory())
            .Register(Component.For<IEncodingJobClientModelFactory>().AsFactory());

        container.Register(Component.For<ILogger>()
            .ImplementedBy<Logger>()
            .LifestyleSingleton());

        container.Register(Component.For<ICommunicationMessageHandler>()
            .ImplementedBy<CommunicationMessageHandler>()
            .LifestyleSingleton());

        container.Register(Component.For<IClientUpdateSubscriber>()
            .ImplementedBy<ClientUpdateSubscriber>()
            .LifestyleTransient()
            .OnCreate(sub => sub.Initialize()));

        container.Register(Component.For<IAutoEncodeClientViewModel>()
            .ImplementedBy<AutoEncodeClientViewModel>()
            .LifestyleSingleton());

        // Source File
        container.Register(Component.For<ISourceFileClientModel>()
            .ImplementedBy<SourceFileClientModel>()
            .LifestyleTransient());

        container.Register(Component.For<ISourceFileViewModel>()
            .ImplementedBy<SourceFileViewModel>()
            .LifestyleTransient());

        container.Register(Component.For<ISourceFilesSubdirectoryViewModel>()
            .ImplementedBy<SourceFilesSubdirectoryViewModel>()
            .LifestyleTransient());

        container.Register(Component.For<ISourceFilesDirectoryViewModel>()
            .ImplementedBy<SourceFilesDirectoryViewModel>()
            .LifestyleTransient());

        container.Register(Component.For<ISourceFilesViewModel>()
            .ImplementedBy<SourceFilesViewModel>()
            .LifestyleSingleton());

        // Encoding Job
        container.Register(Component.For<IEncodingJobClientModel>()
            .ImplementedBy<EncodingJobClientModel>()
            .LifestyleTransient()
            .OnCreate(model => model.Initialize()));

        container.Register(Component.For<IEncodingJobViewModel>()
            .ImplementedBy<EncodingJobViewModel>()
            .LifestyleTransient());

        container.Register(Component.For<IEncodingJobQueueViewModel>()
            .ImplementedBy<EncodingJobQueueViewModel>()
            .LifestyleSingleton());
    }

    private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        => Logger?.LogException(e.Exception, "Unhandled Dispatcher Exception");

}
