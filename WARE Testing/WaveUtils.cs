using System;
using System.Collections.Generic;
using System.Text;

namespace WARE_Testing
{
    public static class WaveUtils
    {
        public static byte[] FixedString(string value, int length, byte pad)
        {
            if (value == null) value = string.Empty;

            byte[] buffer = new byte[length];
            Array.Fill(buffer, pad);

            byte[] strBytes = Encoding.UTF8.GetBytes(value);

            int copyLen = strBytes.Length;
            if (copyLen > length)
            {
                // Start at the maximum allowed length
                copyLen = length;

                // 1. Back up if we are in the middle of a multi-byte character (Continuation Byte)
                while (copyLen > 0 && (strBytes[copyLen - 1] & 0xC0) == 0x80)
                {
                    copyLen--;
                }

                // 2. Back up one more if the current last byte is a Lead Byte (Starts with 11)
                // because it's now missing its required continuation bytes.
                if (copyLen > 0 && (strBytes[copyLen - 1] & 0x80) != 0)
                {
                    copyLen--;
                }
            }

            Array.Copy(strBytes, 0, buffer, 0, copyLen);
            return buffer;
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

        public static void WriteInfoSubChunk(BinaryWriter writer, string id, string text)
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

        public static DateTime ParseDate(string rawDate)
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
    }

}
