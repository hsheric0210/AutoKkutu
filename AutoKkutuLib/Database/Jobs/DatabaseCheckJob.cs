using AutoKkutuLib.Extension;
using AutoKkutuLib.Game.Extension;
using AutoKkutuLib.Node;
using Dapper;
using Serilog;
using System.Diagnostics;

namespace AutoKkutuLib.Database.Jobs;

public class DatabaseCheckJob
{
	private readonly NodeManager nodeManager;
	private AbstractDatabaseConnection DbConnection => nodeManager.DbConnection;

	public DatabaseCheckJob(NodeManager nodeManager) => this.nodeManager = nodeManager;

	#region Main check process
	/// <summary>
	/// 데이터베이스의 무결성을 검증하고, 문제를 발견하면 수정합니다.
	/// </summary>
	/// <param name="UseOnlineDB">온라인 검사(끄투 사전을 통한 검사)를 진행하는지의 여부</param>
	public void CheckDB(bool UseOnlineDB, JsEvaluator jsEvaluator)
	{
		// FIXME: Move to caller
		//if (UseOnlineDB && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
		//	MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 여십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
		//	return;

		DatabaseEvents.TriggerDatabaseIntegrityCheckStart();

		Task.Run(() =>
		{
			try
			{
				var watch = new Stopwatch();

				var totalElementCount = DbConnection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {DatabaseConstants.WordTableName}");
				Log.Information("Database has Total {0} elements.", totalElementCount);

				int currentElementIndex = 0, DeduplicatedCount = 0, RemovedCount = 0, FixedCount = 0;

				var deletionList = new List<string>();
				Dictionary<string, string> wordFixList = new(), wordIndexCorrection = new(), reverseWordIndexCorrection = new(), kkutuIndexCorrection = new();
				var flagCorrection = new Dictionary<string, int>();

				Log.Information("Opening auxiliary SQLite DbConnection...");
				// Deduplicate
				DeduplicatedCount = DeduplicateDatabaseAndGetCount();

				// Refresh node lists
				RefreshNodeLists();

				// Check for errorsd
				Log.Information("Searching problems...");
				watch.Start();
				foreach (WordModel element in DbConnection.Query<WordModel>($"SELECT * FROM {DatabaseConstants.WordTableName} ORDER BY({DatabaseConstants.WordColumnName}) DESC"))
				{
					currentElementIndex++;
					var word = element.Word;
					Log.Information("Total {0} of {1} ({2})", totalElementCount, currentElementIndex, word);

					// Check word validity
					if (IsInvalid(word))
					{
						Log.Information("Invalid word {word}, will be removed.", word);
						deletionList.Add(word);
						continue;
					}

					// Online verify
					if (UseOnlineDB && !jsEvaluator.VerifyWordOnline(word.Trim()))
					{
						deletionList.Add(word);
						continue;
					}

					// Check WordIndex tag
					VerifyWordIndexes(DatabaseConstants.WordIndexColumnName, word, element.WordIndex, WordToNodeExtension.GetLaFHeadNode, wordIndexCorrection);

					// Check ReverseWordIndex tag
					VerifyWordIndexes(DatabaseConstants.ReverseWordIndexColumnName, word, element.ReverseWordIndex, WordToNodeExtension.GetFaLHeadNode, reverseWordIndexCorrection);

					// Check KkutuIndex tag
					VerifyWordIndexes(DatabaseConstants.KkutuWordIndexColumnName, word, element.KkutuWordIndex, WordToNodeExtension.GetKkutuHeadNode, kkutuIndexCorrection);

					// Check Flags
					VerifyWordFlags(word, element.Flags, flagCorrection);
				}
				watch.Stop();
				Log.Information("Done searching problems. Took {0}ms.", watch.ElapsedMilliseconds);

				watch.Restart();

				// Start fixing
				RemovedCount += DeleteWordRange(deletionList);
				FixedCount += ResetWordIndex(wordFixList, DatabaseConstants.WordColumnName);
				FixedCount += ResetWordIndex(wordIndexCorrection, DatabaseConstants.WordIndexColumnName);
				FixedCount += ResetWordIndex(reverseWordIndexCorrection, DatabaseConstants.ReverseWordIndexColumnName);
				FixedCount += ResetWordIndex(kkutuIndexCorrection, DatabaseConstants.KkutuWordIndexColumnName);
				FixedCount += ResetWordFlag(flagCorrection);

				watch.Stop();
				Log.Information("Done fixing problems. Took {0}ms.", watch.ElapsedMilliseconds);

				ExecuteVacuum();

				Log.Information("Database check completed: Total {0} / Removed {1} / Deduplicated {2} / Fixed {3}.", totalElementCount, RemovedCount, DeduplicatedCount, FixedCount);

				new DataBaseIntegrityCheckDoneEventArgs($"{RemovedCount + DeduplicatedCount} 개 항목 제거됨 / {FixedCount} 개 항목 수정됨").TriggerDatabaseIntegrityCheckDone();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception while checking database");
			}
		});
	}
	#endregion

