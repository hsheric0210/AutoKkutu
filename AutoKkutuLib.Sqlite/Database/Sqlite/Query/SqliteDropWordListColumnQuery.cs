using AutoKkutuLib.Sqlite.Database.Sqlite;

namespace AutoKkutuLib.Database.Sql.Query;
public class SqliteDropWordListColumnQuery : DropWordListColumnQueryBase
{
	internal SqliteDropWordListColumnQuery(DbConnectionBase connection, string columnName) : base(connection, columnName) { }

	public override int Execute()
	{
		Connection.RebuildWordList();
		return 0;
	}
}
