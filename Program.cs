using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Diagnostics;
using Microsoft.Office.Interop.Access;  // COM: Microsoft Access 16.0 Object Library
using System.Data.SqlClient;

namespace access_linker
{
	internal class Program
	{
		static void Main(string[] args)
		{
			string accessFilename = args[0];
			string serverName = args[1];
			string databaseName = args[2];
			string emptyAccessFilename = args[3];

			File.Copy(emptyAccessFilename, accessFilename, true);

			LinkAccess(accessFilename, serverName, databaseName);

		}

		public static void LinkAccess(string accessFilename, string serverName, string databaseName)
		{
			string[] tableNames = ListDatabaseTables(serverName, databaseName);

			string connectionString =
				$"ODBC;Driver={{ODBC Driver 17 for SQL Server}};SERVER={serverName};DATABASE={databaseName};Trusted_Connection=Yes;";

			Application application = new Application();
			application.OpenCurrentDatabase(accessFilename);

			try
			{
				foreach (string tableName in tableNames)
				{
					Console.Write($"{databaseName}.{tableName}");

					application.DoCmd.TransferDatabase(
						AcDataTransferType.acLink, "ODBC Database", connectionString, AcObjectType.acTable, tableName, tableName, false, false);

					Console.WriteLine();
				}
			}
			finally
			{
				application.CloseCurrentDatabase();
				application.Quit();
			}
		}

		public static string[] ListDatabaseTables(string serverName, string databaseName)
		{
			string connectionString = $"Data Source='{serverName}';Initial Catalog='{databaseName}';Integrated Security=True;";

			using (SqlConnection connection = new SqlConnection(connectionString))
				return ListDatabaseTables(connection);
		}

		public static string[] ListDatabaseTables(SqlConnection connection)
		{
			List<string> result = new List<string>();

			DataTable table = ExecuteFill(connection,
				"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME").Tables[0];

			foreach (DataRow row in table.Rows)
				result.Add((string)row["TABLE_NAME"]);

			return result.ToArray();
		}

		public static DataSet ExecuteFill(SqlConnection connection, string commantText)
		{
			DataSet dataSet = new DataSet();

			using (SqlDataAdapter adapter = new SqlDataAdapter(commantText, connection))
				adapter.Fill(dataSet);

			return dataSet;
		}

		public static string TextTable(DataTable table)
		{
			StringBuilder result = new StringBuilder();

			foreach (DataColumn column in table.Columns)
			{
				if (column.Ordinal != 0)
					result.Append('\t');

				result.Append(column.ColumnName);
			}
			result.AppendLine();

			foreach (DataRow row in table.Rows)
			{
				foreach (DataColumn column in table.Columns)
				{
					if (column.Ordinal != 0)
						result.Append('\t');

					object value = row[column];

					if (value != null)
						result.Append(Convert.ToString(value));
				}
				result.AppendLine();
			}

			return result.ToString();
		}

		public static void PopText(DataTable table)
		{
			PopText(TextTable(table));
		}
		public static void PopText(string text)
		{
			string filename = Path.GetTempFileName();
			File.WriteAllText(filename, text, Encoding.UTF8);
			Process.Start("notepad.exe", filename);
			Environment.Exit(0);
		}


	}
}
