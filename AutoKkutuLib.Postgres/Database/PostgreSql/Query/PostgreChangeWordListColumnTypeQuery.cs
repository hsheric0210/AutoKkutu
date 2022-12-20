using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class PostgreChangeWordListColumnTypeQuery : AbstractChangeWordListColumnTypeQuery
{
	internal PostgreChangeWordListColumnTypeQuery(AbstractDatabaseConnection connection, string tableName, string columnName, string newType) : base(connection, tableName, columnName, newType) { }

	public override bool Execute() => Connection.Execute($"ALTER TABLE {TableName} ALTER COLUMN {ColumnName} TYPE {NewType}") > 0;
}
