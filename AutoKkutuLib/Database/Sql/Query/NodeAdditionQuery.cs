using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class NodeAdditionQuery : SqlQuery<bool>
{
	private readonly string tableName;
	public string? Node { get; set; }
	public bool CheckExistence { get; set; } = true;

	internal NodeAdditionQuery(DbConnectionBase connection, string tableName) : base(connection)
	{
		if (string.IsNullOrWhiteSpace(tableName))
			throw new ArgumentException("Table name should be filled.", nameof(tableName));
		this.tableName = tableName;
	}

	public bool Execute(string node)
	{
		Node = node;
		return Execute();
	}

	public override bool Execute()
	{
		if (string.IsNullOrWhiteSpace(Node))
			throw new InvalidOperationException(nameof(Node) + " not set.");

		if (CheckExistence && Connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @Node;", new { Node }) > 0)
			return false;

		var count = Connection.Execute($"INSERT INTO {tableName}({DatabaseConstants.WordIndexColumnName}) VALUES(@Node);", new { Node });
		LibLogger.Debug<NodeAdditionQuery>("Added {0} of node {1} to database table {2}.", count, Node, tableName);
		return count > 0;
	}
}
