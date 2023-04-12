﻿using AutoEncodeClient.ApiClient;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeClient.Models;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Collections;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using AutoEncodeClient.Command;

namespace AutoEncodeClient.ViewModels
{
    public class AutoEncodeClientViewModel : 
        ViewModelBase<AutoEncodeClientModel>,
        IAutoEncodeClientViewModel
    {
        private Timer EncodingJobQueueStateTimer { get; set; }

        public AutoEncodeClientViewModel(AutoEncodeClientModel model)
            : base(model) 
        {
            AECommand refreshSourceFilesCommand = new(RefreshSourceFiles);
            RefreshSourceFilesCommand = refreshSourceFilesCommand;

            EncodingJobQueueStateTimer = new Timer(3000)
            {
                AutoReset = false
            };
            EncodingJobQueueStateTimer.Elapsed += EncodingJobQueueStateTimerElapsed;
            EncodingJobQueueStateTimer.Start();

            RefreshSourceFiles();
        }

        #region Commands
        public ICommand RefreshSourceFilesCommand { get; }
        #endregion Commands

        public BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; } = new BulkObservableCollection<EncodingJobViewModel>();
        public ObservableDictionary<string, BulkObservableCollection<VideoSourceData>> MovieSourceFiles { get; }
            = new ObservableDictionary<string, BulkObservableCollection<VideoSourceData>>();
        public ObservableDictionary<string, BulkObservableCollection<ShowSourceData>> ShowSourceFiles { get; }
            = new ObservableDictionary<string, BulkObservableCollection<ShowSourceData>>();

        private EncodingJobViewModel _selectedEncodingJobViewModel = null;
        public EncodingJobViewModel SelectedEncodingJobViewModel
        {
            get => _selectedEncodingJobViewModel;
            set => SetAndNotify(_selectedEncodingJobViewModel, value, () => _selectedEncodingJobViewModel = value);
        }

        #region Timer Elapsed
        private void EncodingJobQueueStateTimerElapsed(object src, ElapsedEventArgs e)
        {
            List<EncodingJobData> encodingJobQueue = Model.GetCurrentEncodingJobQueue();

            if (encodingJobQueue != null && encodingJobQueue.Any())
            {
                // Remove jobs no longer in queue first
                IEnumerable<EncodingJobViewModel> viewModelsToRemove = EncodingJobs.Where(x => !encodingJobQueue.Any(y => y.Id == x.Id));

                Application.Current.Dispatcher.BeginInvoke(() => EncodingJobs.RemoveRange(viewModelsToRemove));

                // Update or Create the rest
                foreach (EncodingJobData data in encodingJobQueue)
                {
                    EncodingJobViewModel job = EncodingJobs.SingleOrDefault(x => x.Equals(data));
                    if (job is not null)
                    {
                        job.Update(data);
                        int currentIndex = EncodingJobs.IndexOf(job);
                        int newIndex = encodingJobQueue.IndexOf(data);

                        bool isSelectedViewModel = job.Id == SelectedEncodingJobViewModel?.Id;

                        if (currentIndex != newIndex) 
                        {
                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                EncodingJobs.Move(currentIndex, newIndex);
                                if (isSelectedViewModel is true) SelectedEncodingJobViewModel = job;
                            });
                        }
                    }
                    else
                    {
                        EncodingJobClientModel model = new(data);
                        EncodingJobViewModel viewModel = new(model);
                        Application.Current.Dispatcher.BeginInvoke(() => EncodingJobs.Insert(encodingJobQueue.IndexOf(data), viewModel));
                    }
                }
            }

            EncodingJobQueueStateTimer.Start();
        }
        #endregion Timer Elapsed

        private void RefreshSourceFiles()
        {
            Task.Factory.StartNew(() =>
            {
                Dictionary<string, List<VideoSourceData>> movieSourceData = Model.GetCurrentMovieSourceData();
                Dictionary<string, List<ShowSourceData>> showSourceData = Model.GetCurrentShowSourceData();

                if (movieSourceData != null) 
                {
                    var converted = new Dictionary<string, BulkObservableCollection<VideoSourceData>>(movieSourceData
                                            .ToDictionary(x => x.Key, x => new BulkObservableCollection<VideoSourceData>(x.Value)));
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        MovieSourceFiles.Refresh(converted);
                    });
                }

                if (showSourceData != null) 
                {
                    var converted = new Dictionary<string, BulkObservableCollection<ShowSourceData>>(showSourceData
                                            .ToDictionary(x => x.Key, x => new BulkObservableCollection<ShowSourceData>(x.Value)));
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        ShowSourceFiles.Refresh(converted);
                    });
                }
            });
        }
    }
}