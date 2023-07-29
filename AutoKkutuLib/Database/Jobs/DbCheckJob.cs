using AutoKkutuLib.Browser;
using AutoKkutuLib.Database.Path;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Hangul;
using Dapper;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AutoKkutuLib.Database.Jobs;

public class DbCheckJob
{
	private readonly NodeManager nodeManager;
	private DbConnectionBase Db => nodeManager.DbConnection;

	public DbCheckJob(NodeManager nodeManager) => this.nodeManager = nodeManager;

	#region Main check process
	/// <summary>
	/// 데이터베이스의 무결성을 검증하고, 문제를 발견하면 수정합니다.
	/// </summary>
	/// <param name="UseOnlineDB">온라인 검사(끄투 사전을 통한 검사)를 진행하는지의 여부</param>
	public void CheckDB(bool UseOnlineDB, BrowserBase? browser)
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
				var recalc = new WordFlagsRecalculator(nodeManager, null!); // fixme: add themeManager field

				var watch = new Stopwatch();

				var totalElementCount = Db.ExecuteScalar<int>($"SELECT COUNT(*) FROM {DatabaseConstants.WordTableName}");
				LibLogger.Info<DbCheckJob>("Database has Total {0} elements.", totalElementCount);

				int currentElementIndex = 0, DeduplicatedCount = 0, RemovedCount = 0, FixedCount = 0;

				var deletionList = new List<string>();
				Dictionary<string, string> wordIndexCorrection = new(),
					reverseWordIndexCorrection = new(),
					kkutuIndexCorrection = new(),
					choseongCorrection = new();
				var flagCorrection = new Dictionary<string, int>();

				// Deduplicate
				DeduplicatedCount = DeduplicateDatabaseAndGetCount();

				// Refresh node lists
				RefreshNodeLists();

				// Check for errorsd
				LibLogger.Info<DbCheckJob>("Searching problems...");
				watch.Start();
				foreach (var element in Db.Query<WordModel>($"SELECT * FROM {DatabaseConstants.WordTableName} ORDER BY({DatabaseConstants.WordColumnName}) DESC"))
				{
					currentElementIndex++;
					var word = element.Word;
					//LibLogger.Info("Total {0} of {1} ({2})", totalElementCount, currentElementIndex, word);

					// Check word validity
					if (IsInvalid(word))
					{
						LibLogger.Info<DbCheckJob>("Invalid word {word}, will be removed.", word);
						deletionList.Add(word);
						continue;
					}

					// Online verify
					if (UseOnlineDB && browser?.VerifyWordOnline(word.Trim()) == false)
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
					VerifyWordFlags(recalc, word, element.Flags, flagCorrection);

					VerifyChoseong(word, element.Choseong, choseongCorrection);
				}
				watch.Stop();
				LibLogger.Info<DbCheckJob>("Done searching problems. Took {0}ms.", watch.ElapsedMilliseconds);

				watch.Restart();

				// Start fixing
				var transaction = Db.BeginTransaction(); // Speed optimization, especially on SQLite.
				RemovedCount += DeleteWordRange(deletionList);
				FixedCount += UpdateWordColumn(wordIndexCorrection, DatabaseConstants.WordIndexColumnName);
				FixedCount += UpdateWordColumn(reverseWordIndexCorrection, DatabaseConstants.ReverseWordIndexColumnName);
				FixedCount += UpdateWordColumn(kkutuIndexCorrection, DatabaseConstants.KkutuWordIndexColumnName);
				FixedCount += UpdateWordColumn(choseongCorrection, DatabaseConstants.ChoseongColumnName);
				FixedCount += UpdateWordFlagColumn(flagCorrection);
				transaction.Commit();

				watch.Stop();
				LibLogger.Info<DbCheckJob>("Done fixing problems. Took {0}ms.", watch.ElapsedMilliseconds);

				ExecuteVacuum();

				LibLogger.Info<DbCheckJob>("Database check completed: Total {0} / Removed {1} / Deduplicated {2} / Fixed {3}.", totalElementCount, RemovedCount, DeduplicatedCount, FixedCount);

