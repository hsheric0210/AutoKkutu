using AutoKkutuLib.Browser;
using AutoKkutuLib.Database.Helper;
using AutoKkutuLib.Database.Sql.Query;
using System.Data;

namespace AutoKkutuLib.Database.Jobs.Word;
public sealed class BatchWordAdditionJob : BatchWordJob
{
	private readonly NodeManager nodeManager;
	private readonly BrowserBase? browser;
	private readonly WordFlags wordFlags;
	private readonly bool verifyOnline;
	private readonly bool transactioned;

	public BatchWordAdditionJob(NodeManager nodeManager, BrowserBase? browser, WordFlags wordFlags, bool verifyOnline, bool transactioned = true) : base(nodeManager.DbConnection)
	{
		this.nodeManager = nodeManager;
		this.browser = browser;
		this.wordFlags = wordFlags;
		this.verifyOnline = verifyOnline;
		this.transactioned = transactioned;
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
				transaction = DbConnection.BeginTransaction(); // This will increase addition speed, especially on SQLite
			var query = DbConnection.Query.AddWord();
			foreach (var word in wordList)
			{
				if (string.IsNullOrWhiteSpace(word))
					continue;

				// Check word length
				if (word.Length <= 1)
				{
					LibLogger.Warn<BatchWordAdditionJob>("Word {word} is too short to add!", word);
					count.IncrementError();
					continue;
				}

				if (!verifyOnline || browser?.VerifyWordOnline(word) != false)
					AddSingleWord(query, word, wordFlags, ref count);
			}

			transaction?.Commit();
		}
		catch (Exception ex)
		{
			LibLogger.Error<BatchWordAdditionJob>(ex, "Failed to perform batch word addition.");
		}
		finally
		{
			transaction?.Dispose();
		}
		return count;
	}

	private void AddSingleWord(WordAdditionQuery query, string word, WordFlags flags, ref WordCount wordCount)
	{
		try
		{
			nodeManager.UpdateNodeListsByWord(word, ref flags);

			LibLogger.Info<BatchWordAdditionJob>("Adding {word} into database... (flags: {flags})", word, flags);
			if (query.Execute(word, flags))
				wordCount.Increment(flags, 1);
		}
		catch (Exception ex)
		{
			LibLogger.Error<BatchWordAdditionJob>(ex, "Exception on word addition: {word}.", word);
			wordCount.IncrementError();
		}
	}
}
