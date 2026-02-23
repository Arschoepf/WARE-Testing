using System;
using System.Collections.Generic;
using System.Text;

namespace WARE_Testing
{
    public class ListChunk
    {

        public static byte[] Serialize(AudioMetadata data)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                // For demonstration, we'll just write the Artist and Title in a simple format
                writer.Write("LIST    ".ToCharArray()); // Chunk ID
                writer.Write("INFO".ToCharArray());     // List type
                WaveUtils.WriteInfoSubChunk(writer, "IPRD", data.Album);
                WaveUtils.WriteInfoSubChunk(writer, "IART", data.Artist);
                WaveUtils.WriteInfoSubChunk(writer, "INAM", data.Title);
                WaveUtils.WriteInfoSubChunk(writer, "ICRD", data.ReleaseDate.ToString("yyyy-MM-dd"));

                long totalChunkSize = writer.BaseStream.Length;
                writer.Seek(4, SeekOrigin.Begin); // Jump back to file size field
                writer.Write((int)(totalChunkSize - 8)); // RIFF size = FileSize - 8

                return ms.ToArray();
            }
        }

        public static void Parse(AudioMetadata fileInfo, BinaryReader reader, FileStream fs, WavChunk listEntry, bool verbose)
        {
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
                            fileInfo.ReleaseDate = WaveUtils.ParseDate(infoData);
                            break;

                    }
                }
            }
        }
    }
}
