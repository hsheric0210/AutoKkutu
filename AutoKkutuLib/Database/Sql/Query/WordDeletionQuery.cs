using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class WordDeletionQuery : SqlQuery<int>
{
	public string? Word { get; set; }

	internal WordDeletionQuery(AbstractDatabaseConnection connection) : base(connection) { }

	public int Execute(string word)
	{
		Word = word;
		return Execute();
	}

	public override int Execute()
	{
		if (string.IsNullOrWhiteSpace(Word))
			throw new InvalidOperationException(nameof(Word) + " not set.");

		return Connection.Execute($"DELETE FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} = @Word;", new { Word });
	}
}
