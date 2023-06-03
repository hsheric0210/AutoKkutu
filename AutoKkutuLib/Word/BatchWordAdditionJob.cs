using AutoKkutuLib.Browser;
using AutoKkutuLib.Browser.Extension;
using AutoKkutuLib.Database.Sql.Query;
using AutoKkutuLib.Node;
using Serilog;

namespace AutoKkutuLib.Word;
public sealed class BatchWordAdditionJob : BatchWordJob
{
	private readonly NodeManager nodeManager;
	private readonly BrowserBase jsEvaluator;
	private readonly WordFlags wordFlags;
	private readonly bool verifyOnline;

	public BatchWordAdditionJob(NodeManager nodeManager, BrowserBase jsEvaluator, WordFlags wordFlags, bool verifyOnline) : base(nodeManager.DbConnection)
	{
		this.nodeManager = nodeManager;
		this.jsEvaluator = jsEvaluator;
		this.wordFlags = wordFlags;
		this.verifyOnline = verifyOnline;
	}

	public override WordCount Execute(string[] wordList)
	{
		if (wordList == null)
			throw new ArgumentNullException(nameof(wordList));

		var count = new WordCount();
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

			if (!verifyOnline || jsEvaluator.VerifyWordOnline(word))
				AddSingleWord(query, word, wordFlags, ref count);
		}

		return count;
	}

	private void AddSingleWord(WordAdditionQuery query, string word, WordFlags flags, ref WordCount wordCount)
	{
		try
		{
			nodeManager.UpdateNodeListsByWord(word, ref flags);

			Log.Information("Adding {word} into database... (flags: {flags})", word, flags);
			if (query.Execute(word,flags))
				wordCount.Increment(flags, 1);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Exception on word addition: {word}.", word);
			wordCount.IncrementError();
		}
	}
}
