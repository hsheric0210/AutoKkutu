namespace AutoKkutuLib.Database.Sql.Query;
public abstract class AbstractDropWordListColumnQuery : SqlQuery<int>
{
	protected string ColumnName { get; }

	protected AbstractDropWordListColumnQuery(AbstractDatabaseConnection connection, string columnName) : base(connection) => ColumnName = columnName;
}
