using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class MySqlAddWordListSequenceColumnQuery : AddWordListSequenceColumnQueryBase
{
	internal MySqlAddWordListSequenceColumnQuery(DbConnectionBase connection) : base(connection) { }

	public override bool Execute() => Connection.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} ADD COLUMN seq NOT NULL AUTO_INCREMENT PRIMARY KEY;") > 0;
}
