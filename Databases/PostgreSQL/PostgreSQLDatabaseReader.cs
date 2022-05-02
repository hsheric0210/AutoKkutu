using Npgsql;
using System;

namespace AutoKkutu.Databases.PostgreSQL
{
	public class PostgreSQLDatabaseReader : CommonDatabaseReader
	{
		private readonly NpgsqlDataReader Reader;

		public PostgreSQLDatabaseReader(NpgsqlDataReader reader) => Reader = reader;

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
