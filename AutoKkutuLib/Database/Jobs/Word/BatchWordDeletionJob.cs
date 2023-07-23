﻿using AutoKkutuLib.Database.Sql.Query;

namespace AutoKkutuLib.Database.Jobs.Word;
public sealed class BatchWordDeletionJob : BatchWordJob
{
	public BatchWordDeletionJob(DbConnectionBase dbConnection) : base(dbConnection)
	{
	}

	public override WordCount Execute(string[] wordList)
	{
		if (wordList == null)
			throw new ArgumentNullException(nameof(wordList));

		var count = new WordCount();
		var transaction = DbConnection.BeginTransaction();
		var query = DbConnection.Query.DeleteWord();
		foreach (var word in wordList)
		{
			if (RemoveSingleWord(query, word))
				count.Increment(WordFlags.None, 1);
		}

		try
		{
			transaction.Commit();
		}
		catch (Exception ex)
		{
			LibLogger.Error<BatchWordDeletionJob>(ex, "Failed to commit word addition queries to the database.");
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
			LibLogger.Error<BatchWordDeletionJob>(ex, "Exception on removing word: {word}", word);
			return false;
		}
	}
}
