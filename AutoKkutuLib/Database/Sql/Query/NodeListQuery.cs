using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class NodeListQuery : SqlQuery<ICollection<string>>
{
	private readonly string tableName;
	public string? Node { get; set; }

	internal NodeListQuery(AbstractDatabaseConnection connection, string tableName) : base(connection)
	{
		if (string.IsNullOrWhiteSpace(tableName))
			throw new ArgumentException("Table name should be filled.", nameof(tableName));
		this.tableName = tableName;
	}

	public override ICollection<string> Execute() => Connection.Query<string>($"SELECT {DatabaseConstants.WordIndexColumnName} FROM {tableName}").AsList();
}
