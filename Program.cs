using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.IO;

namespace access_linker
{
	internal class Program
	{
		private static Dictionary<string, string> arguments = new Dictionary<string, string>();

		static void Main(string[] args)
		{
			if (args.Length > 0 && args[0].Contains("=") == false)
				args[0] = $"command={args[0]}";

			foreach (string arg in args)
			{
				int index = arg.IndexOf('=');
				if (index == -1)
					throw new ApplicationException($"Bad argument expecting key=value: {arg}");

				arguments.Add(arg.Substring(0, index).ToLower(), arg.Substring(index + 1));
			}

			foreach (string key in arguments.Keys)
				Console.WriteLine($"{key}\t{arguments[key]}");

			if (arguments.ContainsKey("command") == false)
				throw new ApplicationException("Bad usage");

			if (arguments.ContainsKey("odbc") == true)
				arguments["odbc"] = Tools.MakeConnectionStringODBC(arguments["odbc"]);

			switch (arguments["command"])
			{
				//
				//	MS Access
				//
				case "access-delete":
					ValidateRequiredParameters("filename");
					MsAccess.Delete(arguments["filename"]);
					break;

				case "access-create":
					ValidateRequiredParameters("filename");
					MsAccess.Create(arguments["filename"]);
					break;

				case "access-link":
					ValidateRequiredParameters("filename", "odbc");
					MsAccess.Link(arguments["filename"], arguments["odbc"]);
					break;

				case "access-import":
					ValidateRequiredParameters("filename", "odbc");
					MsAccess.Import(arguments["filename"], arguments["odbc"]);
					break;

				case "access-export":
					ValidateRequiredParameters("filename", "odbc");
					MsAccess.Export(arguments["filename"], arguments["odbc"]);
					break;

				case "access-insert":
					ValidateRequiredParameters("filename", "odbc");
					MsAccess.Insert(arguments["filename"], arguments["odbc"]);
					break;

				case "access-insert-new":
					ValidateRequiredParameters("filename", "odbc");
					MsAccess.Delete(arguments["filename"]);
					MsAccess.Create(arguments["filename"]);
					MsAccess.Insert(arguments["filename"], arguments["odbc"]);
					break;

				case "access-schema":
					ValidateRequiredParameters("filename");
					MsAccess.Schema(arguments["filename"]);
					break;


				//
				// SQLite
				//
				case "sqlite-delete":
					ValidateRequiredParameters("filename");
					File.Delete($"{arguments["filename"]}-journal");
					File.Delete(arguments["filename"]);
					break;

				case "sqlite-create":
					ValidateRequiredParameters("filename");
					File.WriteAllBytes(arguments["filename"], new byte[0]);
					break;




				//
				// SQL Server
				//



				//
				// OBDC
				//
				case "odbc-schema":
					ValidateRequiredParameters("odbc");
					DataSet schema;
					using (OdbcConnection connection = new OdbcConnection(arguments["odbc"]))
						schema = Tools.SchemaConnection(connection);
					Tools.PopText(schema);
					break;


				default:
					throw new ApplicationException($"Unknown command: {arguments["command"]}");
			}
		}

		private static void ValidateRequiredParameters(params string[] names)
		{
			List<string> missing = new List<string>();

			foreach (string name in names)
				if (arguments.ContainsKey(name) == false)
					missing.Add(name);

			if (missing.Count > 0)
				throw new ApplicationException($"This command requires these parameters '{String.Join(", ", missing)}'.");

		}

	}
}
