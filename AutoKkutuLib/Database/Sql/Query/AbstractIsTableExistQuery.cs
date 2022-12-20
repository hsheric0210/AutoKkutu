namespace AutoKkutuLib.Database.Sql.Query;
public abstract class AbstractIsTableExistsQuery : SqlQuery<bool>
{
	protected string TableName { get; }

	protected AbstractIsTableExistsQuery(AbstractDatabaseConnection connection, string tableName) : base(connection) => TableName = tableName;
}
