using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu.Databases.MySQL
{
	public class MySQLDatabaseReader : CommonDatabaseReader
	{
		private readonly MySqlDataReader Reader;

		public MySQLDatabaseReader(MySqlDataReader reader) => Reader = reader;

		protected override object GetObject(string name) => Reader[name];

		public override string GetString(int index) => Reader.GetString(index);

		public override int GetOrdinal(string name) => Reader.GetOrdinal(name);

		public override int GetInt32(int index) => Reader.GetInt32(index);

		public override bool Read() => Reader.Read();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Reader.Dispose();
			base.Dispose(disposing);
		}
	}
}
