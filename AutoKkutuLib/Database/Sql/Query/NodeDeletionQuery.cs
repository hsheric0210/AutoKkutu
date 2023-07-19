using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class NodeDeletionQuery : SqlQuery<int>
{
	private readonly string tableName;
	public string? Node { get; set; }

	internal NodeDeletionQuery(AbstractDatabaseConnection connection, string tableName) : base(connection)
	{
		if (string.IsNullOrWhiteSpace(tableName))
			throw new ArgumentException("Table name should be filled.", nameof(tableName));
		this.tableName = tableName;
	}

	public int Execute(string node)
	{
		Node = node;
		return Execute();
	}

	public override int Execute()
	{
		if (string.IsNullOrWhiteSpace(Node))
			throw new InvalidOperationException(nameof(Node) + " not set.");

		var count = Connection.Execute($"DELETE FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @Node;", new { Node });
		LibLogger.Debug<NodeDeletionQuery>("Deleted {0} of node {1} from database.", count, Node);
		return count;
	}
}
