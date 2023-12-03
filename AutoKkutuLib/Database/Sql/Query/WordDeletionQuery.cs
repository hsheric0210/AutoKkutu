using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class WordDeletionQuery : SqlQuery<int>
{
	public string? Word { get; set; }
	public bool Regexp { get; set; }

	internal WordDeletionQuery(DbConnectionBase connection) : base(connection) { }

	public int Execute(string word, bool regexp = false)
	{
		Word = word;
		Regexp = regexp;
		return Execute();
	}

	public override int Execute()
	{
		if (string.IsNullOrWhiteSpace(Word))
			throw new InvalidOperationException(nameof(Word) + " not set.");

		LibLogger.Debug<WordDeletionQuery>("Deleting word {0} from database.", Word);
		string query;
		if (Regexp)
		{
			Word = "(?i)^" + Word + '$';
			query = $"DELETE FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} REGEXP @Word;";
		}
		else
		{
			query = $"DELETE FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} = @Word;";
		}

		var count = Connection.Execute(query, new { Word });
		LibLogger.Debug<WordDeletionQuery>("Deleted {0} of word {1} from database.", count, Word);
		return count;
	}
}
