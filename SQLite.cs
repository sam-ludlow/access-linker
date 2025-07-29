using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace access_linker
{
	public class SQLite
	{
		public static string[] ListTables(OdbcConnection connection)
		{
			DataTable table = new DataTable();

			using (OdbcDataAdapter adapter = new OdbcDataAdapter("SELECT tbl_name FROM sqlite_master WHERE type='table' ORDER BY tbl_name", connection))
				adapter.Fill(table);

			return table.Rows.Cast<DataRow>().Select(row => (string)row["tbl_name"]).ToArray();
		}
		public static DataSet SchemaPragma(OdbcConnection connection)
		{
			DataSet dataSet = new DataSet();

			DataTable table = new DataTable("tables");

			using (OdbcDataAdapter adapter = new OdbcDataAdapter("SELECT tbl_name FROM sqlite_master WHERE type='table' ORDER BY tbl_name", connection))
				adapter.Fill(table);

			dataSet.Tables.Add(table);

			return dataSet;
		}
	}
}
