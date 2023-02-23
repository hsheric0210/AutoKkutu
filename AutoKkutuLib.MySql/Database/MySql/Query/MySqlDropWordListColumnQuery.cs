using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class MySqlDropWordListColumnQuery : AbstractDropWordListColumnQuery
{
	internal MySqlDropWordListColumnQuery(AbstractDatabaseConnection connection, string columnName) : base(connection, columnName) { }

	public override int Execute() => Connection.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} DROP {ColumnName}");
}
