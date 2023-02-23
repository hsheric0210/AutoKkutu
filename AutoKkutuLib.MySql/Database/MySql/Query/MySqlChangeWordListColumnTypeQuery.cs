using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class MySqlChangeWordListColumnTypeQuery : AbstractChangeWordListColumnTypeQuery
{
	internal MySqlChangeWordListColumnTypeQuery(AbstractDatabaseConnection connection, string tableName, string columnName, string newType) : base(connection, tableName, columnName, newType) { }

	public override bool Execute() => Connection.Execute($"ALTER TABLE {TableName} MODIFY {ColumnName} {NewType}") > 0;
}
