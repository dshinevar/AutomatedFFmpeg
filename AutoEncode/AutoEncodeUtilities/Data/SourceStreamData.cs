﻿using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Data
{
    public class SourceStreamData : IUpdateable<SourceStreamData>
    {
        public int DurationInSeconds { get; set; }
        /// <summary>This is an approx. number; Used for dolby vision jobs</summary>
        public int NumberOfFrames { get; set; }
        public VideoStreamData VideoStream { get; set; }
        public List<AudioStreamData> AudioStreams { get; set; } = new();
        public List<SubtitleStreamData> SubtitleStreams { get; set; }

        public void Update(SourceStreamData data) 
        {
            DurationInSeconds = data.DurationInSeconds;
            NumberOfFrames = data.NumberOfFrames;
        }
    }

    public abstract class StreamData
    {
        public int StreamIndex { get; set; } = -1;
        public string Title { get; set; }
    }

    public class VideoStreamData : 
        StreamData,
        IUpdateable<VideoStreamData>
    {
        public HDRData HDRData { get; set; }
        public bool HasHDR => (!HDRData?.HDRFlags.Equals(HDRFlags.NONE)) ?? false;
        public bool HasDynamicHDR => HasHDR && (HDRData?.IsDynamic ?? false);
        public string CodecName { get; set; }
        public string PixelFormat { get; set; }
        /// <summary> Crop string should be in this format as it allows it to be dropped into the ffmpeg command: XXXX:YYYY:AA:BB </summary>
        public string Crop { get; set; }
        public string Resolution { get; set; }
        public int ResoultionInt { get; set; }
        public string ColorSpace { get; set; }
        public string ColorPrimaries { get; set; }
        public string ColorTransfer { get; set; }
        public string FrameRate { get; set; }
        public bool Animated { get; set; } = false;
        public VideoScanType ScanType { get; set; } = VideoScanType.UNDETERMINED;
        public ChromaLocation? ChromaLocation { get; set; } = null;
        public void Update(VideoStreamData data)
        {
            CodecName = data.CodecName;
            PixelFormat = data.PixelFormat;
            Resolution = data.Resolution;
            FrameRate = data.FrameRate;
            Crop = data.Crop;
            ResoultionInt = data.ResoultionInt;
            ColorSpace = data.ColorSpace;
            ColorPrimaries = data.ColorPrimaries;
            ColorTransfer = data.ColorTransfer;
            Animated = data.Animated;
            ScanType = data.ScanType;
            ChromaLocation = data.ChromaLocation;
            HDRData.Update(data.HDRData);
        }
    }

    public class HDRData : IUpdateable<HDRData>
    {
        public HDRFlags HDRFlags { get; set; } = HDRFlags.NONE;
        public string Red_X { get; set; }
        public string Red_Y { get; set; }
        public string Green_X { get; set; }
        public string Green_Y { get; set; }
        public string Blue_X { get; set; }
        public string Blue_Y { get; set; }
        public string WhitePoint_X { get; set; }
        public string WhitePoint_Y { get; set; }
        public string MinLuminance { get; set; }
        public string MaxLuminance { get; set; }
        public string MaxCLL { get; set; }
        public Dictionary<HDRFlags, string> DynamicMetadataFullPaths { get; set; }
        public bool IsDynamic => HDRFlags.HasFlag(HDRFlags.HDR10PLUS) || HDRFlags.HasFlag(HDRFlags.DOLBY_VISION);
        public void Update(HDRData data) => data.CopyProperties(this);
    }

    public class AudioStreamData : 
        StreamData,
        IUpdateable<AudioStreamData>
    {
        public int AudioIndex { get; set; } = -1;
        public string CodecName { get; set; }
        public string Descriptor { get; set; }
        public int Channels { get; set; }
        public string ChannelLayout { get; set; }
        public string Language { get; set; }
        public bool Commentary { get; set; }
        public void Update(AudioStreamData data) => data.CopyProperties(this);
    }

    public class SubtitleStreamData : StreamData
    {
        public int SubtitleIndex { get; set; } = -1;
        public string Language { get; set; }
        public string Descriptor { get; set; }
        public bool Forced { get; set; }
    }
}
