using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WARE_Testing
{
    public class ScotChunk
    {

        public static byte[] Searialize(AudioMetadata data)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write("scot    ".ToCharArray());
                writer.Write((byte)0);                                                              // 1 byte, scratchpad
                writer.Write(data.Flags0);                                                          // 1 byte, flags
                writer.Write(data.ArtistNumber);                                                    // 2 bytes, artist num
                writer.Write(WaveUtils.FixedString(data.Title, 43, (byte)' '));                                 // 43 bytes, title
                writer.Write(WaveUtils.FixedString(data.CartNumber, 4, 0x00));                                // 4 bytes, cart number
                writer.Write((byte)' ');                                                            // 1 byte, cart padding, skip
                writer.Write(WaveUtils.FixedString(data.RawLength, 5, (byte)' '));                            // 5 bytes, file length in either MM:SS or HMMSS format based on rawLengthFormat
                writer.Write((short)data.StartTime.TotalSeconds);                                   // 2 bytes, start time seconds
                writer.Write((short)(data.StartTime.Milliseconds / 10));                            // 2 bytes, start time hundredths
                writer.Write((short)data.EndTime.TotalSeconds);                                     // 2 bytes, end time seconds
                writer.Write((short)(data.EndTime.Milliseconds / 10));                              // 2 bytes, end time hundredths

                // Write Start and End dates, 6 bytes each
                if (data.StartDate.Equals(DateTime.UnixEpoch))
                {
                    writer.Write("000000".ToCharArray());
                }
                else
                {
                    writer.Write(data.StartDate.ToString("MMddyy").ToCharArray());
                }

                if (data.EndDate.Equals(DateTime.MaxValue))
                {
                    writer.Write("999999".ToCharArray());
                }
                else
                {
                    writer.Write(data.EndDate.ToString("MMddyy").ToCharArray());
                }

                // Write Start and End hours, 1 byte each
                if (data.StartDate.Equals(DateTime.UnixEpoch))
                {
                    writer.Write((byte)128);
                }
                else
                {
                    writer.Write((byte)(data.StartDate.Hour + 128));
                }

                if (data.EndDate.Equals(DateTime.MaxValue))
                {
                    writer.Write((byte)0);
                }
                else
                {
                    writer.Write((byte)(data.EndDate.Hour + 128));
                }

                writer.Write(data.AudioType);                                                       // 1 byte, audio type (e.g. 'A' for analog, 'D' for digital)
                writer.Write((ushort)(data.SampleRate / 100));                                      // 2 bytes, sample rate in Hz (stored as actual sample rate divided by 100)
                writer.Write(data.StereoMono);                                                      // 1 byte, stereo/mono (e.g. 'S' for stereo, 'M' for mono)
                writer.Write(data.CompressionType);                                                 // 1 byte, compression type (refer to documentation for specific values)
                writer.Write(data.EomStartTenths);                                                  // 4 bytes, EOM start time in tenths of a second
                writer.Write(data.EomLengthHundredths);                                             // 2 bytes, EOM length in hundredths of a second
                writer.Write(data.Flags1);                                                          // 4 byte, extended (flags)1 bitfield
                writer.Write(data.HookStartMs);                                                     // 4 bytes, hook start time in milliseconds
                writer.Write(data.HookEomMs);                                                       // 4 bytes, hook EOM time in milliseconds
                writer.Write(data.HookEndMs);                                                       // 4 bytes, hook end time in milliseconds
                writer.Write(data.FontColor);                                                       // 4 bytes, font color RGBA?
                writer.Write(data.BackgroundColor);                                                 // 4 bytes, background color RGBA?
                writer.Write(data.SegmentEomMs);                                                    // 4 bytes, segment EOM time in milliseconds (if file has segments)
                writer.Write((ushort)data.VtStartTime.Seconds);                                     // 2 bytes, VT start time seconds
                writer.Write((ushort)(data.VtStartTime.Milliseconds / 10));                         // 2 bytes, VT start time hundredths
                writer.Write(WaveUtils.FixedString(data.BeforeCategory, 3, (byte)' '));             // 3 bytes, before link category
                writer.Write(WaveUtils.FixedString(data.BeforeCart, 4, (byte)' '));                 // 4 bytes, before link cart number
                writer.Write((byte)' ');                                                            // 1 byte, before link padding, skip
                writer.Write(WaveUtils.FixedString(data.AfterCategory, 3, (byte)' '));              // 3 bytes, after link category
                writer.Write(WaveUtils.FixedString(data.AfterCart, 4, (byte)' '));                  // 4 bytes, after link cart number
                writer.Write((byte)' ');                                                            // 1 byte, after link padding, skip
                writer.Write(data.RawDayparting);                                                   // 21 bytes, dayparting bitfield for 7 days * 24 hours = 168 bits (21 bytes)
                writer.Write(new byte[108]);                                                        // 108 bytes, unused, skip
                writer.Write(WaveUtils.FixedString(data.Artist, 34, (byte)' '));                    // 34 bytes
                writer.Write(WaveUtils.FixedString(data.Album, 34, (byte)' '));                     // 34 bytes
                writer.Write(WaveUtils.FixedString(data.IntroSeconds, 2, (byte)'0'));               // 2 bytes
                writer.Write(data.EndType);                                                         // 1 byte
                writer.Write(WaveUtils.FixedString(data.ReleaseDate.Year.ToString(), 4, (byte)'0'));// 4 bytes
                writer.Write((byte)0);                                                              // 1 byte, skip
                if (data.ImportDate.Equals(DateTime.UnixEpoch))
                {
                    writer.Write((byte)128);                                                        // 1 byte, Hour + 128 offset
                    writer.Write("000000".ToCharArray());                                           // 6 bytes, Import date
                }
                else
                {
                    writer.Write((byte)(data.ImportDate.Hour + 128));                               // 1 byte, Hour + 128 offset
                    writer.Write(data.ImportDate.ToString("MMddyy").ToCharArray());                 // 6 bytes, Import date
                }
                writer.Write(data.MpegBitrate);                                                     // 2 bytes
                writer.Write(data.PlaybackSpeed);                                                   // 2 bytes, raw playback speed
                writer.Write(data.PlaybackLevel);                                                   // 2 bytes, raw playback level
                // TODO: Calculate proper file size and flags instead of overwriting with 0s
                writer.Write(new byte[5]);                                                          // 5 bytes, file size, skip, new size should be calculated (UInt32)
                writer.Write(data.NewPlaybackLevel);                                                // 2 bytes, playback level as percentage of original level x10, highest bit means valid
                writer.Write(data.ChopSize);                                                        // 4 bytes, hundredths of seconds removed from audio middle, highest bt means valid
                writer.Write(data.VtEomOvr);                                                        // 4 bytes, millis to subtract from pre-cut EOM
                writer.Write(data.DesiredLength);                                                   // 4 bytes, desired file length
                writer.Write(data.Trigger1);                                                        // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                writer.Write(data.Trigger2);                                                        // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                writer.Write(data.Trigger3);                                                        // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                writer.Write(data.Trigger4);                                                        // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                writer.Write(new byte[33]);                                                         // 33 bytes to end of header

                long totalChunkSize = writer.BaseStream.Length;
                writer.Seek(4, SeekOrigin.Begin); // Jump back to file size field
                writer.Write((int)(totalChunkSize - 8)); // RIFF size = FileSize - 8

                return ms.ToArray();
            }
        }

        public static void Parse(AudioMetadata fileInfo, BinaryReader reader, FileStream fs, WavChunk scotEntry, bool verbose) 
        {
            // Temporary variables for scott fields
            short startHundredths = 0;
            short startSeconds = 0;
            short endSeconds = 0;
            short endHundredths = 0;
            string rawStartDate = "000000";
            string rawEndDate = "999999";
            int tempStartHour = 0;
            int tempEndHour = 0;
            byte startHour = 0;
            byte endHour = 0;
            short vtStartSeconds = 0;
            short vtStartHundredths = 0;
            byte importHour = 0;
            string rawImportDate = "000000";
            byte[] rawTitle;
            byte[] rawArtist;
            byte[] rawAlbum;




            // Parse the fields based on the expected structure
            fs.Seek(scotEntry.Offset, SeekOrigin.Begin);

            fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, scratchpad, skip
            fileInfo.Flags0 = reader.ReadByte();                                                // 1 byte, flags0 bitfield
            fileInfo.ArtistNumber = reader.ReadInt16();                                         // 2 bytes, artist number

            rawTitle = reader.ReadBytes(43);                                                    // 43 bytes, title
            fileInfo.Title = Encoding.UTF8.GetString(rawTitle).TrimEnd('\0', ' ');              // Process title with UTF-8

            byte[] rawCart = reader.ReadBytes(4);                                               // 4 bytes, cart number
            fileInfo.CartNumber = Encoding.UTF8.GetString(rawCart).TrimEnd('\0', ' ');

            fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, cart padding, skip

            byte[] rawLenBytes = reader.ReadBytes(5);                                           // 5 bytes, file length
            fileInfo.RawLength = Encoding.UTF8.GetString(rawLenBytes).TrimEnd('\0', ' ');

            startSeconds = reader.ReadInt16();                                                  // 2 bytes, start time seconds
            startHundredths = reader.ReadInt16();                                               // 2 bytes, start time hundredths
            endSeconds = reader.ReadInt16();                                                    // 2 bytes, end time seconds
            endHundredths = reader.ReadInt16();                                                 // 2 bytes, end time hundredths

            byte[] rawStartDtBytes = reader.ReadBytes(6);                                       // 6 bytes, start date YYMMDD
            rawStartDate = Encoding.UTF8.GetString(rawStartDtBytes).TrimEnd('\0', ' ');

            byte[] rawEndDtBytes = reader.ReadBytes(6);                                         // 6 bytes, end date YYMMDD
            rawEndDate = Encoding.UTF8.GetString(rawEndDtBytes).TrimEnd('\0', ' ');

            tempStartHour = (reader.ReadByte() - 0x80);                                         // 1 byte, start hour (0x80-0x97)
            startHour = (byte)Math.Max(0, tempStartHour);

            tempEndHour = (reader.ReadByte() - 0x80);                                           // 1 byte, end hour (0x80-0x97)
            endHour = (byte)Math.Max(0, tempEndHour);

            fileInfo.AudioType = (char)reader.ReadByte();                                       // 1 byte, audio type
            fileInfo.SampleRate = reader.ReadInt16() * 100;                                     // 2 bytes, sample rate Hz
            fileInfo.StereoMono = (char)reader.ReadByte();                                      // 1 byte, stereo/mono
            fileInfo.CompressionType = reader.ReadByte();                                       // 1 byte, compression type
            fileInfo.EomStartTenths = reader.ReadInt32();                                       // 4 bytes, EOM start tenths
            fileInfo.EomLengthHundredths = reader.ReadInt16();                                  // 2 bytes, EOM length hundredths
            fileInfo.Flags1 = reader.ReadBytes(4);                                              // 4 byte, extended flags
            fileInfo.HookStartMs = reader.ReadInt32();                                          // 4 bytes, hook start ms
            fileInfo.HookEomMs = reader.ReadInt32();                                            // 4 bytes, hook EOM ms
            fileInfo.HookEndMs = reader.ReadInt32();                                            // 4 bytes, hook end ms
            fileInfo.FontColor = reader.ReadBytes(4);                                           // 4 bytes, font color
            fileInfo.BackgroundColor = reader.ReadBytes(4);                                     // 4 bytes, background color
            fileInfo.SegmentEomMs = reader.ReadInt32();                                         // 4 bytes, segment EOM ms

            vtStartSeconds = reader.ReadInt16();                                                // 2 bytes, VT start seconds
            vtStartHundredths = reader.ReadInt16();                                             // 2 bytes, VT start hundredths

            byte[] rawBeforeCat = reader.ReadBytes(3);                                          // 3 bytes, before link category
            fileInfo.BeforeCategory = Encoding.UTF8.GetString(rawBeforeCat).TrimEnd('\0', ' ');
            byte[] rawBeforeCart = reader.ReadBytes(4);                                         // 4 bytes, before link cart
            fileInfo.BeforeCart = Encoding.UTF8.GetString(rawBeforeCart).TrimEnd('\0', ' ');
            fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, before link padding, skip

            byte[] rawAfterCat = reader.ReadBytes(3);                                           // 3 bytes, after link category
            fileInfo.AfterCategory = Encoding.UTF8.GetString(rawAfterCat).TrimEnd('\0', ' ');
            byte[] rawAfterCart = reader.ReadBytes(4);                                          // 4 bytes, after link cart
            fileInfo.AfterCart = Encoding.UTF8.GetString(rawAfterCart).TrimEnd('\0', ' ');
            fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, after link padding, skip

            fileInfo.RawDayparting = reader.ReadBytes(21);                                      // 21 bytes, dayparting
            fs.Seek(108, SeekOrigin.Current);                                                   // 108 bytes, unused, skip

            rawArtist = reader.ReadBytes(34);                                                   // 34 bytes, artist
            fileInfo.Artist = Encoding.UTF8.GetString(rawArtist).TrimEnd('\0', ' ');

            rawAlbum = reader.ReadBytes(34);                                                    // 34 bytes, album
            fileInfo.Album = Encoding.UTF8.GetString(rawAlbum).TrimEnd('\0', ' ');

            byte[] rawIntro = reader.ReadBytes(2);                                              // 2 bytes, intro seconds
            fileInfo.IntroSeconds = Encoding.UTF8.GetString(rawIntro).TrimEnd('\0', ' ');
            fileInfo.EndType = (char)reader.ReadByte();                                         // 1 byte, end type

            byte[] rawYearBytes = reader.ReadBytes(4);                                          // 4 bytes, release year
            string yearStr = Encoding.UTF8.GetString(rawYearBytes).TrimEnd('\0', ' ').Trim();
            if (int.TryParse(new string(yearStr.Where(char.IsDigit).ToArray()), out int yearVal) && yearVal > 0)
            {
                fileInfo.ReleaseDate = new DateTime(yearVal, 1, 1);
            }
            else
            {
                fileInfo.ReleaseDate = new DateTime(1900, 1, 1);
            }

            fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, skip

            importHour = (byte)(reader.ReadByte() - 0x80);                                      // 1 byte, import hour

            byte[] rawImpDtBytes = reader.ReadBytes(6);                                         // 6 bytes, import date
            rawImportDate = Encoding.UTF8.GetString(rawImpDtBytes).TrimEnd('\0', ' ');

            fileInfo.MpegBitrate = reader.ReadInt16();                                          // 2 bytes, bitrate
            fileInfo.PlaybackSpeed = reader.ReadUInt16();                                       // 2 bytes, playback speed
            fileInfo.PlaybackLevel = reader.ReadUInt16();                                       // 2 bytes, playback level
            fs.Seek(5, SeekOrigin.Current);                                                     // 5 bytes, skip
            fileInfo.NewPlaybackLevel = reader.ReadUInt16();                                    // 2 bytes, new level
            fileInfo.ChopSize = reader.ReadUInt32();                                            // 4 bytes, chop size
            fileInfo.VtEomOvr = reader.ReadUInt32();                                            // 4 bytes, VT EOM override
            fileInfo.DesiredLength = reader.ReadUInt32();                                       // 4 bytes, desired length
            fileInfo.Trigger1 = reader.ReadUInt32();                                            // 4 bytes, trigger 1
            fileInfo.Trigger2 = reader.ReadUInt32();                                            // 4 bytes, trigger 2
            fileInfo.Trigger3 = reader.ReadUInt32();                                            // 4 bytes, trigger 3
            fileInfo.Trigger4 = reader.ReadUInt32();                                            // 4 bytes, trigger 4
            fs.Seek(33, SeekOrigin.Current);                                                    // 33 bytes to end of header




            // Create bit arrays to parse flags
            BitArray flags0Bits = new BitArray(new byte[] { fileInfo.Flags0 });
            BitArray flags1Bits = new BitArray(fileInfo.Flags1);

            // Parse flag0
            bool flags1Enabled = flags0Bits[7];
            bool fileHasSegments = flags1Bits[6];
            // Bit 5 is reserved for future use
            bool isRotationLUT = flags0Bits[4];
            bool isVoiceTrack = flags0Bits[3];
            bool rawLengthFromat = flags0Bits[2];
            bool rawLengthType = flags0Bits[1];
            bool parentOfRotationSet = flags0Bits[0];

            // Parse flag1 (extended)
            bool daypartingEnabled = flags1Bits[8];
            bool isDTMFRecording = flags1Bits[7];
            bool archiveAfterPlay = flags1Bits[6];
            bool deleteAfterPlay = flags1Bits[5];
            bool hasHookModeValues = flags1Bits[4];
            bool hasTriggerValues = flags1Bits[3];
            bool useDesiredLength = flags1Bits[2];
            bool vtEOMOVREnabled = flags1Bits[1];
            bool notPlayOnInternet = flags1Bits[0];

            //Console.WriteLine("Flags0");
            //for (int i = 0; i < 8; i++)
            //{
            //    Console.WriteLine($"Flag {i:D2}:   {(flags0Bits[i] ? "1" : "0")}");
            //}

            //Console.WriteLine("Flags1");
            //for (int i = 0; i < 32; i++)
            //{
            //    Console.WriteLine($"Flag {i:D2}:   {(flags1Bits[i] ? "1" : "0")}");
            //}

            if (verbose)
            {
                // Print Flags:
                Console.WriteLine();
                Console.WriteLine("File Flags:");
                Console.WriteLine($"Extended Flags Enabled:         {(flags1Enabled ? "Yes" : "No")}");
                Console.WriteLine($"File Has Segments:              {(fileHasSegments ? "Yes" : "No")}");
                Console.WriteLine($"Is a Rotation Look Up Table:    {(isRotationLUT ? "Yes" : "No")}");
                Console.WriteLine($"Is a Voice Track:               {(isVoiceTrack ? "Yes" : "No")}");
                Console.WriteLine($"Length Format:                  {(rawLengthFromat ? "HMMSS" : "MM:SS")}");
                Console.WriteLine($"Length Type:                    {(rawLengthType ? "Until EOM" : "Until EOF")}");
                Console.WriteLine($"Parent of Rotation Set:         {(parentOfRotationSet ? "Yes" : "No")}");

                // Print Extended Flags (if enabled)
                if (flags1Enabled)
                {
                    Console.WriteLine();
                    Console.WriteLine("Extended Flags (If Enabled):");
                    Console.WriteLine($"Dayparting Enabled:             {(daypartingEnabled ? "Yes" : "No")}");
                    Console.WriteLine($"Is a DTMF Recording:            {(isDTMFRecording ? "Yes" : "No")}");
                    Console.WriteLine($"Archive file after playing:     {(archiveAfterPlay ? "Yes" : "No")}");
                    Console.WriteLine($"Delete file after playing:      {(deleteAfterPlay ? "Yes" : "No")}");
                    Console.WriteLine($"File Has Hook Mode Values:      {(hasHookModeValues ? "Yes" : "No")}");
                    Console.WriteLine($"File Has Trigger Values:        {(hasTriggerValues ? "Yes" : "No")}");
                    Console.WriteLine($"Use Desired Length:             {(useDesiredLength ? "Yes" : "No")}");
                    Console.WriteLine($"Voice Track EOM Override:       {(vtEOMOVREnabled ? "Yes" : "No")}");
                    Console.WriteLine($"Do Not Play On Internet:        {(notPlayOnInternet ? "Yes" : "No")}");
                }
            }

            // Print Dayparting (if enabled)
            if (flags1Enabled && daypartingEnabled && verbose)
            {
                Console.WriteLine();
                Console.WriteLine("Dayparting Table (If Enabled):");
                DumpDayparting(fileInfo.RawDayparting);
            }

            // Calculate length
            TimeSpan length;
            if (rawLengthFromat)
            {
                //HMMSS
                int hours = int.Parse(fileInfo.RawLength.Substring(0, 1));
                int minutes = int.Parse(fileInfo.RawLength.Substring(1, 2));
                int seconds = int.Parse(fileInfo.RawLength.Substring(3, 2));
                length = new TimeSpan(hours, minutes, seconds);
            }
            else
            {
                //MM:SS
                int minutes = int.Parse(fileInfo.RawLength.Substring(0, 2));
                int seconds = int.Parse(fileInfo.RawLength.Substring(3, 2));
                length = new TimeSpan(0, minutes, seconds);
            }

            // Calculate start and end times in seconds
            fileInfo.StartTime = new TimeSpan(0, 0, 0, startSeconds, startHundredths * 10);
            fileInfo.EndTime = new TimeSpan(0, 0, 0, endSeconds, endHundredths * 10);

            DateTime tempStartDate, tempEndDate, tempImportDate;

            // Calculate start and end dates
            if (!DateTime.TryParseExact(rawStartDate, "MMddyy", null, System.Globalization.DateTimeStyles.None, out tempStartDate))
            {
                fileInfo.StartDate = DateTime.UnixEpoch;
            }
            else
            {
                fileInfo.StartDate = tempStartDate.AddHours(startHour);
            }

            if (!DateTime.TryParseExact(rawEndDate, "MMddyy", null, System.Globalization.DateTimeStyles.None, out tempEndDate))
            {
                fileInfo.EndDate = DateTime.MaxValue;
            }
            else
            {
                fileInfo.EndDate = tempEndDate.AddHours(endHour);
            }

            if (!DateTime.TryParseExact(rawImportDate, "MMddyy", null, System.Globalization.DateTimeStyles.None, out tempImportDate))
            {
                fileInfo.ImportDate = DateTime.UnixEpoch;
            }
            else
            {
                fileInfo.ImportDate = tempImportDate.AddHours(importHour);
            }

            if (verbose)
            {
                // Print file data
                Console.WriteLine();
                Console.WriteLine($"Title:          {fileInfo.Title}");
                Console.WriteLine($"Artist:         {fileInfo.Artist}");
                Console.WriteLine($"Album:          {fileInfo.Album}");
                Console.WriteLine($"Year:           {fileInfo.ReleaseDate.Year}");
                Console.WriteLine($"Cart:           {fileInfo.CartNumber}");
                Console.WriteLine($"Length:         {length.Minutes:D2}:{length.Seconds:D2}");
                Console.WriteLine($"Time:           {fileInfo.StartTime.Minutes:D2}:{fileInfo.StartTime.Seconds:D2}.{fileInfo.StartTime.Milliseconds:D3} " +
                                               $"to {fileInfo.EndTime.Minutes:D2}:{fileInfo.EndTime.Seconds:D2}.{fileInfo.EndTime.Milliseconds:D3}");
                Console.WriteLine($"Dates:          {fileInfo.StartDate:g} to {fileInfo.EndDate:g}");
                Console.WriteLine($"Audio Type:     {fileInfo.AudioType}");
                Console.WriteLine($"Sample Rate:    {fileInfo.SampleRate} Hz");
                Console.WriteLine($"Stereo/Mono:    {fileInfo.StereoMono}");
                Console.WriteLine($"Compression:    {fileInfo.CompressionType}");
                Console.WriteLine($"EOM Start:      {fileInfo.EomStartTenths / 10.0} seconds");
                Console.WriteLine($"EOM Length:     {fileInfo.EomLengthHundredths / 100.0} seconds");
                Console.WriteLine($"Hook Start:     {fileInfo.HookStartMs / 1000.0} seconds");
                Console.WriteLine($"Hook EOM:       {fileInfo.HookEomMs / 1000.0} seconds");
                Console.WriteLine($"Hook End:       {fileInfo.HookEndMs / 1000.0} seconds");
                Console.WriteLine($"Font Color:     RGBA({fileInfo.FontColor[0]}, {fileInfo.FontColor[1]}, {fileInfo.FontColor[2]}, {fileInfo.FontColor[3]})");
                Console.WriteLine($"BG Color:       RGBA({fileInfo.BackgroundColor[0]}, {fileInfo.BackgroundColor[1]}, {fileInfo.BackgroundColor[2]}, {fileInfo.BackgroundColor[3]})");
                Console.WriteLine($"VT Start Time:  {fileInfo.VtStartTime.Seconds}.{fileInfo.VtStartTime.Milliseconds:D3} seconds");
                Console.WriteLine($"Before Link:    {fileInfo.BeforeCategory}/{fileInfo.BeforeCart}");
                Console.WriteLine($"After Link:     {fileInfo.AfterCategory}/{fileInfo.AfterCart}");
                Console.WriteLine($"Intro Time:     {fileInfo.IntroSeconds} seconds");
                Console.WriteLine($"End Type:       {fileInfo.EndType}");
                Console.WriteLine($"Import Hour:    {importHour}:00");
                Console.WriteLine($"Import Date:    {rawImportDate}");
                Console.WriteLine($"MPEG Bitrate:   {fileInfo.MpegBitrate} kbps");
                Console.WriteLine($"Playback Speed: {fileInfo.PlaybackSpeed / 100.0}%");
            }
        }

        private static void DumpDayparting(byte[] dayparting)
        {
            char[] days = { 'U', 'M', 'T', 'W', 'R', 'F', 'S' };

            Console.WriteLine("  00                     23");
            Console.WriteLine("  |           |           |");
            Console.WriteLine(new string('-', 27));

            for (int i = 0; i < 168; i++)
            {
                if (i % 24 == 0)
                {
                    Console.Write(days[i / 24]);
                }

                if (i % 12 == 0)
                {
                    Console.Write(' ');
                }

                Console.Write((dayparting[i / 8] >> (7 - (i % 8))) & 0x1);

                if ((i + 1) % 24 == 0)
                {
                    Console.WriteLine();
                }
            }

        }

    }
}
