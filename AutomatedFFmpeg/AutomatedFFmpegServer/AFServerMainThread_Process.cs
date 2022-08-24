﻿using AutomatedFFmpegServer.ServerSocket;
using AutomatedFFmpegServer.WorkerThreads;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomatedFFmpegServer
{
    public partial class AFServerMainThread
    {
        #region Add Task Functions
        /// <summary>Adds task to task queue.</summary>
        /// <param name="task">Action</param>
        private void AddTask(Action task) => TaskQueue.Enqueue(task);
        /// <summary>Adds ProcessMessage task to Task Queue (Client to Server Message).</summary>
        /// <param name="msg">AFMessageBase</param>
        public void AddProcessMessage(AFMessageBase msg) => AddTask(() => ProcessMessage(msg));
        public void AddSendClientConnectData() => AddTask(() => SendClientConnectData());
        /// <summary>Adds SendMessage task to Task Queue (Server To Client Message). </summary>
        /// <param name="msg">AFMessageBase</param>
        public void AddSendMessage(AFMessageBase msg) => AddTask(() => SendMessage(msg));
        #endregion Add Functions

        #region SendMessage Functions
        /// <summary>Send message to client.</summary>
        /// <param name="msg"></param>
        private void SendMessage(AFMessageBase msg) => ServerSocket.Send(msg);

        private void SendClientConnectData()
        {
            ClientConnectData clientConnect = new ClientConnectData()
            {
                VideoSourceFiles = EncodingJobFinderThread.GetMovieSourceFiles(),
                ShowSourceFiles = EncodingJobFinderThread.GetShowSourceFiles()
            };
            SendMessage(ServerToClientMessageFactory.CreateClientConnectMessage(clientConnect));
        }
        #endregion SendMessage Functions

        #region Process Functions
        /// <summary>Process received message from client. </summary>
        /// <param name="msg"></param>
        private void ProcessMessage(AFMessageBase msg)
        {
            switch (msg.MessageType)
            {
                case AFMessageType.CLIENT_REQUEST:
                {
                    SendClientConnectData();
                    break;
                }
            }
        }

        /// <summary>Server timer task: Send update to client; Spin up threads for other tasks</summary>
        private void OnServerTimerElapsed(object obj)
        {
            // TODO: Handle Cancelling

            if (EncodingJobQueue.Any())
            {
                // Check if task is done (or null -- first time setup)
                if (EncodingJobBuilderTask?.IsCompletedSuccessfully ?? true)
                {
                    EncodingJob jobToBuild = EncodingJobQueue.GetNextEncodingJobWithStatus(EncodingJobStatus.NEW);
                    if (jobToBuild is not null)
                    {
                        EncodingJobBuilderTask = Task.Factory.StartNew(()
                            => EncodingJobTaskFactory.BuildEncodingJob(jobToBuild, Config.ServerSettings.FFmpegDirectory, Config.ServerSettings.HDR10PlusExtractorFullPath, Config.ServerSettings.DolbyVisionExtractorFullPath, Logger, EncodingJobBuilderCancellationToken.Token), EncodingJobBuilderCancellationToken.Token);
                    }
                }

                // Check if task is done (or null -- first time setup)
                if (EncodingTask?.IsCompletedSuccessfully ?? true)
                {
                    EncodingJob jobToEncode = EncodingJobQueue.GetNextEncodingJobWithStatus(EncodingJobStatus.BUILT);
                    if (jobToEncode is not null)
                    {
                        EncodingTask = Task.Factory.StartNew(()
                            => EncodingJobTaskFactory.Encode(jobToEncode, Config.ServerSettings.FFmpegDirectory, Logger, EncodingCancellationToken.Token), EncodingJobBuilderCancellationToken.Token);
                    }
                }

                if (EncodingJobPostProcessingTask?.IsCompletedSuccessfully ?? true)
                {
                    EncodingJob jobToPostProcess = EncodingJobQueue.GetNextEncodingJobForPostProcessing();
                    if (jobToPostProcess is not null)
                    {
                        EncodingJobPostProcessingTask = Task.Factory.StartNew(()
                            => EncodingJobTaskFactory.EncodingJobPostProcessing(jobToPostProcess, Logger, EncodingJobPostProcessingCancellationToken.Token), EncodingJobPostProcessingCancellationToken.Token);
                    }
                }

                ClearCompletedJobs();
                ClearErroredJobs();
            }

            if (ServerSocket?.IsConnected() ?? false) SendMessage(ServerToClientMessageFactory.CreateClientUpdateMessage(new ClientUpdateData()));

            Logger?.CheckAndDoRollover();
        }

        /// <summary> Task Timer: Checks, dequeues, and invokes tasks. </summary>
        /// <param name="obj">Task Queue</param>
        private void OnTaskTimerElapsed(object obj)
        {
            TaskQueue.TryDequeue(out Action task);
            task?.Invoke();
        }

        private void ClearCompletedJobs()
        {
            var jobsRemoved = EncodingJobQueue.ClearCompletedJobs(Config.GlobalJobSettings.HoursCompletedUntilRemoval);
            if (jobsRemoved?.Any() ?? false)
            {
                List<string> jobsRemovedLog = new() { "Completed Jobs Removed" };
                jobsRemovedLog.AddRange(jobsRemoved);
                Logger.LogInfo(jobsRemovedLog);
            }
        }

        private void ClearErroredJobs()
        {
            var jobsRemoved = EncodingJobQueue.ClearErroredJobs(Config.GlobalJobSettings.HoursErroredUntilRemoval);
            if (jobsRemoved?.Any() ?? false)
            {
                List<string> jobsRemovedLog = new() { "Errored Jobs Removed" };
                jobsRemovedLog.AddRange(jobsRemoved);
                Logger.LogInfo(jobsRemovedLog);
            }
        }
        #endregion Process Functions
    }
}
