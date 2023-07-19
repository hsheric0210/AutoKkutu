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

		LibLogger.Debug<WordDeletionQuery>(nameof(WordDeletionQuery) + ": Deleting word {0} from database.", Word);
		var count = Connection.Execute($"DELETE FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} = @Word;", new { Word });
		LibLogger.Debug<WordDeletionQuery>(nameof(WordDeletionQuery) + ": Deleted {0} of word {1} from database.", count, Word);
		return count;
	}
}
