using System.Collections;
using System.Text;
using System.Security.Cryptography;

namespace WARE_Testing
{
    public class WavChunk
    {
        public string Id { get; set; }
        public long Offset { get; set; } // The exact byte position in the file
        public int Size { get; set; }    // Length of the data payload
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
                        AudioMetadata fileInfo = new AudioMetadata();


                        if (scotEntry != null && !dataRead)
                        {
                            dataRead = true;

                            if (dumpChunks) { DumpChunk(fs, scotEntry); }

                            // Parse the scot chunk to fill in the AudioMetadata object
                            ScotChunk.Parse(fileInfo, reader, fs, scotEntry, verbose);

                        }

                        if (listEntry != null && !dataRead)
                        {
                            dataRead = true;

                            if (dumpChunks) { DumpChunk(fs, listEntry); }

                            // Parse the list chunk to fill in the AudioMetadata object
                            ListChunk.Parse(fileInfo, reader, fs, listEntry, verbose);


                        }

                        if (id3Entry != null && !dataRead)
                        {
                            dataRead = true;

                            DumpChunk(fs, id3Entry);
                        }



                        // Create output path and writer
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                        using (var writer = new BinaryWriter(File.Create(destinationPath)))
                        {
                            writer.Write("RIFF    WAVE".ToCharArray());
                            WaveUtils.CopyChunk(writer, reader, fmtEntry, 128);
                            writer.Write(ScotChunk.Searialize(fileInfo));
                            WaveUtils.CopyChunk(writer, reader, dataEntry, 8192);
                            writer.Write(ListChunk.Serialize(fileInfo));

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
                    Console.WriteLine($"\nSomething went wrong: {ex}");
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
    }
}

