using AutoKkutuLib.Sqlite.Database.Sqlite;

namespace AutoKkutuLib.Database.Sql.Query;
public class SqliteChangeWordListColumnTypeQuery : ChangeWordListColumnTypeQueryBase
{
	internal SqliteChangeWordListColumnTypeQuery(DbConnectionBase connection, string tableName, string columnName, string newType) : base(connection, tableName, columnName, newType) { }

	public override bool Execute()
	{
		Connection.RebuildWordList();
		return true;
	}
}
