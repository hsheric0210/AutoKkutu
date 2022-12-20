using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class PostgreAddWordListSequenceColumnQuery : AbstractAddWordListSequenceColumnQuery
{
	internal PostgreAddWordListSequenceColumnQuery(AbstractDatabaseConnection connection) : base(connection) { }

	public override bool Execute() => Connection.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} ADD COLUMN seq SERIAL PRIMARY KEY") > 0;
}
