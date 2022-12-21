using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql.Query;
using Serilog;

namespace AutoKkutuLib.Word;
public sealed class WordDeletionJob : WordJob
{
	public WordDeletionJob(AbstractDatabaseConnection dbConnection) : base(dbConnection)
	{
	}

	public int BatchRemoveWord(string[] wordlist)
	{
		if (wordlist == null)
			throw new ArgumentNullException(nameof(wordlist));

		var count = 0;
		WordDeletionQuery query = DbConnection.Query.DeleteWord();
		foreach (var word in wordlist)
		{
			if (RemoveSingleWord(query, word))
				count++;
		}

		return count;
	}

	private bool RemoveSingleWord(WordDeletionQuery query, string word)
	{
		if (string.IsNullOrWhiteSpace(word))
			return false;

		try
		{
			return query.Execute(word) > 0;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Exception on removing word: {word}", word);
			return false;
		}
	}
}
