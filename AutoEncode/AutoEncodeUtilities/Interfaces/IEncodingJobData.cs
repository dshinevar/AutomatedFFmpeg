﻿using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Json;
using Newtonsoft.Json;
using System;

namespace AutoEncodeUtilities.Interfaces
{
    /// <summary>Interface that inidicates what any Encoding Job object should have</summary>
    public interface IEncodingJobData : IEquatable<IEncodingJobData>
    {
        /// <summary>Unique job identifier </summary>
        int Id { get; }
        /// <summary>Name of job (FileName without extension) </summary>
        string Name { get; }
        /// <summary>FileName of Job </summary>
        string FileName { get; }
        /// <summary>Full Path of the job's Source File </summary>
        string SourceFullPath { get; }
        /// <summary>Directory of destination file full path </summary>
        string DestinationDirectory { get; }
        /// <summary>Full Path of the job's expected Destination File </summary>
        string DestinationFullPath { get; }

        #region Status
        EncodingJobStatus Status { get; }
        /// <summary>Flag showing if a job is in error </summary>
        bool Error { get; }
        /// <summary>Error message from when a job was last marked in error. </summary>
        string LastErrorMessage { get; }
        /// <summary> Flag showing if a job is paused </summary>
        bool Paused { get; }
        /// <summary> Flag showing if a job is cancelled </summary>
        bool Cancelled { get; }
        /// <summary>Encoding Progress Percentage </summary>
        int EncodingProgress { get; }
        /// <summary>Amount of time spent encoding. </summary>
        TimeSpan? ElapsedEncodingTime { get; }
        /// <summary> DateTime when encoding was completed </summary>
        DateTime? CompletedEncodingDateTime { get; }
        /// <summary> DateTime when postprocessing was completed </summary>
        DateTime? CompletedPostProcessingTime { get; }
        /// <summary> DateTime when job was errored </summary>
        DateTime? ErrorTime { get; }
        #endregion Status

        #region Processing Data
        /// <summary>The raw stream (video, audio subtitle) data </summary>
        SourceStreamData SourceStreamData { get; }
        /// <summary>Instructions on how to encode job based on the source stream data and rules </summary>
        EncodingInstructions EncodingInstructions { get; }
        /// <summary>Determines if the job needs PostProcessing</summary>
        bool NeedsPostProcessing { get; }
        /// <summary>Marks what PostProcessing functions should be done to this job. </summary>
        PostProcessingFlags PostProcessingFlags { get; }
        /// <summary>Settings for PostProcessing; Initially copied over from AEServerConfig file. </summary>
        PostProcessingSettings PostProcessingSettings { get; }
        /// <summary>Arguments passed to FFmpeg Encoding Job </summary>
        [JsonConverter(typeof(EncodingCommandArgumentsConverter<IEncodingCommandArguments>))]
        IEncodingCommandArguments EncodingCommandArguments { get; }
        #endregion Processing Data
    }
}
