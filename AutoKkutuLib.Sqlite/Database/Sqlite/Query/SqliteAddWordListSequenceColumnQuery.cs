using AutoKkutuLib.Sqlite.Database.Sqlite;

namespace AutoKkutuLib.Database.Sql.Query;
public class SqliteAddWordListSequenceColumnQuery : AbstractAddWordListSequenceColumnQuery
{
	internal SqliteAddWordListSequenceColumnQuery(AbstractDatabaseConnection connection) : base(connection) { }

	public override bool Execute()
	{
		Connection.RebuildWordList();
		return true;
	}
}