				new DataBaseIntegrityCheckDoneEventArgs($"{RemovedCount + DeduplicatedCount} 개 항목 제거됨 / {FixedCount} 개 항목 수정됨").TriggerDatabaseIntegrityCheckDone();
			}
			catch (Exception ex)
			{
				LibLogger.Error<DbCheckJob>(ex, "Exception while checking database");
			}
		});
	}
	#endregion

	#region Database checkings
	private void VerifyWordFlags(WordFlagsRecalculator recalc, string word, int currentFlags, IDictionary<string, int> correction)
	{
		const int keepFlags = (int)(WordFlags.LoanWord | WordFlags.Dialect | WordFlags.DeadLang | WordFlags.Munhwa);
		var keptFlags = currentFlags & keepFlags;

		var correctFlags = (int)recalc.GetWordFlags(word) | keptFlags;
		if (correctFlags != currentFlags)
		{
			LibLogger.Debug<DbCheckJob>("Word {word} has invaild flags {currentFlags}, will be fixed to {correctFlags}.", word, (WordFlags)currentFlags, (WordFlags)correctFlags);
			correction.Add(word, correctFlags);
		}
	}

	private void VerifyWordIndexes(string wordIndexName, string word, string currentWordIndex, Func<string, string> wordIndexSupplier, IDictionary<string, string> correction)
	{
		var correctWordIndex = wordIndexSupplier(word);
		if (correctWordIndex != currentWordIndex)
		{
			LibLogger.Debug<DbCheckJob>("Invaild {wordIndexName} column {currentWordIndex}, will be fixed to {correctWordIndex}.", wordIndexName, currentWordIndex, correctWordIndex);
			correction.Add(word, correctWordIndex);
		}
	}

	private void VerifyChoseong(string word, string wordChoseong, IDictionary<string, string> correction)
	{
		var newCho = word.GetChoseong();
		if (!string.Equals(newCho, wordChoseong))
		{
			LibLogger.Debug<DbCheckJob>("Invalid choseong '{cho}' for word '{word}' will be fixed to '{newcho}'", wordChoseong, word, newCho);
			correction.Add(word, newCho);
		}
	}

	/// <summary>
	/// 단어가 올바른 단어인지, 유효하지 않은 문자를 포함하고 있진 않은지 검사합니다.
	/// </summary>
	/// <param name="content">검사할 단어</param>
	/// <returns>단어가 유효할 시 false, 그렇지 않을 경우 true</returns>
	private bool IsInvalid(string content)
	{
		if (content.Length == 1)
			return true;

		var first = content[0];
		if (first is '(' or '{' or '[' or '-' or '.')
			return true;

		var last = content.Last();
		return last is ')' or '}' or ']' || SimpleMatch.Any(ch => content.Contains(ch, StringComparison.Ordinal)) || RegexMatch.Match(content).Success;
	}

	private readonly char[] SimpleMatch = new char[] { ' ', ':', ';', '?', '!' };
	private readonly Regex RegexMatch = new("[^a-zA-Z0-9ㄱ-ㅎ가-힣]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
	#endregion

	#region Database fixings
	private int UpdateWordFlagColumn(IDictionary<string, int> correction)
	{
		var Counter = 0;

		foreach (var pair in correction)
		{
			var affected = Db.Execute($"UPDATE {DatabaseConstants.WordTableName} SET {DatabaseConstants.FlagsColumnName} = @Flags WHERE {DatabaseConstants.WordColumnName} = @Word;", new
			{
				Flags = pair.Value,
				Word = pair.Key
			});

			if (affected > 0)
			{
				LibLogger.Debug<DbCheckJob>("Reset flags of {word} to {to}.", pair.Key, (WordFlags)pair.Value);
				Counter += affected;
			}
		}
		return Counter;
	}

	private int UpdateWordColumn(IDictionary<string, string> correction, string indexColumnName)
	{
		var counter = 0;
		foreach (var pair in correction)
		{
			var affected = Db.Execute($"UPDATE {DatabaseConstants.WordTableName} SET {indexColumnName} = @Value WHERE {DatabaseConstants.WordColumnName} = @Word;", new
			{
				Value = pair.Value,
				Word = pair.Key
			});
			if (affected > 0)
			{
				LibLogger.Debug<DbCheckJob>("Reset {column} of {word} to {to}.", indexColumnName, pair.Key, pair.Value);
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
			var affected = Db.Execute($"DELETE FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} = @Word", new
			{
				Word = word
			});
			if (affected > 0)
			{
				LibLogger.Debug<DbCheckJob>("Removed {word} from database.", word);
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
		LibLogger.Info<DbCheckJob>("Executing vacuum...");
		watch.Restart();
		Db.Query.Vacuum().Execute();
		watch.Stop();
		LibLogger.Info<DbCheckJob>("Vacuum took {0}ms.", watch.ElapsedMilliseconds);
	}

	/// <summary>
	/// 단어 노드 목록들(한방 단어 노드 목록, 공격 단어 노드 목록 등)을 데이터베이스로부터 다시 로드합니다.
	/// </summary>
	private void RefreshNodeLists()
	{
		var watch = new Stopwatch();
		watch.Start();
		LibLogger.Info<DbCheckJob>("Updating node lists...");
		try
		{
			nodeManager.LoadNodeLists(Db);
			watch.Stop();
			LibLogger.Info<DbCheckJob>("Done refreshing node lists. Took {0}ms.", watch.ElapsedMilliseconds);
		}
		catch (Exception ex)
		{
			LibLogger.Error<DbCheckJob>(ex, "Failed to refresh node lists");
		}
	}

	private int DeduplicateDatabaseAndGetCount()
	{
		var count = 0;
		var watch = new Stopwatch();
		watch.Start();
		LibLogger.Info<DbCheckJob>("Deduplicating entries...");
		try
		{
			count = Db.Query.Deduplicate().Execute();
			watch.Stop();
			LibLogger.Info<DbCheckJob>("Removed {0} duplicate entries. Took {1}ms.", count, watch.ElapsedMilliseconds);
		}
		catch (Exception ex)
		{
			LibLogger.Error<DbCheckJob>(ex, "Deduplication failed");
		}
		return count;
	}
	#endregion
}
