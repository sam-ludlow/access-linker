using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Xml.Linq;

namespace access_linker
{
	public class XML
	{
		public static void InsertAccess(string xmlFilename, string targetFilename)
		{
			using (var targetConnection = new OleDbConnection(Tools.MakeConnectionStringOLEDB(targetFilename)))
			{
				DataSet dataSet = ImportXML(xmlFilename);
				DataSet schema = SchemaDataSet(dataSet);

				foreach (string tableName in MsAccess.CreateAccessTables(schema, targetConnection))
				{
					DataTable table = dataSet.Tables[tableName];

					Console.WriteLine($"{table.TableName} {table.Rows.Count}");

					MsAccess.AccessBulkInsert(targetConnection, table);
				}
			}
		}
		public static DataSet ImportXML(string filename)
		{
			XElement document = XElement.Load(filename);

			Dictionary<string, HashSet<string>> tableNameParentNames = new Dictionary<string, HashSet<string>>();
			PassXML(document, tableNameParentNames, null);

			HashSet<string> parentNameTables = new HashSet<string>();
			foreach (string tableName in tableNameParentNames.Keys)
			{
				if (tableNameParentNames[tableName].Count > 1)
				{
					foreach (string parentName in tableNameParentNames[tableName])
						parentNameTables.Add($"{parentName}_{tableName}");
				}
			}

			DataSet dataSet = new DataSet();
			ImportXML(document, dataSet, null, parentNameTables, typeof(int));

			return dataSet;
		}

		public static DataSet SchemaDataSet(DataSet dataSet)
		{
			DataSet schema = new DataSet();

			DataTable TABLES = Tools.MakeDataTable("TABLES",
				"TABLE_NAME",
				"String");
			schema.Tables.Add(TABLES);

			DataTable COLUMNS = Tools.MakeDataTable("COLUMNS",
				"TABLE_NAME	COLUMN_NAME	NULLABLE	TYPE_NAME	COLUMN_SIZE	ORDINAL_POSITION",
				"String		String		Int16		String		Int32		Int32");
			schema.Tables.Add(COLUMNS);

			DataTable INDEXES = Tools.MakeDataTable("INDEXES",
				"TABLE_NAME	INDEX_NAME	COLUMN_NAME	ORDINAL_POSITION",
				"String		String		String		Int32");
			schema.Tables.Add(INDEXES);

			foreach (DataTable table in dataSet.Tables)
			{
				TABLES.Rows.Add(table.TableName);

				foreach (DataColumn column in table.Columns)
				{
					int max = column.MaxLength;
					if (column.DataType == typeof(string))
					{
						foreach (DataRow row in table.Rows)
						{
							if (row.IsNull(column) == false)
							{
								string value = (string)row[column];
								if (value.Length > max)
									max = value.Length;
							}
						}
					}
					COLUMNS.Rows.Add(table.TableName, column.ColumnName, column.AllowDBNull ? 1 : 0, column.DataType.Name, max, column.Ordinal);
				}

				for (int ordinal = 0; ordinal < table.PrimaryKey.Length; ++ordinal)
				{
					DataColumn column = table.PrimaryKey[ordinal];
					INDEXES.Rows.Add(table.TableName, $"PK_{table.TableName}", column.ColumnName, ordinal);
				}
			}

			return schema;
		}

		private static void PassXML(XElement element, Dictionary<string, HashSet<string>> tableNameParentNames, string parentTableName)
		{
			string tableName = element.Name.LocalName;

			if (tableNameParentNames.ContainsKey(tableName) == false)
				tableNameParentNames.Add(tableName, new HashSet<string>());

			if (parentTableName != null)
				tableNameParentNames[tableName].Add(parentTableName);

			foreach (XElement childElement in element.Elements())
			{
				if (childElement.HasAttributes == true || childElement.HasElements == true)
					PassXML(childElement, tableNameParentNames, tableName);
			}
		}

		private static void ImportXML(XElement element, DataSet dataSet, DataRow parentRow, HashSet<string> parentNameTables, Type pkType)
		{
			string tableName = element.Name.LocalName;

			if (parentRow != null)
			{
				string parentTableName = $"{parentRow.Table.TableName}_{tableName}";
				if (parentNameTables.Contains(parentTableName) == true)
					tableName = parentTableName;
			}

			string forignKeyName = null;
			if (parentRow != null)
				forignKeyName = parentRow.Table.TableName + "_id";

			DataTable table;

			if (dataSet.Tables.Contains(tableName) == false)
			{
				table = new DataTable(tableName);
				DataColumn pkColumn = table.Columns.Add(tableName + "_id", pkType);
				pkColumn.AutoIncrement = true;
				pkColumn.AutoIncrementSeed = 1;

				table.PrimaryKey = new DataColumn[] { pkColumn };

				if (parentRow != null)
					table.Columns.Add(forignKeyName, parentRow.Table.Columns[forignKeyName].DataType);

				dataSet.Tables.Add(table);
			}
			else
			{
				table = dataSet.Tables[tableName];
			}

			Dictionary<string, string> rowValues = new Dictionary<string, string>();

			foreach (XAttribute attribute in element.Attributes())
				rowValues.Add(attribute.Name.LocalName, attribute.Value);

			foreach (XElement childElement in element.Elements())
			{
				if (childElement.HasAttributes == false && childElement.HasElements == false)
					rowValues.Add(childElement.Name.LocalName, childElement.Value);
			}

			foreach (string columnName in rowValues.Keys)
			{
				if (table.Columns.Contains(columnName) == false)
					table.Columns.Add(columnName, typeof(string));
			}

			DataRow row = table.NewRow();

			if (parentRow != null)
				row[forignKeyName] = parentRow[forignKeyName];

			foreach (string columnName in rowValues.Keys)
				row[columnName] = rowValues[columnName];

			table.Rows.Add(row);

			foreach (XElement childElement in element.Elements())
			{
				if (childElement.HasAttributes == true || childElement.HasElements == true)
					ImportXML(childElement, dataSet, row, parentNameTables, pkType);
			}
		}
	}
}
