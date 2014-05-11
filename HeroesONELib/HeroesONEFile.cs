using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HeroesONELib
{
    public class HeroesONEFile
    {
        public class File
        {
            public string Name { get; set; }
            public byte[] Data { get; set; }

            public File()
            {
                Name = string.Empty;
            }

            public File(string fileName)
            {
                Name = Path.GetFileName(fileName);
                Data = System.IO.File.ReadAllBytes(fileName);
            }

            public File(string name, byte[] data)
            {
                Name = name;
                Data = data;
            }
        }

        public List<File> Files { get; set; }

        const int Magic = 0x1400FFFF;

        public HeroesONEFile()
        {
            Files = new List<File>();
        }

        public HeroesONEFile(string filename)
        {
            Files = new List<File>();
            using (FileStream stream = System.IO.File.OpenRead(filename))
            using (BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.ASCII))
            {
                stream.Seek(4, SeekOrigin.Current);
                int filesize = reader.ReadInt32() + 0xC;
                if (reader.ReadInt32() != Magic)
                    throw new Exception("Error: Unknown archive type");
                stream.Seek(4, SeekOrigin.Current);
                long fnlength = reader.ReadInt32();
                stream.Seek(4, SeekOrigin.Current);
                List<string> filenames = new List<string>((int)(fnlength / 64));
                fnlength += stream.Position;
                while (stream.Position < fnlength)
                    filenames.Add(reader.ReadString(64));
                stream.Seek(fnlength, SeekOrigin.Begin);
                while (stream.Position < filesize)
                {
                    int fn = reader.ReadInt32();
                    int sz = reader.ReadInt32();
                    stream.Seek(4, SeekOrigin.Current);
                    Files.Add(new File(filenames[fn], reader.ReadBytes(sz)));
                }
            }
        }

        public void Save(string filename)
        {
            byte[] filenames = new byte[256 * 64];
            using (FileStream stream = System.IO.File.Open(filename, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.ASCII))
            {
                writer.Write(0);
                long fspos = stream.Position;
                writer.Write(-1);
                writer.Write(Magic);
                long fsstart = stream.Position;
                writer.Write(1);
                writer.Write(filenames.Length);
                writer.Write(Magic);
                long fnpos = stream.Position;
                writer.Write(filenames);
                int i = 2;
                foreach (File item in Files)
                {
                    writer.Write(i++);
                    writer.Write(item.Data.Length);
                    writer.Write(Magic);
                    writer.Write(item.Data);
                    System.Text.Encoding.ASCII.GetBytes(item.Name).CopyTo(filenames, i * 64);
                }
                stream.Seek(fnpos, SeekOrigin.Begin);
                writer.Write(filenames);
                stream.Seek(fspos, SeekOrigin.Begin);
                writer.Write((int)(stream.Length - fsstart));
            }
        }

        private static string GetCString(byte[] file, int address)
        {
            int textsize = 0;
            while (file[address + textsize] > 0 & textsize < 64)
                textsize++;
            return Encoding.ASCII.GetString(file, address, textsize);
        }
    }
    public static class Extensions
    {
        /// <summary>
        /// Reads a null-terminated ASCII string from the current stream and advances the position by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The maximum length of the string, in bytes.</param>
        public static string ReadString(this BinaryReader br, int length)
        {
            byte[] buffer = br.ReadBytes(length);
            for (int i = 0; i < length; i++)
                if (buffer[i] == 0)
                    return Encoding.ASCII.GetString(buffer, 0, i);
            return Encoding.ASCII.GetString(buffer);
        }
    }
}