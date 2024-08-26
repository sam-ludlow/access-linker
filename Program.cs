using System;
using System.Collections.Generic;

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

			if (Globals.Arguments.ContainsKey("DATABASE") == true && Globals.Arguments.ContainsKey("SERVER_SQL") == true)
			{
				Globals.SqlConnectionString = MakeConnectionStringSQL(Globals.Arguments["SERVER_SQL"], Globals.Arguments["DATABASE"]);

				if (Globals.Arguments.ContainsKey("SERVER_ODBC") == false)
					Globals.Arguments["SERVER_ODBC"] = Globals.Arguments["SERVER_SQL"];

				Globals.OdbcConnectionString = MakeConnectionStringODBC(Globals.Arguments["SERVER_ODBC"], Globals.Arguments["DATABASE"]);
			}

			Console.WriteLine($"SqlConnectionString:	{Globals.SqlConnectionString}");
			Console.WriteLine($"OdbcConnectionString:	{Globals.OdbcConnectionString}");

			Console.WriteLine();

			switch (Globals.Arguments["COMMAND"].ToUpper())
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
	}
}
