namespace AutoKkutuLib.Database.Sql.Query;
public abstract class AbstractDropWordListColumnQuery : SqlQuery<int>
{
	protected string ColumnName { get; }

	protected AbstractDropWordListColumnQuery(DbConnectionBase connection, string columnName) : base(connection) => ColumnName = columnName;
}
