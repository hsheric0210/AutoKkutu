using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class NodeListQuery : SqlQuery<ICollection<string>>
{
	public string? TableName { get; set; }

	internal NodeListQuery(AbstractDatabaseConnection connection) : base(connection)
	{
	}

	public ICollection<string> Execute(string tableName)
	{
		TableName = tableName;
		return Execute();
	}

	public override ICollection<string> Execute()
	{
		if (string.IsNullOrWhiteSpace(TableName))
			throw new InvalidOperationException("Table name should be filled.");
		return Connection.Query<string>($"SELECT {DatabaseConstants.WordIndexColumnName} FROM {TableName}").AsList();
	}
}
