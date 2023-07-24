namespace AutoKkutuLib.Database.Sql.Query;
public abstract class DropWordListColumnQueryBase : SqlQuery<int>
{
	protected string ColumnName { get; }

	protected DropWordListColumnQueryBase(DbConnectionBase connection, string columnName) : base(connection) => ColumnName = columnName;
}
