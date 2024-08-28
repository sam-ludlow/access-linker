using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Data;

namespace access_linker
{
	public class DataSQL
	{



		public static void Create(string connectionString, string database, string directoryData, string directoryLogs)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				string commandText = $"CREATE DATABASE [{database}]";

				if (directoryData != null)
				{
					string dataName = database;
					string logName = database + "_log";

					string mdfFilename = Path.Combine(directoryData, dataName + ".mdf");
					string ldfFilename = Path.Combine(directoryLogs, logName + ".ldf");

					commandText += $" ON (NAME = '{dataName}', FILENAME = '{mdfFilename}') LOG ON (NAME = '{logName}', FILENAME = '{ldfFilename}')";
				}

				ExecuteNonQuery(connection, commandText);
			}
		}


		public static void Delete(string connectionString, string database)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				if (DatabaseExists(connection, database) == true)
				{
					ExecuteNonQuery(connection, $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
					ExecuteNonQuery(connection, $"DROP DATABASE [{database}]");
				}
			}
		}

		public static bool DatabaseExists(SqlConnection connection, string name)
		{
			using (SqlCommand command = new SqlCommand("SELECT name FROM sys.databases WHERE name = @name", connection))
			{
				command.Parameters.AddWithValue("@name", name);

				object obj = ExecuteScalar(command);

				if (obj == null || obj is DBNull)
					return false;

				return true;
			}
		}

		public static DataSet Schema(string sqlConnectionString)
		{
			DataSet dataSet = new DataSet();

			using (SqlConnection connection = new SqlConnection(sqlConnectionString))
			{
				connection.Open();
				try
				{
					List<string> collectionNames = new List<string>();
					foreach (DataRow row in connection.GetSchema().Rows)
						collectionNames.Add((string)row["CollectionName"]);
					collectionNames.Sort();

					foreach (string collectionName in collectionNames)
					{
						DataTable table = connection.GetSchema(collectionName);
						table.TableName = collectionName;
						dataSet.Tables.Add(table);
					}
				}
				finally
				{
					connection.Close();
				}
			}

			return dataSet;
		}

		public static DataSet SchemaANSI(string sqlConnectionString)
		{
			using (SqlConnection connection = new SqlConnection(sqlConnectionString))
				return GetInformationSchemas(connection);
		}

		public static DataSet GetInformationSchemas(SqlConnection connection)
		{
			string[] informationSchemaNames = {
				"CHECK_CONSTRAINTS",
				"COLUMN_DOMAIN_USAGE",
				"COLUMN_PRIVILEGES",
				"COLUMNS",
				"CONSTRAINT_COLUMN_USAGE",
				"CONSTRAINT_TABLE_USAGE",
				"DOMAIN_CONSTRAINTS",
				"DOMAINS",
				"KEY_COLUMN_USAGE",
				"PARAMETERS",
				"REFERENTIAL_CONSTRAINTS",
				"ROUTINE_COLUMNS",
				"ROUTINES",
				"SCHEMATA",
				"TABLE_CONSTRAINTS",
				"TABLE_PRIVILEGES",
				"TABLES",
				"VIEW_COLUMN_USAGE",
				"VIEW_TABLE_USAGE",
				"VIEWS",
			};

			DataSet dataSet = new DataSet("INFORMATION_SCHEMA");

			foreach (string name in informationSchemaNames)
			{
				using (SqlDataAdapter adapter = new SqlDataAdapter($"SELECT * FROM [{dataSet.DataSetName}].[{name}]", connection))
				{
					DataTable table = new DataTable(name);
					adapter.Fill(table);
					dataSet.Tables.Add(table);
				}
			}

			return dataSet;
		}









		public static void Backup(Dictionary<string, string> arguments)
		{
			Tools.RequiredArguments(arguments, new string[] { "FILENAME", "DATABASE", "SERVER" });

			string filename = arguments["FILENAME"];
			string databaseName = arguments["DATABASE"];
			string connectionString = arguments["SERVER"];
			string with = arguments["WITH"];

			connectionString = MakeConnectionStringSQL(connectionString, null);

			Backup(filename, connectionString, databaseName, with);
		}

		public static void Verify(Dictionary<string, string> arguments)
		{
			Tools.RequiredArguments(arguments, new string[] { "FILENAME", "SERVER" });

			string filename = arguments["FILENAME"];
			string connectionString = arguments["SERVER"];

			connectionString = MakeConnectionStringSQL(connectionString, null);

			BackupVerify(filename, connectionString);
		}

		public static void List(Dictionary<string, string> arguments)
		{
			Tools.RequiredArguments(arguments, new string[] { "FILENAME", "SERVER" });

			string filename = arguments["FILENAME"];
			string connectionString = arguments["SERVER"];

			connectionString = MakeConnectionStringSQL(connectionString, null);

			BackupFileList(filename, connectionString);
		}

		public static void Restore(Dictionary<string, string> arguments)
		{
			Tools.RequiredArguments(arguments, new string[] { "FILENAME", "DATABASE", "SERVER" });

			string filename = arguments["FILENAME"];
			string databaseName = arguments["DATABASE"];
			string connectionString = arguments["SERVER"];
			string with = arguments["WITH"];

			string directoryMDF = arguments["DIRECTORY"];
			string directoryLDF = arguments["LOG_DIRECTORY"];

			if (directoryLDF == null)
				directoryLDF = directoryMDF;

			connectionString = MakeConnectionStringSQL(connectionString, null);

			Restore(filename, connectionString, databaseName, with, directoryMDF, directoryLDF);
		}

		public static void Rename(Dictionary<string, string> arguments)
		{
			Tools.RequiredArguments(arguments, new string[] { "FILENAME", "DATABASE", "SERVER" });

			string databaseName = arguments["DATABASE"];
			string newDatabaseName = arguments["NEW_DATABASE"];
			string connectionString = arguments["SERVER"];

			string directoryMDF = arguments["DIRECTORY"];
			string directoryLDF = arguments["LOG_DIRECTORY"];

			if (directoryLDF == null)
				directoryLDF = directoryMDF;

			connectionString = MakeConnectionStringSQL(connectionString, null);

			Rename(connectionString, databaseName, newDatabaseName, directoryMDF, directoryLDF);
		}

		public static void Create(Dictionary<string, string> arguments)
		{
			Tools.RequiredArguments(arguments, new string[] { "DATABASE", "SERVER" });

			string databaseName = arguments["DATABASE"];
			string connectionString = arguments["SERVER"];

			string directoryMDF = arguments["DIRECTORY"];
			string directoryLDF = arguments["LOG_DIRECTORY"];

			if (directoryLDF == null)
				directoryLDF = directoryMDF;

			connectionString = MakeConnectionStringSQL(connectionString, null);

			Create(connectionString, databaseName, directoryMDF, directoryLDF);
		}

		public static string MakeConnectionStringSQL(string server, string database)
		{
			if (server.Contains(";") == false)
				server = $"Data Source='{server}';Integrated Security=True;TrustServerCertificate=True;";

			if (database != null)
				server += $"Initial Catalog='{database}';";

			return server;
		}

		public static void Restore(string filename, string connectionString, string database, string with, string directoryMDF, string directoryLDF)
		{
			using (SqlConnection serverConnection = new SqlConnection(connectionString))
			{
				if (DatabaseExists(serverConnection, database) == true)
					throw new ApplicationException("Database exists");

				if (directoryMDF == null)
					directoryMDF = (string)ExecuteScalar(serverConnection, "SELECT SERVERPROPERTY('InstanceDefaultDataPath')");

				if (directoryLDF == null)
					directoryLDF = (string)ExecuteScalar(serverConnection, "SELECT SERVERPROPERTY('InstanceDefaultLogPath')");

				directoryMDF = directoryMDF.Trim(new char[] { '\\' });
				directoryLDF = directoryLDF.Trim(new char[] { '\\' });

				Console.WriteLine($"DATA directory: {directoryMDF}");
				Console.WriteLine($"LOG directory: {directoryLDF}");

				DataTable filesTable = ExecuteFill(serverConnection, $"RESTORE FILELISTONLY FROM DISK = '{filename}'");

				if (filesTable.Rows.Count != 2)
					throw new ApplicationException("This only works with one ROWS and one LOG file");

				filesTable.PrimaryKey = new DataColumn[] { filesTable.Columns["Type"] };

				DataRow rowsRow = filesTable.Rows.Find("D");
				DataRow logRow = filesTable.Rows.Find("L");

				if (rowsRow == null || logRow == null)
					throw new ApplicationException("Did not find the 2 file rows.");

				string rowsBackupLogicalName = (string)rowsRow["LogicalName"];
				string logBackupLogicalName = (string)logRow["LogicalName"];

				string rowsPhysicalName = Path.Combine(directoryMDF, $"{database}.mdf");
				string logPhysicalName = Path.Combine(directoryLDF, $"{database}_log.ldf");

				string commandText = $"RESTORE DATABASE [{database}] FROM DISK='{filename}' WITH " +
					$"MOVE '{rowsBackupLogicalName}' TO '{rowsPhysicalName}', " +
					$"MOVE '{logBackupLogicalName}' TO '{logPhysicalName}'";

				if (with != null)
					commandText += $", {with}";

				Console.Write($"Restore BAK {filename} ...");
				ExecuteNonQuery(serverConnection, commandText);
				Console.WriteLine("...done");

				string rowsLogicalName = database;
				string logLogicalName = database + "_log";

				if (rowsBackupLogicalName.ToLower() != rowsLogicalName.ToLower())
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE[{database}] MODIFY FILE(NAME= '{rowsBackupLogicalName}', NEWNAME= '{rowsLogicalName}')");

				if (logBackupLogicalName.ToLower() != logLogicalName.ToLower())
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE[{database}] MODIFY FILE(NAME= '{logBackupLogicalName}', NEWNAME= '{logLogicalName}')");

				//using (SqlConnection databaseConnection = new SqlConnection(MakeConnectionStringSQL(connectionString, database)))
				//{
				//	Console.Write($"Shrinking LDF {logPhysicalName} ...");
				//	ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{database}] SET RECOVERY SIMPLE");
				//	ExecuteNonQuery(databaseConnection, $"DBCC SHRINKFILE ({logLogicalName}, 1)");
				//	ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{database}] SET RECOVERY FULL");
				//	Console.WriteLine("...done");
				//}
			}
		}

		public static void BackupVerify(string filename, string connectionString)
		{
			using (SqlConnection serverConnection = new SqlConnection(connectionString))
			{
				ExecuteNonQuery(serverConnection, $"RESTORE VERIFYONLY FROM DISK = '{filename}'");
			}
		}

		public static void BackupFileList(string filename, string connectionString)
		{
			DataTable table;

			using (SqlConnection serverConnection = new SqlConnection(connectionString))
			{
				table = ExecuteFill(serverConnection, $"RESTORE FILELISTONLY FROM DISK = '{filename}'");
			}

			Tools.PopText(table);
		}

		public static void Rename(string connectionString, string databaseSource, string databaseTarget, string directoryData, string directoryLogs)
		{
			using (SqlConnection serverConnection = new SqlConnection(connectionString))
			{
				if (DatabaseExists(serverConnection, databaseSource) == false)
					throw new ApplicationException("Database Source does not exist");

				if (DatabaseExists(serverConnection, databaseTarget) == true)
					throw new ApplicationException("Target Database exists");

				ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{databaseSource}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
				ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{databaseSource}] MODIFY NAME = [{databaseTarget}]");
				ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{databaseTarget}] SET MULTI_USER WITH ROLLBACK IMMEDIATE;");

				using (SqlConnection databaseConnection = new SqlConnection(MakeConnectionStringSQL(connectionString, databaseTarget)))
				{
					DataTable filesTable = ExecuteFill(databaseConnection, "SELECT * FROM sys.database_files");

					if (filesTable.Rows.Count != 2)
						throw new ApplicationException("This only works with one ROWS and one LOG database");

					filesTable.PrimaryKey = new DataColumn[] { filesTable.Columns["type_desc"] };

					DataRow rowsRow = filesTable.Rows.Find("ROWS");
					DataRow logRow = filesTable.Rows.Find("LOG");

					if (rowsRow == null || logRow == null)
						throw new ApplicationException("Did not find the 2 file rows.");

					string rowsLogicalName = (string)rowsRow["name"];
					string logLogicalName = (string)logRow["name"];

					string rowsPhysicalName = (string)rowsRow["physical_name"];
					string logPhysicalName = (string)logRow["physical_name"];

					string rowsNewName = databaseTarget;
					string logNewName = databaseTarget + "_log";

					if (directoryData == null)
						directoryData = Path.GetDirectoryName(rowsPhysicalName);

					if (directoryLogs == null)
						directoryLogs = Path.GetDirectoryName(logPhysicalName);

					string rowsNewPhysicalName = Path.Combine(directoryData, rowsNewName + ".mdf");
					string logNewPhysicalName = Path.Combine(directoryLogs, logNewName + ".ldf");

					// rename logical
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE[{databaseTarget}] MODIFY FILE(NAME= '{rowsLogicalName}', NEWNAME= '{rowsNewName}')");
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE[{databaseTarget}] MODIFY FILE(NAME= '{logLogicalName}', NEWNAME= '{logNewName}')");

					// rename physical
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{databaseTarget}] MODIFY FILE(NAME = '{rowsNewName}', FILENAME = '{rowsNewPhysicalName}')");
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{databaseTarget}] MODIFY FILE(NAME = '{logNewName}', FILENAME = '{logNewPhysicalName}')");

					// rename filesystem
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{databaseTarget}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{databaseTarget}] SET OFFLINE");

					Console.Write($"Moving MDF {rowsPhysicalName} => {rowsNewPhysicalName} ...");
					File.Move(rowsPhysicalName, rowsNewPhysicalName);
					Console.WriteLine("...done");

					Console.Write($"Moving LDF {logPhysicalName} => {logNewPhysicalName} ...");
					File.Move(logPhysicalName, logNewPhysicalName);
					Console.WriteLine("...done");

					ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{databaseTarget}] SET ONLINE");
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{databaseTarget}] SET MULTI_USER WITH ROLLBACK IMMEDIATE");
				}
			}
		}


		public static void Backup(string filename, string connectionString, string databaseName, string with)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				string commandText = "BACKUP DATABASE @database TO DISK=@disk";

				if (with != null)
					commandText += $" WITH {with}";

				using (SqlCommand command = new SqlCommand(commandText, connection))
				{
					command.CommandTimeout = 24 * 60 * 60;

					command.Parameters.AddWithValue("@database", databaseName);
					command.Parameters.AddWithValue("@disk", filename);

					ExecuteNonQuery(command);
				}
			}
		}







		public static string[] ListDatabaseTables(string connectionString)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
				return ListDatabaseTables(connection);
		}

		public static string[] ListDatabaseTables(SqlConnection connection)
		{
			List<string> result = new List<string>();

			DataTable table = DataSQL.ExecuteFill(connection,
				"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME");

			foreach (DataRow row in table.Rows)
				result.Add((string)row["TABLE_NAME"]);

			return result.ToArray();
		}


		public static int ExecuteNonQuery(SqlConnection connection, string commandText)
		{
			using (SqlCommand command = new SqlCommand(commandText, connection))
			{
				command.CommandTimeout = 24 * 60 * 60;

				return ExecuteNonQuery(command);
			}
		}

		public static int ExecuteNonQuery(SqlCommand command)
		{
			Console.WriteLine($"ExecuteNonQuery: {command.CommandText}");

			command.Connection.Open();
			try
			{
				return command.ExecuteNonQuery();
			}
			finally
			{
				command.Connection.Close();
			}
		}

		public static object ExecuteScalar(SqlConnection connection, string commandText)
		{
			using (SqlCommand command = new SqlCommand(commandText, connection))
				return ExecuteScalar(command);
		}

		public static object ExecuteScalar(SqlCommand command)
		{
			command.Connection.Open();
			try
			{
				return command.ExecuteScalar();
			}
			finally
			{
				command.Connection.Close();
			}
		}

		public static DataTable ExecuteFill(SqlConnection connection, string commandText)
		{
			DataTable table = new DataTable();
			using (SqlDataAdapter adapter = new SqlDataAdapter(commandText, connection))
				adapter.Fill(table);
			return table;
		}
	}
}
