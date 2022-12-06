using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Extension;
using AutoKkutuLib.HandlerManagement.Extension;
using Serilog;

namespace AutoKkutuLib.Path;

public class WordBatchJob
{
	private readonly NodeManager nodeManager;

	public WordBatchJob(NodeManager nodeManager) => this.nodeManager = nodeManager;

	#region Procedure for every single word (for internal use)
	private enum AddWordResultType
	{
		Success,
		Duplicate,
		Failed
	}

	private sealed record AddWordResult(AddWordResultType ResultType, int NewEndNode = 0, int NewAttackNode = 0);

	private AddWordResult AddSingleWord(string word)
	{
		try
		{
			int newEnd = 0, newAttack = 0;
			WordFlags flags = WordFlags.None;
			nodeManager.UpdateNodeListsByWord(word, ref flags, ref newEnd, ref newAttack);

			Log.Information("Adding {word} into database... (flags: {flags})", word, flags);
			if (nodeManager.DbConnection.AddWord(word, flags))
			{
				Log.Information("Successfully Add {word} to database!", word);
				return new AddWordResult(AddWordResultType.Success, newEnd, newAttack);
			}
			else
			{
				Log.Warning("{word} already exists on database.", word);
				return new AddWordResult(AddWordResultType.Duplicate, newEnd, newAttack);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to add {word} to the database.", word);
			return new AddWordResult(AddWordResultType.Failed);
		}
	}

	private bool RemoveSingleWord(string word)
	{
		if (string.IsNullOrWhiteSpace(word))
			return false;

		try
		{
			return nodeManager.DbConnection.DeleteWord(word) > 0;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to remove {word} from the database.", word);
			return false;
		}
	}
	#endregion

	#region Batch job
	private struct WordBatchResult
	{
		public int SuccessCount;
		public int DuplicateCount;
		public int FailedCount;
		public int NewEndNode;
		public int NewAttackNode;
	}

	public void BatchAddWord(string[] wordList, BatchJobOptions batchOptions)
	{
		if (wordList == null)
			throw new ArgumentNullException(nameof(wordList));

		var onlineVerify = batchOptions.HasFlag(BatchJobOptions.VerifyBeforeAdd);
		if (onlineVerify && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
			// FIXME: Replace with event
			// MessageBox.Show("끄투 사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", _namespace, MessageBoxButton.OK, MessageBoxImage.Warning);
			return;

		new DatabaseImportEventArgs("Batch word addition").TriggerDatabaseImportStart();

		Log.Information("{0} elements queued.", wordList.Length);

		Task.Run(() =>
		{
			WordBatchResult result = PerformBatchAddWord(wordList, onlineVerify);

			var message = $"{result.SuccessCount} succeed / {result.NewEndNode} new end nodes / {result.NewAttackNode} new attack nodes / {result.DuplicateCount} duplicated / {result.FailedCount} failed";
			Log.Information("Database Operation Complete: {0}", message);
			new DatabaseImportEventArgs("Batch word addition", message).TriggerDatabaseImportDone();
		});
	}

	private WordBatchResult PerformBatchAddWord(string[] wordlist, bool onlineVerify)
	{
		var result = new WordBatchResult();
		foreach (var word in wordlist)
		{
			if (string.IsNullOrWhiteSpace(word))
				continue;

			// Check word length
			if (word.Length <= 1)
			{
				Log.Warning("{word} is too short to add!", word);
				result.FailedCount++;
				continue;
			}

			if (!onlineVerify || word.VerifyWordOnline())
			{
				var singleResult = AddSingleWord(word);
				switch (singleResult.ResultType)
				{
					case AddWordResultType.Success:
						result.SuccessCount++;
						break;

					case AddWordResultType.Duplicate:
						result.DuplicateCount++;
						break;

					default:
						result.FailedCount++;
						break;
				}

				result.NewEndNode += singleResult.NewEndNode;
				result.NewAttackNode += singleResult.NewAttackNode;
			}
		}

		return result;
	}

	public void BatchRemoveWord(string[] wordlist)
	{
		if (wordlist == null)
			throw new ArgumentNullException(nameof(wordlist));

		new DatabaseImportEventArgs("Batch word removal").TriggerDatabaseImportStart();

		Log.Information("{0} elements queued.", wordlist.Length);

		Task.Run(() =>
		{
			int SuccessCount = 0, FailedCount = 0;
			foreach (var word in wordlist)
			{
				if (RemoveSingleWord(word))
					SuccessCount++;
				else
					FailedCount++;
			}

			var message = $"{SuccessCount} deleted / {FailedCount} failed";
			Log.Information("Batch remove operation complete: {0}", message);
			new DatabaseImportEventArgs("Batch word removal", message).TriggerDatabaseImportDone();
		});
	}
	#endregion
}
