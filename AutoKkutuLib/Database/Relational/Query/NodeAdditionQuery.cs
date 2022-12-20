using AutoKkutuLib.Database.Relational;
using Dapper;

namespace AutoKkutuLib.Database.Relational.Query;
public class NodeAdditionQuery : SqlQuery<bool>
{
	private readonly string tableName;
	public string? Node { get; set; }
	public bool CheckExistence { get; set; } = true;

	public NodeAdditionQuery(AbstractDatabaseConnection connection, string tableName) : base(connection)
	{
		if (string.IsNullOrWhiteSpace(tableName))
			throw new ArgumentException("Table name should be filled.", nameof(tableName));
		this.tableName = tableName;
	}

	public NodeAdditionQuery(AbstractDatabaseConnection connection, NodeTypes nodeType) : this(connection, nodeType.ToNodeTableName()) { }

	public bool Execute(string node)
	{
		Node = node;
		return Execute();
	}

	public override bool Execute()
	{
		if (string.IsNullOrWhiteSpace(Node))
			throw new InvalidOperationException("Node not set.");

		if (CheckExistence && Connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @Node;", new { Node }) > 0)
			return false;

		Connection.Execute($"INSERT INTO {tableName}({DatabaseConstants.WordIndexColumnName}) VALUES(@Node)", new { Node });
		return true;
	}
}
