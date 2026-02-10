using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WavHeaderScanner
{
    public class WavChunk
    {
        public string Id { get; set; }
        public long Offset { get; set; } // The exact byte position in the file
        public int Size { get; set; }    // Length of the data payload
    }

    public class ScottData
    {
        public byte Flags0 { get; set; } = 0x86;
        public short ArtistNumber { get; set; } = 0;
        public string Title { get; set; } = "";
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
        public string Artist { get; set; } = "";
        public string Album { get; set; } = "";
        public string IntroSeconds { get; set; } = "00";
        public char EndType { get; set; } = ' ';
        public DateTime ReleaseDate { get; set; } = DateTime.MinValue;
        public DateTime ImportDate { get; set; }
        public short MpegBitrate { get; set; } = 0;
        public ushort PlaybackSpeed { get; set; } = 33768;
        public ushort PlaybackLevel { get; set; } = 21845;
        public ushort NewPlaybackLevel { get; set; } = 33768;
        public uint ChopSize { get; set; } = 0;
        public uint VtEomOvr { get; set; } = 0;
        public uint DesiredLength { get; set; } = 0;

        // Triggers (Source ID + Time)
        public uint Trigger1 { get; set; } = 0;
        public uint Trigger2 { get; set; } = 0;
        public uint Trigger3 { get; set; } = 0;
        public uint Trigger4 { get; set; } = 0;
    }

    class Program
    {
        static void Main(string[] args)
        {

            bool verbose = false;
            bool dumpChunks = false;

            Console.WriteLine("WMTU Audio Ripper/Editor");
            Console.WriteLine("Starting WAV header scan...\n");



            // Update this to a path of a real WAV file on your machine
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string targetFolder = Path.Combine(appData, "WARE");
            string sourceDir = Path.Combine(targetFolder, "Input Files");
            string outputDir = Path.Combine(targetFolder, "Output Files");

            // Create directories if they don't exist
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(outputDir);

            // Get all wav files
            // TODO: Properly implement directory scanning, handle restricted access, etc.
            var files = Directory.EnumerateFiles(sourceDir, "*.wav", SearchOption.AllDirectories);

            foreach (string filePath in files)
            {

                string fileName = Path.GetFileName(filePath);
                string destinationPath = Path.Combine(outputDir, Path.GetRelativePath(sourceDir, filePath));
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                Console.WriteLine($"Processing file: {fileName}");

                try
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        // 1. Read the Main RIFF Header
                        string riffId = new(reader.ReadChars(4)); // Should be "RIFF"
                        int riffSize = reader.ReadInt32();              // Total size - 8
                        string waveId = new(reader.ReadChars(4)); // Should be "WAVE"

                        if (riffId != "RIFF" || waveId != "WAVE")
                        {
                            Console.WriteLine("Error: This is not a valid WAV file.");
                            return;
                        }

                        List<WavChunk> chunks = new List<WavChunk>();

                        // Loop through chunks until the current position reaches the end of the file
                        while (fs.Position < fs.Length)
                        {
                            // Capture the current position after reading the ID and size
                            long currentOffset = fs.Position + 8;
                            string chunkId = new(reader.ReadChars(4));
                            int chunkSize = reader.ReadInt32();

                            // Add this chunk to the map
                            chunks.Add(new WavChunk
                            {
                                Id = chunkId,
                                Offset = currentOffset,
                                Size = chunkSize
                            });

                            // Jump to the next chunk
                            fs.Seek(chunkSize, SeekOrigin.Current);

                            // Handle padding
                            if (chunkSize % 2 != 0 && fs.Position < fs.Length) fs.ReadByte();
                        }

                        if (verbose)
                        {
                            Console.WriteLine($"{"Offset (Hex)",-15} | {"ID",-6} | {"Size",-10}");
                            Console.WriteLine(new string('-', 35));
                            foreach (var chunk in chunks)
                            {
                                // Output the offset in Hex format (X8 means 8-character hex)
                                Console.WriteLine($"0x{chunk.Offset:X8}      | {chunk.Id,-6} | {chunk.Size:N0}");
                            }
                        }

                        //Now print 'scot' chunk info if it exists
                        var fmtEntry = chunks.Find(c => c.Id == "fmt ");
                        var dataEntry = chunks.Find(c => c.Id == "data");
                        var scotEntry = chunks.Find(c => c.Id == "scot");
                        var listEntry = chunks.Find(c => c.Id == "LIST");
                        var id3Entry = chunks.Find(c => c.Id == "id3 ");

                        // Create data flag and entry
                        bool dataRead = false;
                        ScottData fileInfo = new ScottData();

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

                        if (scotEntry != null && !dataRead)
                        {
                            dataRead = true;

                            if(dumpChunks) { DumpChunk(fs, scotEntry); }

                            // Parse the fields based on the expected structure
                            fs.Seek(scotEntry.Offset, SeekOrigin.Begin);

                            char[] trimChars = { '\0', ' ' };

                            fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, scratchpad, skip
                            fileInfo.Flags0 = reader.ReadByte();                                                // 1 byte, flags0 bitfield
                            fileInfo.ArtistNumber = reader.ReadInt16();                                         // 2 bytes, artist number
                            fileInfo.Title = new string(reader.ReadChars(43)).TrimEnd(trimChars);               // 32 bytes, title
                            fileInfo.CartNumber = new string(reader.ReadChars(4)).TrimEnd('\0');                // 4 bytes, cart number
                            fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, cart padding, skip
                            fileInfo.RawLength = new string(reader.ReadChars(5)).TrimStart(' ').PadLeft(5, '0');// 5 bytes, file length in either MM:SS or HMMSS format based on rawLengthFormat
                            startHundredths = reader.ReadInt16();                                               // 2 bytes, start time hundredths
                            startSeconds = reader.ReadInt16();                                                  // 2 bytes, start time seconds
                            endSeconds = reader.ReadInt16();                                                    // 2 bytes, end time seconds
                            endHundredths = reader.ReadInt16();                                                 // 2 bytes, end time hundredths
                            rawStartDate = new string(reader.ReadChars(6)).TrimEnd('\0');                       // 6 bytes, start date in YYMMDD format
                            rawEndDate = new string(reader.ReadChars(6)).TrimEnd('\0');                         // 6 bytes, end date in YYMMDD format
                            tempStartHour = (reader.ReadByte() - 0x80);                                         // 1 byte, start hour (0-23 stored as 0x80-0x97)
                            if (tempStartHour < 0) { tempStartHour = 0; }
                            startHour = (byte)tempStartHour;
                            tempEndHour = (reader.ReadByte() - 0x80);                                           // 1 byte, end hour (0-23 stored as 0x80-0x97)
                            if (tempEndHour < 0) { tempEndHour = 0; }
                            endHour = (byte)tempEndHour;
                            fileInfo.AudioType = (char)reader.ReadByte();                                       // 1 byte, audio type (e.g. 'A' for analog, 'D' for digital)
                            fileInfo.SampleRate = reader.ReadInt16() * 100;                                     // 2 bytes, sample rate in Hz (stored as actual sample rate divided by 100)
                            fileInfo.StereoMono = (char)reader.ReadByte();                                      // 1 byte, stereo/mono (e.g. 'S' for stereo, 'M' for mono)
                            fileInfo.CompressionType = reader.ReadByte();                                       // 1 byte, compression type (refer to documentation for specific values)
                            fileInfo.EomStartTenths = reader.ReadInt32();                                       // 4 bytes, EOM start time in tenths of a second
                            fileInfo.EomLengthHundredths = reader.ReadInt16();                                  // 2 bytes, EOM length in hundredths of a second
                            fileInfo.Flags1 = reader.ReadBytes(4);                                              // 4 byte, extended (flags)1 bitfield
                            fileInfo.HookStartMs = reader.ReadInt32();                                          // 4 bytes, hook start time in milliseconds
                            fileInfo.HookEomMs = reader.ReadInt32();                                            // 4 bytes, hook EOM time in milliseconds
                            fileInfo.HookEndMs = reader.ReadInt32();                                            // 4 bytes, hook end time in milliseconds
                            fileInfo.FontColor = reader.ReadBytes(4);                                           // 4 bytes, font color RGBA?
                            fileInfo.BackgroundColor = reader.ReadBytes(4);                                     // 4 bytes, background color RGBA?
                            fileInfo.SegmentEomMs = reader.ReadInt32();                                         // 4 bytes, segment EOM time in milliseconds (if file has segments)
                            vtStartSeconds = reader.ReadInt16();                                                // 2 bytes, VT start time seconds
                            vtStartHundredths = reader.ReadInt16();                                             // 2 bytes, VT start time hundredths
                            fileInfo.BeforeCategory = new string(reader.ReadChars(3)).TrimEnd('\0');            // 3 bytes, before link category
                            fileInfo.BeforeCart = new string(reader.ReadChars(4)).TrimEnd('\0');                // 4 bytes, before link cart number
                            fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, before link padding, skip
                            fileInfo.AfterCategory = new string(reader.ReadChars(3)).TrimEnd('\0');             // 3 bytes, after link category
                            fileInfo.AfterCart = new string(reader.ReadChars(4)).TrimEnd('\0');                 // 4 bytes, after link cart number
                            fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, after link padding, skip
                            fileInfo.RawDayparting = reader.ReadBytes(21);                                      // 21 bytes, dayparting bitfield for 7 days * 24 hours = 168 bits (21 bytes)
                            fs.Seek(108, SeekOrigin.Current);                                                   // 108 bytes, unused, skip
                            fileInfo.Artist = new string(reader.ReadChars(34)).TrimEnd(trimChars);              // 34 bytes
                            fileInfo.Album = new string(reader.ReadChars(34)).TrimEnd(trimChars);               // 34 bytes
                            fileInfo.IntroSeconds = new string(reader.ReadChars(2)).TrimEnd('\0');              // 2 bytes
                            fileInfo.EndType = (char)reader.ReadByte();                                         // 1 byte
                            fileInfo.ReleaseDate = new DateTime(int.Parse(new string(reader.ReadChars(4)).TrimEnd('\0')), 1, 1);                      // 4 bytes
                            fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, skip
                            importHour = (byte)(reader.ReadByte() - 0x80);                                      // 1 byte
                            rawImportDate = new string(reader.ReadChars(6)).TrimEnd('\0');                      // 6 bytes
                            fileInfo.MpegBitrate = reader.ReadInt16();                                          // 2 bytes
                            fileInfo.PlaybackSpeed = reader.ReadUInt16();                                       // 2 bytes, raw playback speed
                            fileInfo.PlaybackLevel = reader.ReadUInt16();                                       // 2 bytes, raw playback level
                            fs.Seek(5, SeekOrigin.Current);                                                     // 4 bytes, file size, skip, new size should be calculated (UInt32)
                            fileInfo.NewPlaybackLevel = reader.ReadUInt16();                                    // 2 bytes, playback level as percentage of original level x10, highest bit means valid
                            fileInfo.ChopSize = reader.ReadUInt32();                                            // 4 bytes, hundredths of seconds removed from audio middle, highest bt means valid
                            fileInfo.VtEomOvr = reader.ReadUInt32();                                            // 4 bytes, millis to subtract from pre-cut EOM
                            fileInfo.DesiredLength = reader.ReadUInt32();                                       // 4 bytes, desired file length
                            fileInfo.Trigger1 = reader.ReadUInt32();                                            // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                            fileInfo.Trigger2 = reader.ReadUInt32();                                            // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                            fileInfo.Trigger3 = reader.ReadUInt32();                                            // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                            fileInfo.Trigger4 = reader.ReadUInt32();                                            // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                            fs.Seek(33, SeekOrigin.Current);                                                    // 33 bytes to end of header

                        }

                        if (listEntry != null && !dataRead)
                        {
                            dataRead = true;

                            if (dumpChunks) { DumpChunk(fs, listEntry); }

                            fs.Seek(listEntry.Offset, SeekOrigin.Begin);

                            if (new string(reader.ReadChars(4)).Equals("INFO"))
                            {
                                if (verbose) { Console.WriteLine("\n--- INFO Subchunk ---"); }
                                while (fs.Position < listEntry.Offset + listEntry.Size)
                                {
                                    string infoId = new(reader.ReadChars(4));
                                    int infoSize = reader.ReadInt32();
                                    string infoData = new string(reader.ReadChars(infoSize)).TrimEnd('\0');
                                    if (verbose) { Console.WriteLine($"{infoId}: {infoData}"); }
                                    // Handle padding
                                    if (infoSize % 2 != 0 && fs.Position < listEntry.Offset + listEntry.Size) fs.ReadByte();

                                    switch (infoId)
                                    {
                                        case "IPRD":    // Album
                                            fileInfo.Album = infoData;
                                            break;
                                        case "IART":    // Artist(s)
                                            fileInfo.Artist = infoData;
                                            break;
                                        case "INAM":    // Title
                                            fileInfo.Title = infoData;
                                            break;
                                        case "ICRD":    // Date
                                            fileInfo.ReleaseDate = ParseWavDate(infoData);
                                            break;

                                    }
                                }
                            }
                        }

                        if (id3Entry != null && !dataRead)
                        {
                            dataRead = true;

                            DumpChunk(fs, id3Entry);
                        }

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

                        DateTime tempStartDate, tempEndDate;

                        // Calculate start and end dates
                        if (!DateTime.TryParseExact(rawStartDate, "MMddyy", null, System.Globalization.DateTimeStyles.None, out tempStartDate))
                        {
                            fileInfo.StartDate = DateTime.UnixEpoch;
                        }
                        else
                        {
                            fileInfo.StartDate = tempStartDate.AddHours(startHour);
                        }

                        DateTime endDate;
                        if (!DateTime.TryParseExact(rawEndDate, "MMddyy", null, System.Globalization.DateTimeStyles.None, out tempEndDate))
                        {
                            fileInfo.EndDate = DateTime.MaxValue;
                        }
                        else
                        {
                            fileInfo.EndDate = tempEndDate.AddHours(endHour);
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

                        using (var writer = new BinaryWriter(File.Create(destinationPath)))
                        {
                            writer.Write("RIFF    WAVE".ToCharArray());
                            CopyChunk(writer, reader, fmtEntry, 128);
                            writer.Write(ScotSerializer(fileInfo));
                            CopyChunk(writer, reader, dataEntry, 8192);
                            writer.Write(ListSerializer(fileInfo));

                            //if (id3Entry != null)
                            //{
                            //    CopyChunk(writer, reader, id3Entry, 512);
                            //}

                            long totalFileSize = writer.BaseStream.Length;
                            writer.Seek(4, SeekOrigin.Begin); // Jump back to file size field
                            writer.Write((int)(totalFileSize - 8)); // RIFF size = FileSize - 8
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nSomething went wrong: {ex.Message}");
                }
            }

            Console.WriteLine("\nScan complete. Press any key to exit.");
            Console.ReadKey();
        }

        public static void DumpChunk(FileStream fs, WavChunk chunk, int bytesPerLine = 32)
        {
            if (chunk == null) return;

            // 1. Jump to the data
            fs.Seek(chunk.Offset, SeekOrigin.Begin);

            // 2. Read the chunk data into a local buffer
            byte[] data = new byte[chunk.Size];
            fs.Read(data, 0, chunk.Size);

            // 3. Print the Header
            Console.WriteLine($"\n>>> CHUNK: {chunk.Id} | OFFSET: 0x{chunk.Offset:X8} | SIZE: {chunk.Size} bytes <<<");
            Console.WriteLine($"{"Offset",-6} | {"Content"}");
            Console.WriteLine(new string('-', 45));

            // 4. The Loop (Same logic as before, but cleaner)
            for (int i = 0; i < data.Length; i += bytesPerLine)
            {
                int remaining = Math.Min(bytesPerLine, data.Length - i);
                string text = Encoding.ASCII.GetString(data, i, remaining);

                // Replace control/null chars with dots for visibility
                StringBuilder sb = new StringBuilder();
                foreach (char c in text)
                {
                    sb.Append(char.IsControl(c) || c == '\0' ? '.' : c);
                }

                Console.WriteLine($"{i:X3}    | {sb.ToString()}");
            }
        }

        public static void DumpDayparting(byte[] dayparting)
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

        private static DateTime ParseWavDate(string rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate)) return DateTime.MinValue;

            // 1. Define the formats you expect to see
            string[] formats = {
                "yyyy-MM-dd",
                "yyyy",
                "yyyy/MM/dd",
                "MM/dd/yyyy",
                "dd/MM/yyyy",
                "yyyy-MM-dd HH:mm:ss"
            };

            // 2. Try to parse strictly against your list
            if (DateTime.TryParseExact(rawDate, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
            }

            // 3. Last Resort: Let C# guess (handles "Feb 9, 2026" etc.)
            if (DateTime.TryParse(rawDate, out DateTime looseResult))
            {
                return looseResult;
            }

            // 4. Give up
            return DateTime.MinValue; // or DateTime.UnixEpoch
        }

        public static byte[] ScotSerializer(ScottData data)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write("scot    ".ToCharArray());
                writer.Write((byte)0);                                                              // 1 byte, scratchpad
                writer.Write(data.Flags0);                                                          // 1 byte, flags
                writer.Write(data.ArtistNumber);                                                    // 2 bytes, artist num
                writer.Write(FixedString(data.Title,43,(byte)' '));                                 // 43 bytes, title
                writer.Write(FixedString(data.CartNumber, 4, 0x00));                                // 4 bytes, cart number
                writer.Write((byte)' ');                                                            // 1 byte, cart padding, skip
                writer.Write(FixedString(data.RawLength, 5, (byte)' '));                            // 5 bytes, file length in either MM:SS or HMMSS format based on rawLengthFormat
                writer.Write((ushort)data.StartTime.Seconds);                                       // 2 bytes, start time seconds
                writer.Write((ushort)(data.StartTime.Milliseconds / 10));                           // 2 bytes, start time hundredths
                writer.Write((ushort)data.EndTime.Seconds);                                         // 2 bytes, end time seconds
                writer.Write((ushort)(data.EndTime.Milliseconds / 10));                             // 2 bytes, end time hundredths

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
                writer.Write((ushort)(data.VtStartTime.Milliseconds/10));                           // 2 bytes, VT start time hundredths
                writer.Write(FixedString(data.BeforeCategory, 3, (byte)' '));                       // 3 bytes, before link category
                writer.Write(FixedString(data.BeforeCart, 4, (byte)' '));                           // 4 bytes, before link cart number
                writer.Write((byte)' ');                                                            // 1 byte, before link padding, skip
                writer.Write(FixedString(data.AfterCategory, 3, (byte)' '));                        // 3 bytes, after link category
                writer.Write(FixedString(data.AfterCart, 4, (byte)' '));                            // 4 bytes, after link cart number
                writer.Write((byte)' ');                                                            // 1 byte, after link padding, skip
                writer.Write(data.RawDayparting);                                                   // 21 bytes, dayparting bitfield for 7 days * 24 hours = 168 bits (21 bytes)
                writer.Write(new byte[108]);                                                        // 108 bytes, unused, skip
                writer.Write(FixedString(data.Artist, 34, (byte)' '));                              // 34 bytes
                writer.Write(FixedString(data.Album, 34, (byte)' '));                               // 34 bytes
                writer.Write(FixedString(data.IntroSeconds, 2, (byte)'0'));                         // 2 bytes
                writer.Write(data.EndType);                                                         // 1 byte
                writer.Write(FixedString(data.ReleaseDate.Year.ToString(), 4, (byte)'0'));          // 4 bytes
                writer.Write((byte)0);                                                              // 1 byte, skip
                writer.Write((byte)(data.ImportDate.Hour + 128));                                   // 1 byte
                writer.Write(data.ImportDate.ToString("MMddyy").ToCharArray());                     // 6 bytes
                writer.Write(data.MpegBitrate);                                                     // 2 bytes
                writer.Write(data.PlaybackSpeed);                                                   // 2 bytes, raw playback speed
                writer.Write(data.PlaybackLevel);                                                   // 2 bytes, raw playback level
                writer.Write(new byte[5]);                                                          // 5 bytes, file size, skip, new size should be calculated (UInt32)
                writer.Write(data.NewPlaybackLevel);                                                // 2 bytes, playback level as percentage of original level x10, highest bit means valid
                writer.Write(data.ChopSize);                                                        // 4 bytes, hundredths of seconds removed from audio middle, highest bt means valid
                writer.Write(data.VtEomOvr);                                                        // 4 bytes, millis to subtract from pre-cut EOM
                writer.Write(data.DesiredLength);                                                   // 4 bytes, desired file length
                writer.Write(data.Trigger1);                                                        // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                writer.Write(data.Trigger1);                                                        // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                writer.Write(data.Trigger1);                                                        // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                writer.Write(data.Trigger1);                                                        // 4 bytes, trigger, Highest byte is source ID, next 3 bytes are tenths of seconds from beginning of audio
                writer.Write(new byte[33]);                                                         // 33 bytes to end of header

                long totalChunkSize = writer.BaseStream.Length;
                writer.Seek(4, SeekOrigin.Begin); // Jump back to file size field
                writer.Write((int)(totalChunkSize - 8)); // RIFF size = FileSize - 8

                return ms.ToArray();
            }
        }

        public static byte[] ListSerializer(ScottData data)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                // For demonstration, we'll just write the Artist and Title in a simple format
                writer.Write("LIST    ".ToCharArray()); // Chunk ID
                writer.Write("INFO".ToCharArray());     // List type
                WriteInfoSubChunk(writer, "IPRD", data.Album);
                WriteInfoSubChunk(writer, "IART", data.Artist);
                WriteInfoSubChunk(writer, "INAM", data.Title);
                WriteInfoSubChunk(writer, "ICRD", data.ReleaseDate.ToString("yyyy-MM-dd"));

                long totalChunkSize = writer.BaseStream.Length;
                writer.Seek(4, SeekOrigin.Begin); // Jump back to file size field
                writer.Write((int)(totalChunkSize - 8)); // RIFF size = FileSize - 8

                return ms.ToArray();
            }
        }

        public static void CopyChunk(BinaryWriter writer, BinaryReader reader, WavChunk chunk, int bufferSize)
        {
            // 1. Move the reader to the start of the chunk (ID + Size + Data)
            // If your WavChunk.Offset points to the DATA, subtract 8 to get the Header.
            reader.BaseStream.Position = chunk.Offset;

            // 2. Write the Chunk ID (e.g., "fmt " or "data")
            writer.Write(Encoding.ASCII.GetBytes(chunk.Id));

            // 3. Write the Chunk Size
            writer.Write(chunk.Size);

            // 4. Copy the Data Payload
            long bytesToCopy = chunk.Size;
            byte[] buffer = new byte[bufferSize];

            while (bytesToCopy > 0)
            {
                // Don't read more than what's left in the chunk or the buffer size
                int toRead = (int)Math.Min(bytesToCopy, buffer.Length);
                int read = reader.Read(buffer, 0, toRead);

                if (read == 0) break; // Safety check for end of file

                writer.Write(buffer, 0, read);
                bytesToCopy -= read;
            }

            // 5. The RIFF "Padding" Rule
            // If a chunk size is odd, there is a hidden null byte in the file 
            // to keep things word-aligned. We must copy/write it if it exists.
            if (chunk.Size % 2 != 0)
            {
                writer.Write((byte)0);
            }
        }

        private static byte[] FixedString(string value, int length, byte pad)
        {
            // 1. Handle nulls
            if (value == null) value = "";

            // 2. Create a buffer of the exact length filled with pad character
            byte[] buffer = new byte[length];
            Array.Fill(buffer, pad);

            // 3. Get bytes from string
            byte[] strBytes = Encoding.ASCII.GetBytes(value);

            // 4. Copy string into buffer (truncating if necessary)
            int copyLen = Math.Min(strBytes.Length, length);
            Array.Copy(strBytes, buffer, copyLen);

            // 5. Return
            return buffer;
        }

        private static void WriteInfoSubChunk(BinaryWriter writer, string id, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Strings in INFO chunks must be Null-Terminated
            byte[] textBytes = Encoding.ASCII.GetBytes(text + "\0");

            writer.Write(Encoding.ASCII.GetBytes(id)); // 4-byte ID
            writer.Write(textBytes.Length);            // 4-byte Size
            writer.Write(textBytes);                   // Data

            // RIFF Padding: If the chunk size is odd, you MUST add a padding byte
            if (textBytes.Length % 2 != 0)
            {
                writer.Write((byte)0);
            }
        }
    }
}

   