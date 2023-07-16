using AutoKkutuLib.Browser;
using AutoKkutuLib.Database.Path;
using AutoKkutuLib.Database.Sql.Query;
using Serilog;

namespace AutoKkutuLib.Database.Jobs.Word;
public sealed class BatchWordAdditionJob : BatchWordJob
{
	private readonly NodeManager nodeManager;
	private readonly BrowserBase? browser;
	private readonly WordFlags wordFlags;
	private readonly bool verifyOnline;

	public BatchWordAdditionJob(NodeManager nodeManager, BrowserBase? browser, WordFlags wordFlags, bool verifyOnline) : base(nodeManager.DbConnection)
	{
		this.nodeManager = nodeManager;
		this.browser = browser;
		this.wordFlags = wordFlags;
		this.verifyOnline = verifyOnline;
	}

	public override WordCount Execute(string[] wordList)
	{
		if (wordList == null)
			throw new ArgumentNullException(nameof(wordList));

		var count = new WordCount();
		using var transaction = DbConnection.BeginTransaction(); // This will increase addition speed, especially on SQLite
		var query = DbConnection.Query.AddWord();
		foreach (var word in wordList)
		{
			if (string.IsNullOrWhiteSpace(word))
				continue;

			// Check word length
			if (word.Length <= 1)
			{
				Log.Warning("Word {word} is too short to add!", word);
				count.IncrementError();
				continue;
			}

			if (!verifyOnline || browser?.VerifyWordOnline(word) != false)
				AddSingleWord(query, word, wordFlags, ref count);
		}

		try
		{
			transaction.Commit();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to commit word addition queries to the database.");
		}
		return count;
	}

	private void AddSingleWord(WordAdditionQuery query, string word, WordFlags flags, ref WordCount wordCount)
	{
		try
		{
			nodeManager.UpdateNodeListsByWord(word, ref flags);

			Log.Verbose("Adding {word} into database... (flags: {flags})", word, flags);
			if (query.Execute(word, flags))
				wordCount.Increment(flags, 1);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Exception on word addition: {word}.", word);
			wordCount.IncrementError();
		}
	}
}
