using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class PostgreDropWordListColumnQuery : AbstractDropWordListColumnQuery
{
	internal PostgreDropWordListColumnQuery(AbstractDatabaseConnection connection, string columnName) : base(connection, columnName) { }

	public override int Execute() => Connection.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} DROP COLUMN {ColumnName}");
}
