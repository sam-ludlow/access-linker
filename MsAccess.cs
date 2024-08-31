using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Microsoft.Office.Interop.Access;  // COM: Microsoft Access 16.0 Object Library

namespace access_linker
{
	public class MsAccess
	{
		public static void Create(string filename)
		{
			using (MemoryStream compressedStream = new MemoryStream(Convert.FromBase64String(EMPTY_accdb_gz_base64)))
			{
				using (FileStream targetStream = new FileStream(filename, FileMode.Create))
				{
					using (GZipStream decompress = new GZipStream(compressedStream, CompressionMode.Decompress))
					{
						decompress.CopyTo(targetStream);
					}
				}
			}
		}

		public static void Delete(string filename)
		{
			File.Delete(filename);
		}

		public static void Link(string filename, string odbcConnectionString)
		{
			string[] tableNames;
			using (var connection = new OdbcConnection(odbcConnectionString))
				tableNames = Tools.TableNameList(connection);

			TransferDatabase(filename, "acLink", tableNames, odbcConnectionString);
		}

		public static void Import(string filename, string odbcConnectionString)
		{
			string[] tableNames;
			using (var connection = new OdbcConnection(odbcConnectionString))
				tableNames = Tools.TableNameList(connection);

			TransferDatabase(filename, "acImport", tableNames, odbcConnectionString);
		}

		public static void Export(string filename, string odbcConnectionString, string oledbConnectionString)
		{
			string[] tableNames;
			using (var connection = new OdbcConnection(odbcConnectionString))
				tableNames = Tools.TableNameList(connection);

			TransferDatabase(filename, "acExport", tableNames, odbcConnectionString);
		}

		public static void TransferDatabase(string filename, string type, string[] tableNames, string connectionStringODBC)
		{
			connectionStringODBC = "ODBC;" + connectionStringODBC;

			AcDataTransferType transferType = (AcDataTransferType)Enum.Parse(typeof(AcDataTransferType), type);

			Application application = new Application();

			try
			{
				application.OpenCurrentDatabase(filename);

				try
				{
					foreach (string tableName in tableNames)
					{
						Console.Write(tableName);

						string sourceTableName = tableName;	// = type == "acExport" ? tableName : $"dbo.{tableName}";

						application.DoCmd.TransferDatabase(transferType, "ODBC Database", connectionStringODBC, AcObjectType.acTable, sourceTableName, tableName, false, true);

						Console.WriteLine(".");
					}
				}
				finally
				{
					application.CloseCurrentDatabase();
				}
			}
			finally
			{
				application.Quit();
			}
		}

		public static string[] ListAccessTables(string oledbConnectionString)
		{
			DataTable schemaTables;

			using (OleDbConnection connection = new OleDbConnection(oledbConnectionString))
			{
				connection.Open();
				try
				{
					schemaTables = connection.GetSchema("Tables");
				}
				finally
				{
					connection.Close();
				}
			}

			List<string> result = new List<string>();
			foreach (DataRow row in schemaTables.Rows)
			{
				if ((string)row["TABLE_TYPE"] != "TABLE")
					continue;

				result.Add((string)row["TABLE_NAME"]);
			}
			result.Sort();

			return result.ToArray();
		}

		public static void Insert(string sqlConnectionString, string oledbConnectionString)
		{
			using (var sourceConnection = new SqlConnection(sqlConnectionString))
			{
				DataSet schema = DataSQL.GetInformationSchemas(sourceConnection);

				using (var targetConnection = new OleDbConnection(oledbConnectionString))
				{
					foreach (string tableName in CreateAccessTables(schema, targetConnection))
					{
						DataTable table = DataSQL.ExecuteFill(sourceConnection, $"SELECT * FROM [{tableName}]");
						table.TableName = tableName;

						Console.WriteLine($"{table.TableName} {table.Rows.Count}");

						AccessBulkInsert(targetConnection, table);
					}
				}
			}
		}

		public static DataSet SchemaOLEDB(string connectionString)
		{
			using (var connection = new OleDbConnection(connectionString))
				return Tools.SchemaConnection(connection);
		}

		public static DataSet SchemaODBC(string connectionString)
		{
			using (var connection = new OdbcConnection(connectionString))
				return Tools.SchemaConnection(connection);
		}

