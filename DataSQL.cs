using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Data;

namespace access_linker
{
	public class DataSQL
	{
		public static string MakeConnectionStringSQL(string server, string database)
		{
			if (server.Contains(";") == false)
				server = $"Data Source='{server}';Integrated Security=True;TrustServerCertificate=True;";

			if (database != null)
				server += $"Initial Catalog='{database}';";

			Console.WriteLine($"SQL Connection: {server}");

			return server;
		}

		public static void Restore(string[] args)
		{
			string filename = args[1];
			string database = args[2];
			string connectionString = args[3];
			string directory = null;
			if (args.Length >= 5)
				directory = args[4];

			using (SqlConnection serverConnection = new SqlConnection(MakeConnectionStringSQL(connectionString, null)))
			{
				if (DatabaseExists(serverConnection, database) == true)
					throw new ApplicationException("Database exists");

				Console.Write($"Verify BAK {filename} ...");
				ExecuteNonQuery(serverConnection, $"RESTORE VERIFYONLY FROM DISK = '{filename}'");
				Console.WriteLine("...done");

				if (directory == null)
					directory = (string)ExecuteScalar(serverConnection, "SELECT SERVERPROPERTY('InstanceDefaultDataPath')");

				while (directory.EndsWith(@"\") == true)
					directory = directory.Substring(0, directory.Length - 1);

				Console.WriteLine($"SQL directory: {directory}");

				DataTable filesTable = ExecuteFill(serverConnection, $"RESTORE FILELISTONLY FROM DISK = '{filename}'").Tables[0];

				if (filesTable.Rows.Count != 2)
					throw new ApplicationException("This only works with one ROWS and one LOG file");

				filesTable.PrimaryKey = new DataColumn[] { filesTable.Columns["Type"] };

				DataRow rowsRow = filesTable.Rows.Find("D");
				DataRow logRow = filesTable.Rows.Find("L");

				if (rowsRow == null || logRow == null)
					throw new ApplicationException("Did not find the 2 file rows.");

				string rowsBackupLogicalName = (string)rowsRow["LogicalName"];
				string logBackupLogicalName = (string)logRow["LogicalName"];

				string rowsPhysicalName = Path.Combine(directory, $"{database}.mdf");
				string logPhysicalName = Path.Combine(directory, $"{database}_log.ldf");

				Console.Write($"Restore BAK {filename} ...");
				ExecuteNonQuery(serverConnection, $"RESTORE DATABASE [{database}] FROM DISK='{filename}' WITH " +
					$"MOVE '{rowsBackupLogicalName}' TO '{rowsPhysicalName}', " +
					$"MOVE '{logBackupLogicalName}' TO '{logPhysicalName}'");
				Console.WriteLine("...done");

				string rowsLogicalName = database;
				string logLogicalName = database + "_log";

				if (rowsBackupLogicalName.ToLower() != rowsLogicalName.ToLower())
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE[{database}] MODIFY FILE(NAME= '{rowsBackupLogicalName}', NEWNAME= '{rowsLogicalName}')");

				if (logBackupLogicalName.ToLower() != logLogicalName.ToLower())
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE[{database}] MODIFY FILE(NAME= '{logBackupLogicalName}', NEWNAME= '{logLogicalName}')");

				using (SqlConnection databaseConnection = new SqlConnection(MakeConnectionStringSQL(connectionString, database)))
				{
					Console.Write($"Shrinking LDF {logPhysicalName} ...");
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{database}] SET RECOVERY SIMPLE");
					ExecuteNonQuery(databaseConnection, $"DBCC SHRINKFILE ({logLogicalName}, 1)");
					ExecuteNonQuery(serverConnection, $"ALTER DATABASE [{database}] SET RECOVERY FULL");
					Console.WriteLine("...done");
				}
			}
		}

		public static void Rename(string[] args)
		{
			string databaseSource = args[1];
			string databaseTarget = args[2];
			string connectionString = args[3];

			string directoryData = null;
			if (args.Length >= 5)
				directoryData = args[4];
			string directoryLogs = directoryData;
			if (args.Length >= 6)
				directoryLogs = args[5];

			using (SqlConnection serverConnection = new SqlConnection(MakeConnectionStringSQL(connectionString, null)))
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
					DataTable filesTable = ExecuteFill(databaseConnection, "SELECT * FROM sys.database_files").Tables[0];

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

		public static void Schema(string database, string server)
		{
			using (SqlConnection databaseConnection = new SqlConnection(MakeConnectionStringSQL(server, database)))
			{
				DataSet schema = GetInformationSchemas(databaseConnection);
				Tools.PopText(schema);
			}
		}

		public static void Backup(string filename, string database, string server)
		{
			using (SqlConnection connection = new SqlConnection(MakeConnectionStringSQL(server, null)))
			{
				using (SqlCommand command = new SqlCommand("BACKUP DATABASE @database TO DISK=@disk WITH NO_COMPRESSION", connection))
				{
					command.CommandTimeout = 24 * 60 * 60;

					command.Parameters.AddWithValue("@database", database);
					command.Parameters.AddWithValue("@disk", filename);

					ExecuteNonQuery(command);
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

		public static string[] ListDatabaseTables(string connectionString)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
				return ListDatabaseTables(connection);
		}

		public static string[] ListDatabaseTables(SqlConnection connection)
		{
			List<string> result = new List<string>();

			DataTable table = DataSQL.ExecuteFill(connection,
				"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME").Tables[0];

			foreach (DataRow row in table.Rows)
				result.Add((string)row["TABLE_NAME"]);

			return result.ToArray();
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

		public static DataSet ExecuteFill(SqlConnection connection, string commandText)
		{
			DataSet dataSet = new DataSet();

			using (SqlDataAdapter adapter = new SqlDataAdapter(commandText, connection))
				adapter.Fill(dataSet);

			return dataSet;
		}
	}
}
