using System;

namespace access_linker
{
	internal class Program
	{
		static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("link    : access-linker.exe link <target.accdb> <database> <sql server> [odbc server]");
				Console.WriteLine("import  : access-linker.exe import <target.accdb> <database> <sql server> [odbc server]");
				Console.WriteLine("dump    : access-linker.exe dump <Target.accdb> <database> <sql server> [oledb access]");
				Console.WriteLine("backup  : access-linker.exe backup <filename.bak> <database> <sql server>");
				Console.WriteLine("restore : access-linker.exe restore <filename.bak> <database> <sql server> [directory]");
				Console.WriteLine("rename  : access-linker.exe rename <source name> <target name> <sql server> [directory]");
				Console.WriteLine("schema  : access-linker.exe schema <database> <sql server>");
				Console.WriteLine("encode  : access-linker.exe encode <EMPTY.accdb>");
				return;
			}

			string targetFilename = args[1];

			switch (args[0])
			{
				case "link":
					MsAccess.WriteEmptyAccess(targetFilename);
					MsAccess.TransferDatabase(targetFilename, args, "acLink");
					break;

				case "import":
					MsAccess.WriteEmptyAccess(targetFilename);
					MsAccess.TransferDatabase(targetFilename, args, "acImport");
					break;

				case "dump":
					MsAccess.WriteEmptyAccess(targetFilename);
					MsAccess.DumpAccess(targetFilename, args);
					break;

				case "restore":
					DataSQL.Restore(args);
					break;

				case "rename":
					DataSQL.Rename(args);
					break;

				case "backup":
					DataSQL.Backup(args[1], args[2], args[3]);
					break;

				case "schema":
					DataSQL.Schema(args[1], args[2]);
					break;

				case "encode":
					Tools.EncodeFile(targetFilename);
					break;

				default:
					Console.WriteLine($"Unknow command {args[0]}");
					break;
			}
		}
	}
}
