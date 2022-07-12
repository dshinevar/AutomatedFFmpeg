﻿using System.Threading;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Data;

namespace AutomatedFFmpegServer.Base
{
    /// <summary> Worker thread base class.  Uses a thread loop as the worker threads are expected to have lengthy tasks. </summary>
    public abstract class AFWorkerThreadBase
    {
        private Thread Thread { get; set; }
        private int ThreadSleep { get; set; } = 5000;
        private int ThreadDeepSleep
        {
            get => ThreadSleep * 5;
        }
        protected AFServerMainThread MainThread { get; set; }
        protected AFServerConfig Config { get; set; }
        protected AutoResetEvent SleepARE { get; set; } = new AutoResetEvent(false);
        public string ThreadName { get; set; } = "AFWorkerThread";
        public AFWorkerThreadStatus Status { get; set; } = AFWorkerThreadStatus.PROCESSING;

        /// <summary>Constructor</summary>
        /// <param name="encodingJobs">EncodingJobs handle.</param>
        public AFWorkerThreadBase(string threadName, AFServerMainThread mainThread, AFServerConfig serverConfig)
        {
            ThreadName = threadName;
            MainThread = mainThread;
            Config = serverConfig;
            ThreadSleep = Config.ServerSettings.ThreadSleepInMS;
        }

        /// <summary>Gets the current status of the thread. </summary>
        /// <returns>ThreadStatusData</returns>
        public ThreadStatusData GetThreadStatus() => new ThreadStatusData(ThreadName, Status);

        /// <summary> Starts thread. </summary>
        public virtual void Start(params object[] threadObjects)
        {
            ThreadStart threadStart = () => ThreadLoop(threadObjects);
            Thread = new Thread(threadStart);
            Thread.Start();
        }
        /// <summary> Stops thread (join). </summary>
        public virtual void Stop()
        {
            Wake();
            Thread.Join();
        }

        /// <summary> Wakes up thread by setting the Sleep AutoResetEvent. Not used by default.</summary>
        public void Wake()
        {
            SleepARE.Set();
            Status = AFWorkerThreadStatus.PROCESSING;
        }

        /// <summary> Sleeps thread for certain amount of time. </summary>
        protected virtual void Sleep()
        {
            Status = AFWorkerThreadStatus.SLEEPING;
            SleepARE.WaitOne(ThreadSleep);
        }

        /// <summary> Sleeps thread for 5x length of sleep. </summary>
        protected virtual void DeepSleep()
        {
            Status = AFWorkerThreadStatus.DEEP_SLEEPING;
            SleepARE.WaitOne(ThreadDeepSleep);
        }

        protected abstract void ThreadLoop(object[] objects = null);

    }
}