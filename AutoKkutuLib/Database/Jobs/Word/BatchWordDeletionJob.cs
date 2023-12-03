using AutoKkutuLib.Database.Sql.Query;
using System.Data;

namespace AutoKkutuLib.Database.Jobs.Word;
public sealed class BatchWordDeletionJob : BatchWordJob
{
	private readonly bool transactioned;
	private readonly bool regexp;

	public BatchWordDeletionJob(DbConnectionBase dbConnection, bool transactioned = true, bool regexp = false) : base(dbConnection)
	{
		this.transactioned = transactioned;
		this.regexp = regexp;
	}

	public override WordCount Execute(IEnumerable<string> wordList)
	{
		if (wordList == null)
			throw new ArgumentNullException(nameof(wordList));

		var count = new WordCount();

		IDbTransaction? transaction = null;
		try
		{
			if (transactioned)
				transaction = DbConnection.BeginTransaction();
			var query = DbConnection.Query.DeleteWord();
			foreach (var word in wordList)
			{
				if (RemoveSingleWord(query, word))
					count.Increment(WordFlags.None, 1);
			}

			transaction?.Commit();
		}
		catch (Exception ex)
		{
			LibLogger.Error<BatchWordDeletionJob>(ex, "Failed to commit word deletion queries to the database.");
		}
		finally
		{
			transaction?.Dispose();
		}

		return count;
	}

	private bool RemoveSingleWord(WordDeletionQuery query, string word)
	{
		if (string.IsNullOrWhiteSpace(word))
			return false;

		try
		{
			LibLogger.Info<BatchWordAdditionJob>("Removing {word} from database...", word);
			return query.Execute(word, regexp) > 0;
		}
		catch (Exception ex)
		{
			LibLogger.Error<BatchWordDeletionJob>(ex, "Exception on removing word: {word}", word);
			return false;
		}
	}
}
