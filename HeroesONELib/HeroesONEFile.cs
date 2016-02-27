using FraGag.Compression;
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

		public bool IsShadow { get; private set; }
        public List<File> Files { get; set; }

        const int HeroesMagic = 0x1400FFFF;
		const int ShadowMagic = 0x1C020037;

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
				switch (reader.ReadInt32())
				{
					case HeroesMagic:
						{
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
								Files.Add(new File(filenames[fn], Prs.Decompress(reader.ReadBytes(sz))));
							}
						}
						break;
					case ShadowMagic:
						{
							IsShadow = true;
							if (reader.ReadString(12) != "One Ver 0.60") goto default;
							stream.Seek(4, SeekOrigin.Current);
							int fnum = reader.ReadInt32();
							stream.Seek(0x90, SeekOrigin.Current);
							List<string> filenames = new List<string>(fnum);
							List<int> fileaddrs = new List<int>(fnum);
							for (int i = 0; i < fnum; i++)
							{
								filenames.Add(reader.ReadString(0x2C));
								stream.Seek(4, SeekOrigin.Current);
								fileaddrs.Add(reader.ReadInt32());
								stream.Seek(4, SeekOrigin.Current);
							}
							fileaddrs.Add(filesize);
							for (int i = 0; i < fnum; i++)
							{
								stream.Seek(fileaddrs[i] + 0xC, SeekOrigin.Begin);
								Files.Add(new File(filenames[i], Prs.Decompress(reader.ReadBytes(fileaddrs[i + 1] - fileaddrs[i]))));
							}
						}
						break;
					default:
						throw new Exception("Error: Unknown archive type");
				}
			}
        }

        public void Save(string filename, bool shadow)
        {
            using (FileStream stream = System.IO.File.Open(filename, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.ASCII))
            {
                writer.Write(0);
                long fspos = stream.Position;
                writer.Write(-1);
				if (!shadow)
				{
					byte[] filenames = new byte[256 * 64];
					writer.Write(HeroesMagic);
					writer.Write(1);
					writer.Write(filenames.Length);
					writer.Write(HeroesMagic);
					long fnpos = stream.Position;
					writer.Write(filenames);
					int i = 2;
					foreach (File item in Files)
					{
						byte[] data = Prs.Compress(item.Data);
						writer.Write(i++);
						writer.Write(data.Length);
						writer.Write(HeroesMagic);
						writer.Write(data);
						System.Text.Encoding.ASCII.GetBytes(item.Name).CopyTo(filenames, (i - 1) * 64);
					}
					stream.Seek(fnpos, SeekOrigin.Begin);
					writer.Write(filenames);
				}
				else
				{
					writer.Write(ShadowMagic);
					writer.Write("One Ver 0.60", 12);
					writer.Write(0);
					writer.Write(Files.Count);
					byte[] buf = new byte[0x90];
					for (int i = 1; i < 0x20; i++)
						buf[i] = 0xCD;
					writer.Write(buf);
					Queue<long> addrpos = new Queue<long>(Files.Count);
					foreach (File item in Files)
					{
						writer.Write(item.Name, 0x2C);
						writer.Write(item.Data.Length);
						addrpos.Enqueue(stream.Position);
						writer.Write(-1);
						writer.Write(1);
					}
					foreach (File item in Files)
					{
						stream.Seek(addrpos.Dequeue(), SeekOrigin.Begin);
						writer.Write((int)(stream.Length - 0xC));
						stream.Seek(0, SeekOrigin.End);
						writer.Write(Prs.Compress(item.Data));
					}
				}
                stream.Seek(fspos, SeekOrigin.Begin);
                writer.Write((int)(stream.Length - 0xC));
            }
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

		public static void Write(this BinaryWriter bw, string str, int length)
		{
			byte[] buffer = Encoding.ASCII.GetBytes(str);
			Array.Resize(ref buffer, length);
			bw.Write(buffer);
		}
    }
}