		public static string[] CreateAccessTables(DataSet schema, OleDbConnection connection)
		{
			List<string> ignoreTableNames = new List<string>(new string[] {
				"sysdiagrams",
			});

			List<string> tableNames = new List<string>();

			foreach (DataRow tableRow in schema.Tables["TABLES"].Rows)
			{
				var TABLE_NAME = (string)tableRow["TABLE_NAME"];

				if (ignoreTableNames.Contains(TABLE_NAME) == true)
					continue;

				tableNames.Add(TABLE_NAME);

				List<string> columnDefs = new List<string>();

				foreach (DataRow columnRow in schema.Tables["COLUMNS"].Select($"TABLE_NAME = '{TABLE_NAME}'", "ORDINAL_POSITION"))
				{
					var COLUMN_NAME = (string)columnRow["COLUMN_NAME"];
					var IS_NULLABLE = (string)columnRow["IS_NULLABLE"];
					var DATA_TYPE = (string)columnRow["DATA_TYPE"];
					int CHARACTER_MAXIMUM_LENGTH = columnRow.IsNull("CHARACTER_MAXIMUM_LENGTH") == true ? 0 : (int)columnRow["CHARACTER_MAXIMUM_LENGTH"];

					string dataType;

					switch (DATA_TYPE)
					{
						case "char":
						case "varchar":
						case "nvarchar":
							dataType = "VARCHAR";
							if (CHARACTER_MAXIMUM_LENGTH == -1 || CHARACTER_MAXIMUM_LENGTH > 255)
								dataType = "LONGTEXT";

							IS_NULLABLE = "YES"; // Access don't seem to like empty strings
							break;

						case "bit":
							dataType = "BIT";
							break;

						case "int":
							dataType = "LONG";
							break;

						case "bigint":
							dataType = "BIGINT";
							break;

						case "datetime":
							dataType = "DATETIME";
							break;

						case "datetime2":
							dataType = "DATETIME2";
							break;

						default:
							throw new ApplicationException($"Unknown datatype {DATA_TYPE}");

					}

					string nullable = IS_NULLABLE == "YES" ? "NULL" : "NOT NULL";

					columnDefs.Add($"[{COLUMN_NAME}] {dataType} {nullable}");
				}

				string CONSTRAINT_NAME = schema.Tables["TABLE_CONSTRAINTS"]
					.Select($"TABLE_NAME = '{TABLE_NAME}' AND CONSTRAINT_TYPE = 'PRIMARY KEY'")
					.Select(row => (string)row["CONSTRAINT_NAME"])
					.Single();

				string[] keyColumnNames = schema.Tables["KEY_COLUMN_USAGE"]
					.Select($"TABLE_NAME = '{TABLE_NAME}' AND CONSTRAINT_NAME = '{CONSTRAINT_NAME}'", "ORDINAL_POSITION")
					.Select(row => (string)row["COLUMN_NAME"])
					.ToArray();

				if (keyColumnNames.Length == 0)
					throw new ApplicationException($"Did not find key {CONSTRAINT_NAME}");

				columnDefs.Add($"CONSTRAINT [PrimaryKey] PRIMARY KEY ({String.Join(", ", keyColumnNames)})");

				string commandText = $"CREATE TABLE [{TABLE_NAME}] ({String.Join(", ", columnDefs)})";

				Console.WriteLine(commandText);

				ExecuteNonQuery(connection, commandText);
			}

			return tableNames.ToArray();
		}

		public static void AccessBulkInsert(OleDbConnection connection, DataTable table)
		{
			using (TempDirectory TempDir = new TempDirectory())
			{
				string csvFilename = Path.Combine(TempDir.Path, table.TableName + ".csv");
				string iniFilename = Path.Combine(TempDir.Path, "Schema.ini");

				File.WriteAllLines(iniFilename, new string[] {
					$"[{Path.GetFileName(csvFilename)}]",
					"Format=TabDelimited",
					"CharacterSet=65001",
					"HDR=YES",
				});

				using (StreamWriter writer = new StreamWriter(csvFilename, false, new UTF8Encoding(false)))
				{
					foreach (DataColumn column in table.Columns)
					{
						if (column.Ordinal > 0)
							writer.Write('\t');

						writer.Write(column.ColumnName);
					}
					writer.WriteLine();

					foreach (DataRow row in table.Rows)
					{
						foreach (DataColumn column in table.Columns)
						{
							if (column.Ordinal > 0)
								writer.Write('\t');

							if (row.IsNull(column) == false)
							{
								string value = Convert.ToString(row[column]);

								value = value.Replace("\"", "\"\"");

								if (column.DataType.Name == "String")
									writer.Write('\"');

								writer.Write(value);

								if (column.DataType.Name == "String")
									writer.Write('\"');
							}
						}
						writer.WriteLine();
					}
				}
				string commandText = $"INSERT INTO [{table.TableName}] SELECT * FROM [Text;CharacterSet=65001;Database={Path.GetDirectoryName(csvFilename)}].[{Path.GetFileName(csvFilename)}]";

				Console.WriteLine(commandText);

				ExecuteNonQuery(connection, commandText);
			}
		}



		public static DataSet ExecuteFill(OleDbConnection connection, string commandText)
		{
			DataSet dataSet = new DataSet();

			using (OleDbDataAdapter adapter = new OleDbDataAdapter(commandText, connection))
				adapter.Fill(dataSet);

			return dataSet;
		}

		public static void ExecuteNonQuery(OleDbConnection connection, string commandText)
		{
			using (OleDbCommand command = new OleDbCommand(commandText, connection))
			{
				connection.Open();
				try
				{
					command.ExecuteNonQuery();
				}
				finally
				{
					connection.Close();
				}
			}
		}

		public static string EMPTY_accdb_gz_base64 = @"
			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
		";

	}
}
