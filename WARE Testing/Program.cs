using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WavHeaderScanner
{
    public class WavChunk
    {
        public string ID { get; set; }
        public long Offset { get; set; } // The exact byte position in the file
        public int Size { get; set; }    // Length of the data payload
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Update this to a path of a real WAV file on your machine
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string targetFolder = Path.Combine(appData, "WARE");
            string filePath = Path.Combine(targetFolder, "test.wav");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

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
                            ID = chunkId,
                            Offset = currentOffset,
                            Size = chunkSize
                        });

                        // Jump to the next chunk
                        fs.Seek(chunkSize, SeekOrigin.Current);

                        // Handle padding
                        if (chunkSize % 2 != 0 && fs.Position < fs.Length) fs.ReadByte();
                    }

                    Console.WriteLine($"{"Offset (Hex)",-15} | {"ID",-6} | {"Size",-10}");
                    Console.WriteLine(new string('-', 35));
                    foreach (var chunk in chunks)
                    {
                        // Output the offset in Hex format (X8 means 8-character hex)
                        Console.WriteLine($"0x{chunk.Offset:X8}      | {chunk.ID,-6} | {chunk.Size:N0}");
                    }

                    //Now print 'scot' chunk info if it exists
                    var scotEntry = chunks.Find(c => c.ID == "scot");
                    var listEntry = chunks.Find(c => c.ID == "LIST");
                    var id3Entry = chunks.Find(c => c.ID == "id3 ");

                    if (listEntry != null)
                    {
                        DumpChunk(fs, listEntry);
                    }

                    //if (id3Entry != null)
                    //{
                    //    DumpChunk(fs, id3Entry);
                    //}


                    if (scotEntry != null)
                    {
                        DumpChunk(fs, scotEntry);

                        // Parse the fields based on the expected structure
                        fs.Seek(scotEntry.Offset, SeekOrigin.Begin);

                        char[] trimChars = {'\0',' '};

                        fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, scratchpad, skip
                        byte flags0 = reader.ReadByte();                                                    // 1 byte, flags0 bitfield
                        short artistNumber = reader.ReadInt16();                                            // 2 bytes, artist number
                        string title = new string(reader.ReadChars(43)).TrimEnd(trimChars);                 // 32 bytes, title
                        string cartNumber = new string(reader.ReadChars(4)).TrimEnd('\0');                  // 4 bytes, cart number
                        fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, cart padding, skip
                        string rawLength = new string(reader.ReadChars(5)).TrimStart(' ').PadLeft(5, '0');  // 5 bytes, file length in either MM:SS or HMMSS format based on rawLengthFormat
                        short startHundredths = reader.ReadInt16();                                         // 2 bytes, start time hundredths
                        short startSeconds = reader.ReadInt16();                                            // 2 bytes, start time seconds
                        short endSeconds = reader.ReadInt16();                                              // 2 bytes, end time seconds
                        short endHundredths = reader.ReadInt16();                                           // 2 bytes, end time hundredths
                        string rawStartDate = new string(reader.ReadChars(6)).TrimEnd('\0');                // 6 bytes, start date in YYMMDD format
                        string rawEndDate = new string(reader.ReadChars(6)).TrimEnd('\0');                  // 6 bytes, end date in YYMMDD format
                        byte startHour = (byte)(reader.ReadByte() - 0x80);                                  // 1 byte, start hour (0-23 stored as 0x80-0x97)
                        byte endHour = (byte)(reader.ReadByte() - 0x80);                                    // 1 byte, end hour (0-23 stored as 0x80-0x97)
                        char audioType = (char)reader.ReadByte();                                           // 1 byte, audio type (e.g. 'A' for analog, 'D' for digital)
                        int sampleRate = reader.ReadInt16() * 100;                                          // 2 bytes, sample rate in Hz (stored as actual sample rate divided by 100)
                        char stereoMono = (char)reader.ReadByte();                                          // 1 byte, stereo/mono (e.g. 'S' for stereo, 'M' for mono)
                        byte compressionType = reader.ReadByte();                                           // 1 byte, compression type (refer to documentation for specific values)
                        int eomStartTenths = reader.ReadInt32();                                            // 4 bytes, EOM start time in tenths of a second
                        short eomLengthHundredths = reader.ReadInt16();                                     // 2 bytes, EOM length in hundredths of a second
                        byte[] flags1 = reader.ReadBytes(4);                                                // 4 byte, extended (flags)1 bitfield
                        int hookStartMilliseconds = reader.ReadInt32();                                     // 4 bytes, hook start time in milliseconds
                        int hookEOMMilliseconds = reader.ReadInt32();                                       // 4 bytes, hook EOM time in milliseconds
                        int hookEndMilliseconds = reader.ReadInt32();                                       // 4 bytes, hook end time in milliseconds
                        byte[] fontColor = reader.ReadBytes(4);                                             // 4 bytes, font color RGBA?
                        byte[] backgroundColor = reader.ReadBytes(4);                                       // 4 bytes, background color RGBA?
                        int segmentEOM = reader.ReadInt32();                                                // 4 bytes, segment EOM time in milliseconds (if file has segments)
                        short vtStartSeconds = reader.ReadInt16();                                          // 2 bytes, VT start time seconds
                        short vtStartHundredths = reader.ReadInt16();                                       // 2 bytes, VT start time hundredths
                        string beforeCategory = new string(reader.ReadChars(3)).TrimEnd('\0');              // 3 bytes, before link category
                        string beforeCart = new string(reader.ReadChars(4)).TrimEnd('\0');                  // 4 bytes, before link cart number
                        fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, before link padding, skip
                        string afterCategory = new string(reader.ReadChars(3)).TrimEnd('\0');               // 3 bytes, after link category
                        string afterCart = new string(reader.ReadChars(4)).TrimEnd('\0');                   // 4 bytes, after link cart number
                        fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, after link padding, skip
                        byte[] rawDayparting = reader.ReadBytes(21);                                        // 21 bytes, dayparting bitfield for 7 days * 24 hours = 168 bits (21 bytes)
                        fs.Seek(108, SeekOrigin.Current);                                                   // 108 bytes, unused, skip
                        string artist = new string(reader.ReadChars(34)).TrimEnd(trimChars);                // 34 bytes
                        string album = new string(reader.ReadChars(34)).TrimEnd(trimChars);                 // 34 bytes
                        string introSeconds = new string(reader.ReadChars(2)).TrimEnd('\0');                // 2 bytes
                        char endType = (char)reader.ReadByte();                                             // 1 byte
                        string year = new string(reader.ReadChars(4)).TrimEnd('\0');                        // 4 bytes
                        fs.Seek(1, SeekOrigin.Current);                                                     // 1 byte, skip
                        byte importHour = (byte)(reader.ReadByte() - 0x80);                                 // 1 byte
                        string rawImportDate = new string(reader.ReadChars(6)).TrimEnd('\0');               // 6 bytes
                        short mpegBitrate = reader.ReadInt16();                                             // 2 bytes
                        ushort rawPlaybackSpeed = reader.ReadUInt16();                                      // 2 bytes

                        // Create bit arrays to parse flags
                        BitArray flags0Bits = new BitArray(new byte[] { flags0 });
                        BitArray flags1Bits = new BitArray(flags1);

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
                        bool vtEOMOVR = flags1Bits[1];
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
                            Console.WriteLine($"Voice Track EOM Override:       {(vtEOMOVR ? "Yes" : "No")}");
                            Console.WriteLine($"Do Not Play On Internet:        {(notPlayOnInternet ? "Yes" : "No")}");
                        }

                        // Print Dayparting (if enabled)
                        if (flags1Enabled && daypartingEnabled)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Dayparting Table (If Enabled):");
                            DumpDayparting(rawDayparting);
                        }

                        // Calculate length
                        TimeSpan length;
                        if (rawLengthFromat)
                        {
                            //HMMSS
                            int hours = int.Parse(rawLength.Substring(0, 1));
                            int minutes = int.Parse(rawLength.Substring(1, 2));
                            int seconds = int.Parse(rawLength.Substring(3, 2));
                            length = new TimeSpan(hours, minutes, seconds);
                        }
                        else
                        {
                            //MM:SS
                            int minutes = int.Parse(rawLength.Substring(0, 2));
                            int seconds = int.Parse(rawLength.Substring(3, 2));
                            length = new TimeSpan(0, minutes, seconds);
                        }

                        // Calculate start and end times in seconds
                        TimeSpan startTime = new TimeSpan(0, 0, 0, startSeconds, startHundredths * 10);
                        TimeSpan endTime = new TimeSpan(0, 0, 0, endSeconds, endHundredths * 10);

                        // Calculate start and end dates
                        DateTime startDate;
                        if (!DateTime.TryParseExact(rawStartDate, "yyMMdd", null, System.Globalization.DateTimeStyles.None, out startDate))
                        {
                            startDate = DateTime.UnixEpoch;
                        }
                        else
                        {
                           startDate.AddHours(startHour);
                        }

                        DateTime endDate;
                        if (!DateTime.TryParseExact(rawEndDate, "yyMMdd", null, System.Globalization.DateTimeStyles.None, out endDate))
                        {
                            endDate = DateTime.MaxValue;
                        }
                        else
                        {
                            endDate.AddHours(startHour);
                        }

                        // Print file data
                        Console.WriteLine();
                        Console.WriteLine($"Title:          {title}");
                        Console.WriteLine($"Artist:         {artist}");
                        Console.WriteLine($"Album:          {album}");
                        Console.WriteLine($"Year:           {year}");
                        Console.WriteLine($"Cart:           {cartNumber}");
                        Console.WriteLine($"Length:         {length.Minutes:D2}:{length.Seconds:D2}");
                        Console.WriteLine($"Time:           {startTime.Minutes:D2}:{startTime.Seconds:D2}.{startTime.Milliseconds:D3} to {endTime.Minutes:D2}:{endTime.Seconds:D2}.{endTime.Milliseconds:D3}");
                        Console.WriteLine($"Dates:          {startDate:g} to {endDate:g}");
                        Console.WriteLine($"Audio Type:     {audioType}");
                        Console.WriteLine($"Sample Rate:    {sampleRate} Hz");
                        Console.WriteLine($"Stereo/Mono:    {stereoMono}");
                        Console.WriteLine($"Compression:    {compressionType}");
                        Console.WriteLine($"EOM Start:      {eomStartTenths / 10.0} seconds");
                        Console.WriteLine($"EOM Length:     {eomLengthHundredths / 100.0} seconds");
                        Console.WriteLine($"Hook Start:     {hookStartMilliseconds / 1000.0} seconds");
                        Console.WriteLine($"Hook EOM:       {hookEOMMilliseconds / 1000.0} seconds");
                        Console.WriteLine($"Hook End:       {hookEndMilliseconds / 1000.0} seconds");
                        Console.WriteLine($"Font Color:     RGBA({fontColor[0]}, {fontColor[1]}, {fontColor[2]}, {fontColor[3]})");
                        Console.WriteLine($"BG Color:       RGBA({backgroundColor[0]}, {backgroundColor[1]}, {backgroundColor[2]}, {backgroundColor[3]})");
                        Console.WriteLine($"VT Start Time:  {vtStartSeconds}.{vtStartHundredths:D2} seconds");
                        Console.WriteLine($"Before Link:    {beforeCategory}/{beforeCart}");
                        Console.WriteLine($"After Link:     {afterCategory}/{afterCart}");
                        Console.WriteLine($"Intro Time:     {introSeconds} seconds");
                        Console.WriteLine($"End Type:       {endType}");
                        Console.WriteLine($"Import Hour:    {importHour}:00");
                        Console.WriteLine($"Import Date:    {rawImportDate}");
                        Console.WriteLine($"MPEG Bitrate:   {mpegBitrate} kbps");
                        Console.WriteLine($"Playback Speed: {rawPlaybackSpeed / 100.0}%");

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nSomething went wrong: {ex.Message}");
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
            Console.WriteLine($"\n>>> CHUNK: {chunk.ID} | OFFSET: 0x{chunk.Offset:X8} | SIZE: {chunk.Size} bytes <<<");
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
    }
}