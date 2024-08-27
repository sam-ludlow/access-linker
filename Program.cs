using System;
using System.Collections.Generic;
using System.IO;

namespace access_linker
{
	internal class Program
	{
		static void Main(string[] args)
		{
			foreach (string arg in args)
			{
					int index = arg.IndexOf('=');
					if (index == -1)
						throw new ApplicationException("Bad argument expecting KEY=VALUE got: " + arg);

					Globals.Arguments.Add(arg.Substring(0, index).ToUpper(), arg.Substring(index + 1));
			}

			Console.WriteLine();

			foreach (string key in Globals.Arguments.Keys)
				Console.WriteLine($"{key}\t{Globals.Arguments[key]}");

			Console.WriteLine();

			if (Globals.Arguments.ContainsKey("COMMAND") == false)
			{
				Console.WriteLine(" !!! USAGE !!! access-linker.exe ");
				return;
			}

			string command = Globals.Arguments["COMMAND"].ToUpper();

			if (Globals.Arguments.ContainsKey("DATABASE") == true && Globals.Arguments.ContainsKey("SERVER_SQL") == true)
			{
				Globals.SqlConnectionString = MakeConnectionStringSQL(Globals.Arguments["SERVER_SQL"], Globals.Arguments["DATABASE"]);

				if (Globals.Arguments.ContainsKey("SERVER_ODBC") == false)
					Globals.Arguments["SERVER_ODBC"] = Globals.Arguments["SERVER_SQL"];

				Globals.OdbcConnectionString = MakeConnectionStringODBC(Globals.Arguments["SERVER_ODBC"], Globals.Arguments["DATABASE"]);
			}

			if (Globals.Arguments.ContainsKey("FILENAME") == true)
			{
				if (Globals.Arguments.ContainsKey("SERVER_OLEDB") == false)
					Globals.Arguments["SERVER_OLEDB"] = Globals.Arguments["FILENAME"];

				Globals.OleDbConnectionString = MakeConnectionStringOLEDB(Globals.Arguments["SERVER_OLEDB"]);
			}

			Console.WriteLine($"SqlConnectionString:	{Globals.SqlConnectionString}");
			Console.WriteLine($"OdbcConnectionString:	{Globals.OdbcConnectionString}");
			Console.WriteLine($"OleDbConnectionString:	{Globals.OleDbConnectionString}");

			Console.WriteLine();

			switch (command)
			{
				case "ACCESS_CREATE":
					ValidateRequiredParameters(new string[] { "FILENAME" });
					MsAccess.Create(Globals.Arguments["FILENAME"]);
					break;

				case "ACCESS_DELETE":
					ValidateRequiredParameters(new string[] { "FILENAME" });
					MsAccess.Delete(Globals.Arguments["FILENAME"]);
					break;

				case "ACCESS_LINK":
					ValidateRequiredParameters(new string[] { "FILENAME", "DATABASE", "SERVER_SQL", "SERVER_ODBC" });
					MsAccess.Link(Globals.Arguments["FILENAME"], Globals.SqlConnectionString, Globals.OdbcConnectionString);
					break;

				case "ACCESS_IMPORT":
					ValidateRequiredParameters(new string[] { "FILENAME", "DATABASE", "SERVER_SQL", "SERVER_ODBC" });
					MsAccess.Import(Globals.Arguments["FILENAME"], Globals.SqlConnectionString, Globals.OdbcConnectionString);
					break;

				case "ACCESS_EXPORT":
					ValidateRequiredParameters(new string[] { "FILENAME", "DATABASE", "SERVER_OLEDB", "SERVER_ODBC" });
					MsAccess.Export(Globals.Arguments["FILENAME"], Globals.OleDbConnectionString, Globals.OdbcConnectionString);
					break;




				case "SQL_CREATE":
					ValidateRequiredParameters(new string[] { "DATABASE", "SERVER_SQL" });

					Globals.SqlConnectionString = MakeConnectionStringSQL(Globals.Arguments["SERVER_SQL"], null);

					string dataDirectory = Globals.Arguments.ContainsKey("SQL_DATA_DIRECTORY") == true ? Globals.Arguments["SQL_DATA_DIRECTORY"] : null;
					string logDirectory = Globals.Arguments.ContainsKey("SQL_LOG_DIRECTORY") == true ? Globals.Arguments["SQL_LOG_DIRECTORY"] : null;
					if (dataDirectory != null && logDirectory == null)
						logDirectory = dataDirectory;

					DataSQL.Create(Globals.SqlConnectionString, Globals.Arguments["DATABASE"], dataDirectory, logDirectory);
					break;

				case "SQL_DELETE":
					ValidateRequiredParameters(new string[] { "DATABASE", "SERVER_SQL" });

					Globals.SqlConnectionString = MakeConnectionStringSQL(Globals.Arguments["SERVER_SQL"], null);

					DataSQL.Delete(Globals.SqlConnectionString, Globals.Arguments["DATABASE"]);
					break;


				default:
					Console.WriteLine($" !!! access-linker.exe Unknow command {Globals.Arguments["COMMAND"]}");
					break;
			}
		}

		private static void ValidateRequiredParameters(string[] names)
		{
			List<string> missing = new List<string>();

			foreach (string name in names)
				if (Globals.Arguments.ContainsKey(name) == false)
					missing.Add(name);

			if (missing.Count > 0)
				throw new ApplicationException($"This command requires these parameters '{String.Join(", ", missing)}'.");

		}

		public static string MakeConnectionStringSQL(string server, string database)
		{
			if (server.Contains(";") == false)
				server = $"Data Source='{server}';Integrated Security=True;TrustServerCertificate=True;";

			if (database != null)
				server += $"Initial Catalog='{database}';";

			return server;
		}

		public static string MakeConnectionStringODBC(string server, string database)
		{
			if (server.Contains(";") == false)
				server = $"ODBC;Driver={{ODBC Driver 17 for SQL Server}};SERVER={server};Trusted_Connection=Yes;";

			if (database != null)
				server += $"DATABASE={database};";

			return server;
		}

		public static string MakeConnectionStringOLEDB(string accessFilename)
		{
			if (accessFilename.Contains(";") == false)
				accessFilename = $"Provider='Microsoft.ACE.OLEDB.16.0';User ID='Admin';Password='';Data Source='{accessFilename}';";

			string systemDatabaseFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Access", "System.mdw");

			if (File.Exists(systemDatabaseFilename) == false)
				throw new ApplicationException($"Microsoft Access System database missing: '{systemDatabaseFilename}'.");

			accessFilename += $"Jet OLEDB:System Database='{systemDatabaseFilename}';";

			return accessFilename;
		}
	}
}
