using System;
using System.IO;
using FraGag.Compression;
using HeroesONELib;

namespace HeroesONE
{
    static class Program
    {
		static LongOpt[] opts = {
							 new LongOpt("help", Argument.No, null, 'h'),
							 new LongOpt("no-compression", Argument.No, null, 'n'),
							 new LongOpt("pack", Argument.No, null, 'p'),
							 new LongOpt("unpack", Argument.No, null, 'u')
						 };

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
			Getopt getopt = new Getopt("KensSharp", args, Getopt.digest(opts), opts);
			Mode? mode = null;
			bool compress = true;
			int opt = getopt.getopt();
			while (opt != -1)
			{
				switch (opt)
				{
					case 'h':
						ShowHelp();
						return;
					case 'p':
						mode = Mode.Pack;
						break;
					case 'u':
						mode = Mode.Unpack;
						break;
					case 'n':
						compress = false;
						break;
				}
				opt = getopt.getopt();
			}
			if (mode == null || getopt.Optind + (mode == Mode.Unpack ? 0 : 1) >= args.Length)
			{
				ShowHelp();
				return;
			}
			string input = args[getopt.Optind];
            switch (mode.Value)
            {
                case Mode.Unpack:
                    try
                    {
                        HeroesONEFile one = new HeroesONEFile(input);
                        string dest = Environment.CurrentDirectory;
						if (getopt.Optind + 1 < args.Length)
                        {
                            dest = args[getopt.Optind + 1];
                            Directory.CreateDirectory(dest);
                        }
						foreach (HeroesONEFile.File item in one.Files)
							if (!compress)
								File.WriteAllBytes(Path.Combine(dest, item.Name), item.Data);
							else
								File.WriteAllBytes(Path.Combine(dest, item.Name), Prs.Decompress(item.Data));
					}
                    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                    break;
                case Mode.Pack:
                    string fn = args[args.Length - 1];
                    try
                    {
                        HeroesONEFile ar = new HeroesONEFile();
                        for (int i = getopt.Optind; i < args.Length - 1; i++)
                            ar.Files.Add(new HeroesONEFile.File(args[i]));
                        ar.Save(fn);
                    }
                    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                    break;
            }
        }
	
		static void ShowHelp()
		{
			Console.Write(Properties.Resources.HelpText);
		}
	}

	enum Mode
	{
		Pack,
		Unpack
	}
}