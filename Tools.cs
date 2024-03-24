using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace access_linker
{
	public class Tools
	{
		public static void RequiredArguments(Dictionary<string, string> arguments, string[] requireds)
		{
			bool miss = false;
			foreach (string required in requireds)
				if (arguments.ContainsKey(required) == false)
					miss = true;

			if (requireds.Length == 0 || miss == true)
				throw new ApplicationException("!!! USAGE !!!");
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

		public static void EncodeFile(string filename)
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

				PopText(result.ToString());
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
			Environment.Exit(0);
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
