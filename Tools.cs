using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace access_linker
{
	public class Tools
	{

		public static string MakeConnectionStringOLEDB(string accessFilename)
		{
			if (accessFilename.Contains(";") == false)
				accessFilename = $"Provider='Microsoft.ACE.OLEDB.16.0';User ID='Admin';Password='';Data Source='{accessFilename}';";

			string systemDatabaseFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Access", "System.mdw");

			if (File.Exists(systemDatabaseFilename) == false)
				throw new ApplicationException($"Microsoft Access System database missing: '{systemDatabaseFilename}'.");

			accessFilename += $"Jet OLEDB:System Database='{systemDatabaseFilename}';";

			Console.WriteLine($"OLEDB:\t{accessFilename}");

			return accessFilename;
		}

		public static string MakeConnectionStringODBC(string connectionString)
		{
			if (connectionString.Contains(";") == false)
			{
				if (connectionString.Contains("@") == true)
				{
					string[] parts = connectionString.Split('@');
					if (parts.Length != 2)
						throw new ApplicationException("SQL ODCB Usage: server@database");

					connectionString = $"Driver={{ODBC Driver 18 for SQL Server}};SERVER={parts[0]};DATABASE={parts[1]};Trusted_Connection=Yes;TrustServerCertificate=Yes;";
				}
				else
				{
					connectionString = $"DRIVER={{SQLite3 ODBC Driver}};DATABASE={connectionString};";
				}
			}

			Console.WriteLine($"ODBC:\t{connectionString}");

			return connectionString;
		}

		public static string MakeConnectionStringMsSQL(string connectionString)
		{
			if (connectionString.Contains(";") == false)
			{
				string[] parts = connectionString.Split('@');

				connectionString = $"Data Source={parts[0]};Integrated Security=True;TrustServerCertificate=True;";

				if (parts.Length > 1)
					connectionString += $"Initial Catalog={parts[1]};";
			}

			Console.WriteLine($"MS SQL:\t{connectionString}");

			return connectionString;
		}

		public static string[] TableNameList(DbConnection connection)
		{
			connection.Open();
			try
			{
				List<string> tableNames = connection.GetSchema("Tables").Rows.Cast<DataRow>().Select(row => (string)row["TABLE_NAME"]).ToList();
				tableNames.Sort();
				return tableNames.ToArray();
			}
			finally
			{
				connection.Close();
			}
		}


		public static DataSet SchemaConnection(DbConnection connection)
		{
			DataSet dataSet = new DataSet();

			connection.Open();

			try
			{
				List<string> tableNames = connection.GetSchema("Tables").Rows.Cast<DataRow>().Select(row => (string)row["TABLE_NAME"]).ToList();
				tableNames.Sort();

				List<string> collectionNames = new List<string>(connection.GetSchema().Rows.Cast<DataRow>().Select(row => (string)row["CollectionName"]));
				collectionNames.Sort();

				foreach (string collectionName in collectionNames)
				{
					DataTable table = null;

					if (collectionName == "Indexes")
					{
						string[] restrictions = new string[4];
						foreach (string tableName in tableNames)
						{
							restrictions[2] = tableName;

							DataTable schemaTable;
							try
							{
								schemaTable = connection.GetSchema(collectionName, restrictions);
							}
							catch (ArgumentException e)
							{
								Console.WriteLine($"GetSchema({collectionName}, {tableName}): {e.Message}");
								continue;
							}

							if (table == null)
							{
								table = schemaTable;
							}
							else
							{
								foreach (DataRow row in schemaTable.Rows)
									table.ImportRow(row);
							}
						}
						table.TableName = collectionName;
						dataSet.Tables.Add(table);
					}
					else
					{
						try
						{
							table = connection.GetSchema(collectionName);
							table.TableName = collectionName;
							dataSet.Tables.Add(table);
						}
						catch (Exception e)
						{
							Console.WriteLine($"GetSchema({collectionName}): {e.Message}");
						}

					}
				}
			}
			finally
			{
				connection.Close();
			}

			return dataSet;
		}

		public static int ExecuteNonQuery(OleDbConnection connection, string commandText)
		{
			connection.Open();
			try
			{
				using (OleDbCommand command = new OleDbCommand(commandText, connection))
					return command.ExecuteNonQuery();
			}
			finally
			{
				connection.Close();
			}
		}
		public static object ExecuteScalar(OleDbConnection connection, string commandText)
		{
			connection.Open();
			try
			{
				using (OleDbCommand command = new OleDbCommand(commandText, connection))
					return command.ExecuteScalar();
			}
			finally
			{
				connection.Close();
			}
		}



		public static string TextTable(DataTable table)
		{
			StringBuilder result = new StringBuilder();

			for (int pass = 0; pass < 2; ++pass)
			{
				foreach (DataColumn column in table.Columns)
				{
					if (column.Ordinal != 0)
						result.Append('\t');

					result.Append(pass == 0 ? column.ColumnName : column.DataType.Name);
				}
				result.AppendLine();
			}

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

		public static string EncodeFile(string filename)
		{
			int width = 76;

			using (MemoryStream compressedStream = new MemoryStream())
			{
				using (FileStream sourceStream = new FileStream(filename, FileMode.Open))
				{
					using (GZipStream compress = new GZipStream(compressedStream, CompressionLevel.Optimal))
					{
						sourceStream.CopyTo(compress);
					}
				}

				string base64 = Convert.ToBase64String(compressedStream.ToArray());

				StringBuilder result = new StringBuilder();

				for (int index = 0; index < base64.Length; index += width)
				{
					int remain = base64.Length - index;
					int length = Math.Min(width, remain);

					result.AppendLine(base64.Substring(index, length));
				}

				return result.ToString();
			}
		}

		public static void PopText(DataSet dataSet)
		{
			StringBuilder text = new StringBuilder();

			foreach (DataTable table in dataSet.Tables)
			{
				string hr = new string('-', table.TableName.Length);
				text.AppendLine(hr);
				text.AppendLine(table.TableName);
				text.AppendLine(hr);
				text.AppendLine(TextTable(table));
				text.AppendLine();
			}

			PopText(text.ToString());
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
		}
	}

	public class TempDirectory : IDisposable
	{
		private readonly string _LockFilePath;
		private readonly string _Path;

		public TempDirectory()
		{
			_LockFilePath = System.IO.Path.GetTempFileName();
			_Path = _LockFilePath + ".dir";

			Directory.CreateDirectory(this._Path);
		}

		public void Dispose()
		{
			if (Directory.Exists(_Path) == true)
				Directory.Delete(_Path, true);

			if (_LockFilePath != null)
				File.Delete(_LockFilePath);
		}

		public string Path
		{
			get
			{
				return _Path;
			}
		}

		public override string ToString()
		{
			return _Path;
		}
	}
}
