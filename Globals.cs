using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace access_linker
{
	public static class Globals
	{
		static Globals()
		{
			Arguments = new Dictionary<string, string>();
		}

		public static Dictionary<string, string> Arguments;

		public static string SqlConnectionString;
		public static string OdbcConnectionString;
		public static string OleDbConnectionString;
	}
}
