using System;
using System.Collections.Generic;
using System.Text;

namespace WARE_Testing
{
    public class AudioMetadata
    {
        public byte Flags0 { get; set; } = 0x86;
        public short ArtistNumber { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public string CartNumber { get; set; } = "????";
        public string RawLength { get; set; } = "00000";
        public TimeSpan StartTime { get; set; } = new TimeSpan(0);
        public TimeSpan EndTime { get; set; } = new TimeSpan(0);
        public DateTime StartDate { get; set; } = DateTime.UnixEpoch;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;
        public char AudioType { get; set; } = 'D';
        public int SampleRate { get; set; } = 44100;
        public char StereoMono { get; set; } = 'S';
        public byte CompressionType { get; set; } = 10;
        public int EomStartTenths { get; set; } = 0;
        public short EomLengthHundredths { get; set; } = 0;
        public byte[] Flags1 { get; set; } = { 0x00, 0x01, 0x00, 0x00 }; // Only dayparting enabled
        public int HookStartMs { get; set; } = 0;
        public int HookEomMs { get; set; } = 0;
        public int HookEndMs { get; set; } = 0;
        public byte[] FontColor { get; set; } = { 0, 0, 0, 0 };
        public byte[] BackgroundColor { get; set; } = { 0, 0, 0, 0 };
        public int SegmentEomMs { get; set; } = 0;
        public TimeSpan VtStartTime { get; set; } = new TimeSpan(0);
        public string BeforeCategory { get; set; } = "   ";
        public string BeforeCart { get; set; } = "    ";
        public string AfterCategory { get; set; } = "   ";
        public string AfterCart { get; set; } = "    ";
        public byte[] RawDayparting { get; set; } = Enumerable.Repeat((byte)0xFF, 21).ToArray();
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string IntroSeconds { get; set; } = "00";
        public char EndType { get; set; } = ' ';
        public DateTime ReleaseDate { get; set; } = DateTime.MinValue;
        public DateTime ImportDate { get; set; }
        public short MpegBitrate { get; set; } = 0;
        public ushort PlaybackSpeed { get; set; } = 0;
        public ushort PlaybackLevel { get; set; } = 0;
        public ushort NewPlaybackLevel { get; set; } = 0;
        public uint ChopSize { get; set; } = 0;
        public uint VtEomOvr { get; set; } = 0;
        public uint DesiredLength { get; set; } = 0;

        // Triggers (Source ID + Time)
        public uint Trigger1 { get; set; } = 0;
        public uint Trigger2 { get; set; } = 0;
        public uint Trigger3 { get; set; } = 0;
        public uint Trigger4 { get; set; } = 0;
    }
}
