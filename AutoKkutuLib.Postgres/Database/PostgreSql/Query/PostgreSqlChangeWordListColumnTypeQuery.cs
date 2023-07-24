using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class PostgreSqlChangeWordListColumnTypeQuery : ChangeWordListColumnTypeQueryBase
{
	internal PostgreSqlChangeWordListColumnTypeQuery(DbConnectionBase connection, string tableName, string columnName, string newType) : base(connection, tableName, columnName, newType) { }

	// Inevitable dynamically-formatted SQL: The column name and type could't be parameterized
	public override bool Execute() => Connection.Execute($"ALTER TABLE {TableName} ALTER COLUMN {ColumnName} TYPE {NewType}") > 0;
}
