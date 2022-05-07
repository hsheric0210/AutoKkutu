using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace AutoKkutu.Databases.SQLite
{
	public class SQLiteDatabaseCommand : CommonDatabaseCommand
	{
		public SQLiteDatabaseCommand(SqliteConnection connection, string command, bool noPrepare = false) : base(noPrepare) => Command = new SqliteCommand(command, connection);

		protected override DbParameter[] TranslateParameters(params CommonDatabaseParameter[] parameters) => TranslateParameters<SqliteParameter>(parameters);
	}
}
