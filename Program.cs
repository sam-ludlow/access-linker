using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace access_linker
{
	internal class Program
	{
		private static Dictionary<string, string> arguments = new Dictionary<string, string>();

		[STAThread]
		static void Main(string[] args)
		{
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			string assemblyVersion = $"{version.Major}.{version.Minor}";

			Console.Title = $"access-linker {assemblyVersion}";

			string welcomeText = @"@VERSION
                                                                    __  __            __                           
                                                                    $$\ $$\           $$\                           
                                                                    $$ |\__|          $$ |                          
 $$$$$$\   $$$$$$$\  $$$$$$$\  $$$$$$\   $$$$$$$\  $$$$$$$\         $$ |$$\ $$$$$$$\  $$ |  $$\  $$$$$$\   $$$$$$\  
 \____$$\ $$  _____|$$  _____|$$  __$$\ $$  _____|$$  _____|$$$$$$\ $$ |$$ |$$  __$$\ $$ | $$  |$$  __$$\ $$  __$$\ 
 $$$$$$$ |$$ /      $$ /      $$$$$$$$ |\$$$$$$\  \$$$$$$\  \______|$$ |$$ |$$ |  $$ |$$$$$$  / $$$$$$$$ |$$ |  \__|
$$  __$$ |$$ |      $$ |      $$   ____| \____$$\  \____$$\         $$ |$$ |$$ |  $$ |$$  _$$<  $$   ____|$$ |      
\$$$$$$$ |\$$$$$$$\ \$$$$$$$\ \$$$$$$$\ $$$$$$$  |$$$$$$$  |        $$ |$$ |$$ |  $$ |$$ | \$$\ \$$$$$$$\ $$ |      
 \_______| \_______| \_______| \_______|\_______/ \_______/         \__|\__|\__|  \__|\__|  \__| \_______|\__|      

                                   See the README for more information
                               https://github.com/sam-ludlow/access-linker

";
			Console.WriteLine(welcomeText.Replace("@VERSION", assemblyVersion));

			string quickStart = "Select SQLite, text file containing ODBC connection string, or XML file";

			if (args.Length == 0)
			{
				Console.WriteLine(quickStart);

				OpenFileDialog openFileDialog = new OpenFileDialog
				{
					Title = $"Access Linker {assemblyVersion} - {quickStart}",
					Filter = "All (*.*)|*.*|SQLite (*.sqlite)|*.sqlite|ODBC string in text (*.txt)|*.txt|XML File (*.xml)|*.xml",
				};

				if (openFileDialog.ShowDialog() != DialogResult.OK)
					return;

				string sourceFilename = openFileDialog.FileName;

				switch (Path.GetExtension(sourceFilename).ToLower())
				{
					case ".txt":
						string file = File.ReadAllText(sourceFilename);
						if (file.Length == 0)
							file = Path.GetFileNameWithoutExtension(sourceFilename);

						string directory = Path.GetDirectoryName(sourceFilename);

						string[] lines = file.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

						if (lines.Length == 1)
						{
							Run(new string[] { "access-link-new", $"filename={Path.Combine(directory, Path.GetFileNameWithoutExtension(sourceFilename) + ".accdb")}", $"odbc={lines[0]}" });
							return;
						}

						foreach (string line in lines)
						{
							if (line.StartsWith("#") == true)
								continue;

							string connection = $"{Path.GetFileNameWithoutExtension(sourceFilename)}@{line}";
							string filename = Path.Combine(directory, connection + ".accdb");
							if (File.Exists(filename) == false)
								Run(new string[] { "access-link-new", $"filename={filename}", $"odbc={connection}" });
						}
						return;

					case ".xml":
						args = new string[] { "xml-insert-new", $"filename={sourceFilename}" };
						break;

					default:
						args = new string[] { "access-link-new", $"filename={sourceFilename + ".accdb"}", $"odbc={sourceFilename}" };
						break;
				}
			}

			Run(args);
		}

		static void Run(string[] args)
		{
			arguments.Clear();

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

			if (arguments.ContainsKey("mssql") == true)
				arguments["mssql"] = Tools.MakeConnectionStringMsSQL(arguments["mssql"]);

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

				case "access-schema":
					ValidateRequiredParameters("filename");
					MsAccess.Schema(arguments["filename"]);
					break;

				case "access-link":
					ValidateRequiredParameters("filename", "odbc");
					MsAccess.Link(arguments["filename"], arguments["odbc"]);
					break;

				case "access-link-new":
					ValidateRequiredParameters("filename", "odbc");
					MsAccess.Delete(arguments["filename"]);
					MsAccess.Create(arguments["filename"]);
					MsAccess.Link(arguments["filename"], arguments["odbc"]);
					break;

				case "access-import":
					ValidateRequiredParameters("filename", "odbc");
					MsAccess.Import(arguments["filename"], arguments["odbc"]);
					break;

				case "access-import-new":
					MsAccess.Delete(arguments["filename"]);
					MsAccess.Create(arguments["filename"]);
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


				//
				// Excel
				//
				case "excel-export":
					ValidateRequiredParameters("filename");
					MsAccess.ExcelExport(arguments["filename"]);
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

				case "sqlite-schema":
					ValidateRequiredParameters("odbc");
					using (OdbcConnection connection = new OdbcConnection(arguments["odbc"]))
						Tools.PopText(SQLite.SchemaPragma(connection));

					break;


				//
				// SQL Server
				//
				case "mssql-delete":
					ValidateRequiredParameters("mssql", "name");
					MsSQL.Delete(arguments["mssql"], arguments["name"]);
					break;

				case "mssql-create":
					ValidateRequiredParameters("mssql", "name");
					MsSQL.Create(arguments["mssql"], arguments["name"], null, null);
					break;

				case "mssql-schema-ansi":
					ValidateRequiredParameters("mssql");
					Tools.PopText(MsSQL.SchemaAnsi(arguments["mssql"]));
					break;

				case "mssql-databases":
					ValidateRequiredParameters("mssql");
					Tools.PopText(String.Join(Environment.NewLine, MsSQL.Databases(arguments["mssql"])));
					break;

				case "mssql-backup":
					ValidateRequiredParameters("filename", "mssql", "name");
					MsSQL.Backup(arguments["filename"], arguments["mssql"], arguments["name"], null);
					break;

				case "mssql-restore":
					ValidateRequiredParameters("filename", "mssql", "name");
					MsSQL.Restore(arguments["filename"], arguments["mssql"], arguments["name"], null, null, null);
					break;



				//
				// OBDC
				//
				case "odbc-schema":
					ValidateRequiredParameters("odbc");
					using (OdbcConnection connection = new OdbcConnection(arguments["odbc"]))
						Tools.PopText(Tools.SchemaConnection(connection));
					break;

				//
				// XML
				//
				case "xml-insert-new":
					ValidateRequiredParameters("filename");

					string xmlFilename = arguments["filename"];
					string targetFilename = xmlFilename + ".accdb";

					MsAccess.Delete(targetFilename);
					MsAccess.Create(targetFilename);
					XML.InsertAccess(xmlFilename, targetFilename);
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
