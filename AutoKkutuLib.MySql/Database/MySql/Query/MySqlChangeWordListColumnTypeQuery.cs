using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class MySqlChangeWordListColumnTypeQuery : ChangeWordListColumnTypeQueryBase
{
	internal MySqlChangeWordListColumnTypeQuery(DbConnectionBase connection, string tableName, string columnName, string newType) : base(connection, tableName, columnName, newType) { }

	// Inevitable dynamically-formatted SQL: The column name and type could't be parameterized
	public override bool Execute() => Connection.Execute($"ALTER TABLE {TableName} MODIFY {ColumnName} {NewType}") > 0;
}
