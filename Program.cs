using System;
using System.Collections.Generic;
using System.Linq;

namespace access_linker
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Dictionary<string, string> parameters = new Dictionary<string, string>();

			if (args.Length > 1)
			{
				foreach (string arg in args.Skip(1))
				{
					int index = arg.IndexOf('=');
					if (index == -1)
						throw new ApplicationException("Bad argument expecting KEY=VALUE got: " + arg);

					parameters.Add(arg.Substring(0, index).ToUpper(), arg.Substring(index + 1));
				}
			}

			Console.WriteLine();

			foreach (string key in parameters.Keys)
				Console.WriteLine($"{key}\t{parameters[key]}");

			Console.WriteLine();

			if (args.Length < 2)
			{
				//	 TO UPDATE
				Console.WriteLine("USAGE !!!");

				//	SELECT SERVERPROPERTY('InstanceDefaultDataPath') AS InstanceDefaultDataPath, SERVERPROPERTY('InstanceDefaultLogPath') AS InstanceDefaultLogPath

				//	COMMAND

				//	FILENAME
				//	FILENAME	(BAK)

				//	DATABASE
				//	DATABASE_DIR
				//	DATABASE_LOG_DIR

				//	WITH ???

				//	SERVER
				//	SERVER_ODBC
				//	ACCESS_OLEDB

				return;
			}

		

			switch (args[0].ToLower())
			{
				case "link":
					MsAccess.Link(parameters);
					break;

				case "import":
					MsAccess.Import(parameters);
					break;

				case "export":
					MsAccess.Export(parameters);
					break;

				case "dump":
					MsAccess.Dump(parameters);
					break;

				case "backup":
					DataSQL.Backup(parameters);
					break;

				//case "verify":
				//	DataSQL.BackupVerify(args[1], args[2]);
				//	break;

				//case "list":
				//	DataSQL.BackupFileList(args[1], args[2]);
				//	break;

				//case "restore":
				//	DataSQL.Restore(args);
				//	break;

				//case "rename":
				//	DataSQL.Rename(args);
				//	break;



				//case "empty":
				//	if (args.Length == 2)
				//		MsAccess.WriteEmptyAccess(targetFilename);
				//	else
				//		DataSQL.Empty(args[1], args[2]);	//	TODO: file location
				//	break;

				//case "schema":
				//	DataSQL.Schema(args[1], args[2]);
				//	break;

				//case "encode":
				//	Tools.EncodeFile(targetFilename);
				//	break;

				default:
					Console.WriteLine($"Unknow command {args[0]}");
					break;
			}
		}
	}
}