	#region Database checkings
	private void VerifyWordFlags(string word, int currentFlags, IDictionary<string, int> correction)
	{
		WordFlags correctFlags = nodeManager.CalcWordFlags(word);
		var correctFlagsInt = (int)correctFlags;
		if (correctFlagsInt != currentFlags)
		{
			Log.Information("Word {word} has invaild flags {currentFlags}, will be fixed to {correctFlags}.", word, currentFlags, correctFlags);
			correction.Add(word, correctFlagsInt);
		}
	}

	private void VerifyWordIndexes(string wordIndexName, string word, string currentWordIndex, Func<string, string> wordIndexSupplier, IDictionary<string, string> correction)
	{
		var correctWordIndex = wordIndexSupplier(word);
		if (correctWordIndex != currentWordIndex)
		{
			Log.Information("Invaild {wordIndexName} column {currentWordIndex}, will be fixed to {correctWordIndex}.", wordIndexName, currentWordIndex, correctWordIndex);
			correction.Add(word, correctWordIndex);
		}
	}

	/// <summary>
	/// 단어가 올바른 단어인지, 유효하지 않은 문자를 포함하고 있진 않은지 검사합니다.
	/// </summary>
	/// <param name="content">검사할 단어</param>
	/// <returns>단어가 유효할 시 false, 그렇지 않을 경우 true</returns>
	private bool IsInvalid(string content)
	{
		if (content.Length == 1 || int.TryParse(content[0].ToString(), out var _))
			return true;

		var first = content[0];
		if (first is '(' or '{' or '[' or '-' or '.')
			return true;

		var last = content.Last();
		return last is ')' or '}' or ']' ? true : InvalidChars.Any(ch => content.Contains(ch, StringComparison.Ordinal));
	}

	private readonly char[] InvalidChars = new char[] { ' ', ':', ';', '?', '!' };
	#endregion

	#region Database fixings
	private int ResetWordFlag(IDictionary<string, int> correction)
	{
		var Counter = 0;

		foreach (KeyValuePair<string, int> pair in correction)
		{
			var affected = DbConnection.Execute($"UPDATE {DatabaseConstants.WordTableName} SET flags = @Flags WHERE {DatabaseConstants.WordColumnName} = @Word;", new
			{
				Flags = pair.Value,
				Word = pair.Key
			});

			if (affected > 0)
			{
				Log.Information("Reset flags of {word} to {to}.", pair.Key, (WordFlags)pair.Value);
				Counter += affected;
			}
		}
		return Counter;
	}

	private int ResetWordIndex(IDictionary<string, string> correction, string indexColumnName)
	{
		var counter = 0;
		foreach (KeyValuePair<string, string> pair in correction)
		{
			var affected = DbConnection.Execute($"UPDATE {DatabaseConstants.WordTableName} SET {indexColumnName} = @Index WHERE {DatabaseConstants.WordColumnName} = @Word;", new
			{
				WordIndex = pair.Value,
				Word = pair.Key
			});
			if (affected > 0)
			{
				Log.Information("Reset {column} of {word} to {to}.", indexColumnName, pair.Key, pair.Value);
				counter += affected;
			}
		}
		return counter;
	}

	private int DeleteWordRange(IEnumerable<string> range)
	{
		var counter = 0;
		foreach (var word in range)
		{
			var affected = DbConnection.Execute($"DELETE FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} = @Word", new
			{
				Word = word
			});
			if (affected > 0)
			{
				Log.Information("Removed {word} from database.", word);
				counter += affected;
			}
		}
		return counter;
	}
	#endregion

	#region Database freshening
	/// <summary>
	/// (지원되는 DBMS에 한해) Vacuum 작업을 실행합니다.
	/// </summary>
	private void ExecuteVacuum()
	{
		var watch = new Stopwatch();
		Log.Information("Executing vacuum...");
		watch.Restart();
		DbConnection.Query.Vacuum().Execute();
		watch.Stop();
		Log.Information("Vacuum took {0}ms.", watch.ElapsedMilliseconds);
	}

	/// <summary>
	/// 단어 노드 목록들(한방 단어 노드 목록, 공격 단어 노드 목록 등)을 데이터베이스로부터 다시 로드합니다.
	/// </summary>
	private void RefreshNodeLists()
	{
		var watch = new Stopwatch();
		watch.Start();
		Log.Information("Updating node lists...");
		try
		{
			nodeManager.LoadNodeLists(DbConnection);
			watch.Stop();
			Log.Information("Done refreshing node lists. Took {0}ms.", watch.ElapsedMilliseconds);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to refresh node lists");
		}
	}

	private int DeduplicateDatabaseAndGetCount()
	{
		var count = 0;
		var watch = new Stopwatch();
		watch.Start();
		Log.Information("Deduplicating entries...");
		try
		{
			count = DbConnection.Query.Deduplicate().Execute();
			watch.Stop();
			Log.Information("Removed {0} duplicate entries. Took {1}ms.", count, watch.ElapsedMilliseconds);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Deduplication failed");
		}
		return count;
	}
	#endregion
}
