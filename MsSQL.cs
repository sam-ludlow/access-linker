﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace access_linker
{
	public class MsSQL
	{
		public static void Create(string connectionString, string database, string directoryData, string directoryLogs)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				string commandText = $"CREATE DATABASE [{database}]";

				if (directoryData != null)
				{
					if (directoryLogs == null)
						directoryLogs = directoryData;

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

		public static DataSet SchemaAnsi(string connectionString)
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

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				foreach (string name in informationSchemaNames)
				{
					using (SqlDataAdapter adapter = new SqlDataAdapter($"SELECT * FROM [{dataSet.DataSetName}].[{name}]", connection))
					{
						DataTable table = new DataTable(name);
						adapter.Fill(table);
						dataSet.Tables.Add(table);
					}
				}
			}

			return dataSet;
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

		public static void BackupVerify(string filename, string connectionString)
		{
			using (SqlConnection serverConnection = new SqlConnection(connectionString))
				ExecuteNonQuery(serverConnection, $"RESTORE VERIFYONLY FROM DISK = '{filename}'");
		}

		public static DataTable BackupFileList(string filename, string connectionString)
		{
			using (SqlConnection serverConnection = new SqlConnection(connectionString))
				return ExecuteFill(serverConnection, $"RESTORE FILELISTONLY FROM DISK = '{filename}'");
		}

		public static void Restore(string filename, string connectionString, string database, string directoryMDF, string directoryLDF, string with)
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
			}
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

				using (SqlConnection databaseConnection = new SqlConnection())	// MakeConnectionStringSQL(connectionString, databaseTarget)))
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










		//public static string[] ListDatabaseTables(string connectionString)
		//{
		//	using (SqlConnection connection = new SqlConnection(connectionString))
		//		return ListDatabaseTables(connection);
		//}

		//public static string[] ListDatabaseTables(SqlConnection connection)
		//{
		//	List<string> result = new List<string>();

		//	DataTable table = DataSQL.ExecuteFill(connection,
		//		"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME");

		//	foreach (DataRow row in table.Rows)
		//		result.Add((string)row["TABLE_NAME"]);

		//	return result.ToArray();
		//}


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
