using AutoKkutuLib.Database.Relational;
using Dapper;

namespace AutoKkutuLib.Database.Relational.Query;
public class NodeListQuery : SqlQuery<ICollection<string>>
{
	private readonly string tableName;
	public string? Node { get; set; }

	public NodeListQuery(AbstractDatabaseConnection connection, string tableName) : base(connection)
	{
		if (string.IsNullOrWhiteSpace(tableName))
			throw new ArgumentException("Table name should be filled.", nameof(tableName));
		this.tableName = tableName;
	}

	public NodeListQuery(AbstractDatabaseConnection connection, NodeTypes nodeType) : this(connection, nodeType.ToNodeTableName()) { }

	public override ICollection<string> Execute() => Connection.Query<string>($"SELECT {DatabaseConstants.WordIndexColumnName} FROM {tableName}").AsList();
}
