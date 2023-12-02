using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class NodeDeletionQuery : SqlQuery<int>
{
	private readonly string tableName;
	public string? Node { get; set; }
	public bool Regexp { get; set; }

	internal NodeDeletionQuery(DbConnectionBase connection, string tableName) : base(connection)
	{
		if (string.IsNullOrWhiteSpace(tableName))
			throw new ArgumentException("Table name should be filled.", nameof(tableName));
		this.tableName = tableName;
	}

	public int Execute(string node, bool regexp = false)
	{
		Node = node;
		Regexp = regexp;
		return Execute();
	}

	public override int Execute()
	{
		if (string.IsNullOrWhiteSpace(Node))
			throw new InvalidOperationException(nameof(Node) + " not set.");

		string query;
		if (Regexp)
		{
			Node = "(?i)" + Node; // Case-insensitive match
			query = $"DELETE FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} REGEXP @Node;";
		}
		else
		{
			query = $"DELETE FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @Node;";
		}

		var count = Connection.Execute(query, new { Node });
		LibLogger.Debug<NodeDeletionQuery>("Deleted {0} of node {1} from database.", count, Node);
		return count;
	}
}
