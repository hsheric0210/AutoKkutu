using AutoKkutuLib.Sqlite.Database.Sqlite;

namespace AutoKkutuLib.Database.Sql.Query;
public class SqliteAddWordListSequenceColumnQuery : AddWordListSequenceColumnQueryBase
{
	internal SqliteAddWordListSequenceColumnQuery(DbConnectionBase connection) : base(connection) { }

	public override bool Execute()
	{
		Connection.RebuildWordList();
		return true;
	}
}
