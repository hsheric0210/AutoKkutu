using AutoKkutuLib.Database.Relational;
using Dapper;

namespace AutoKkutuLib.Database.Relational.Query;
public class NodeDeletionQuery : SqlQuery<int>
{
	private readonly string tableName;
	public string? Node { get; set; }

	public NodeDeletionQuery(AbstractDatabaseConnection connection, string tableName) : base(connection)
	{
		if (string.IsNullOrWhiteSpace(tableName))
			throw new ArgumentException("Table name should be filled.", nameof(tableName));
		this.tableName = tableName;
	}

	public NodeDeletionQuery(AbstractDatabaseConnection connection, NodeTypes nodeType) : this(connection, nodeType.ToNodeTableName()) { }

	public int Execute(string node)
	{
		Node = node;
		return Execute();
	}

	public override int Execute()
	{
		if (string.IsNullOrWhiteSpace(Node))
			throw new InvalidOperationException("Node not set.");

		return Connection.Execute($"DELETE FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @Node", new { Node });
	}
}
