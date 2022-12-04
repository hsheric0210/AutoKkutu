using AutoKkutu.Constants;
using AutoKkutu.Database;
using AutoKkutu.Database.Extension;
using AutoKkutu.Modules.Path;
using AutoKkutu.Utils.Extension;
using Dapper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AutoKkutu.Utils
{
	public static partial class DatabaseCheckUtils
	{
		/// <summary>
		/// 데이터베이스의 무결성을 검증하고, 문제를 발견하면 수정합니다.
		/// </summary>
		/// <param name="UseOnlineDB">온라인 검사(끄투 사전을 통한 검사)를 진행하는지의 여부</param>
		public static void CheckDB(this AbstractDatabase database, bool UseOnlineDB)
		{
			if (UseOnlineDB && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 여십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DatabaseEvents.TriggerDatabaseIntegrityCheckStart();

			Task.Run(() =>
			{
				try
				{
					var watch = new Stopwatch();

					int totalElementCount = database.Connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {DatabaseConstants.WordTableName}");
					Log.Information("Database has Total {0} elements.", totalElementCount);

					int currentElementIndex = 0, DeduplicatedCount = 0, RemovedCount = 0, FixedCount = 0;

					var deletionList = new List<string>();
					Dictionary<string, string> wordFixList = new(), wordIndexCorrection = new(), reverseWordIndexCorrection = new(), kkutuIndexCorrection = new();
					var flagCorrection = new Dictionary<string, int>();

					Log.Information("Opening auxiliary SQLite connection...");
					using (AbstractDatabaseConnection auxiliaryConnection = database.OpenSecondaryConnection())
					{
						// Deduplicate
						DeduplicatedCount = auxiliaryConnection.DeduplicateDatabaseAndGetCount();

						// Refresh node lists
						auxiliaryConnection.RefreshNodeLists();

						// Check for errorsd
						Log.Information("Searching problems...");
						watch.Start();
						foreach (WordModel element in auxiliaryConnection.Query<WordModel>($"SELECT * FROM {DatabaseConstants.WordTableName} ORDER BY({DatabaseConstants.WordColumnName}) DESC"))
						{
							currentElementIndex++;
							string word = element.Word;
							Log.Information("Total {0} of {1} ({2})", totalElementCount, currentElementIndex, word);

							// Check word validity
							if (IsInvalid(word))
							{
								Log.Information("Invalid word {word}, will be removed.", word);
								deletionList.Add(word);
								continue;
							}

							// Online verify
							if (UseOnlineDB && !OnlineVerifyExtension.VerifyWordOnline(word.Trim()))
							{
								deletionList.Add(word);
								continue;
							}

							// Check WordIndex tag
							VerifyWordIndexes(DatabaseConstants.WordIndexColumnName, word, element.WordIndex, WordNodeExtension.GetLaFHeadNode, wordIndexCorrection);

							// Check ReverseWordIndex tag
							VerifyWordIndexes(DatabaseConstants.ReverseWordIndexColumnName, word, element.ReverseWordIndex, WordNodeExtension.GetFaLHeadNode, reverseWordIndexCorrection);

							// Check KkutuIndex tag
							VerifyWordIndexes(DatabaseConstants.KkutuWordIndexColumnName, word, element.KkutuWordIndex, WordNodeExtension.GetKkutuHeadNode, kkutuIndexCorrection);

							// Check Flags
							VerifyWordFlags(word, element.Flags, flagCorrection);
						}
						watch.Stop();
						Log.Information("Done searching problems. Took {0}ms.", watch.ElapsedMilliseconds);

						watch.Restart();

						// Start fixing
						RemovedCount += auxiliaryConnection.DeleteWordRange(deletionList);
						FixedCount += auxiliaryConnection.ResetWordIndex(wordFixList, DatabaseConstants.WordColumnName);
						FixedCount += auxiliaryConnection.ResetWordIndex(wordIndexCorrection, DatabaseConstants.WordIndexColumnName);
						FixedCount += auxiliaryConnection.ResetWordIndex(reverseWordIndexCorrection, DatabaseConstants.ReverseWordIndexColumnName);
						FixedCount += auxiliaryConnection.ResetWordIndex(kkutuIndexCorrection, DatabaseConstants.KkutuWordIndexColumnName);
						FixedCount += auxiliaryConnection.ResetWordFlag(flagCorrection);

						watch.Stop();
						Log.Information("Done fixing problems. Took {0}ms.", watch.ElapsedMilliseconds);

						ExecuteVacuum(auxiliaryConnection);
					}

					Log.Information("Database check completed: Total {0} / Removed {1} / Deduplicated {2} / Fixed {3}.", totalElementCount, RemovedCount, DeduplicatedCount, FixedCount);

					DatabaseEvents.TriggerDatabaseIntegrityCheckDone(new DataBaseIntegrityCheckDoneEventArgs($"{RemovedCount + DeduplicatedCount} 개 항목 제거됨 / {FixedCount} 개 항목 수정됨"));
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception while checking database");
				}
			});
		}

		/// <summary>
		/// (지원되는 DBMS에 한해) Vacuum 작업을 실행합니다.
		/// </summary>
		private static void ExecuteVacuum(this AbstractDatabaseConnection connection)
		{
			var watch = new Stopwatch();
			Log.Information("Executing vacuum...");
			watch.Restart();
			connection.ExecuteVacuum();
			watch.Stop();
			Log.Information("Vacuum took {0}ms.", watch.ElapsedMilliseconds);
		}

		private static int ResetWordFlag(this AbstractDatabaseConnection connection, IDictionary<string, int> correction)
		{
			int Counter = 0;

			foreach (KeyValuePair<string, int> pair in correction)
			{
				int affected = connection.Execute($"UPDATE {DatabaseConstants.WordTableName} SET flags = @Flags WHERE {DatabaseConstants.WordColumnName} = @Word;", new
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

		private static int ResetWordIndex(this AbstractDatabaseConnection connection, IDictionary<string, string> correction, string indexColumnName)
		{
			int counter = 0;
			foreach (KeyValuePair<string, string> pair in correction)
			{
				int affected = connection.Execute($"UPDATE {DatabaseConstants.WordTableName} SET {indexColumnName} = @Index WHERE {DatabaseConstants.WordColumnName} = @Word;", new
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

		private static int DeleteWordRange(this AbstractDatabaseConnection connection, IEnumerable<string> range)
		{
			int counter = 0;
			foreach (string word in range)
			{
				int affected = connection.Execute($"DELETE FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} = @Word", new
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

		private static void VerifyWordFlags(string word, int currentFlags, IDictionary<string, int> correction)
		{
			WordFlags correctFlags = DatabaseUtils.GetWordFlags(word);
			int correctFlagsInt = (int)correctFlags;
			if (correctFlagsInt != currentFlags)
			{
				Log.Information("Word {word} has invaild flags {currentFlags}, will be fixed to {correctFlags}.", word, currentFlags, correctFlags);
				correction.Add(word, correctFlagsInt);
			}
		}

		private static void VerifyWordIndexes(string wordIndexName, string word, string currentWordIndex, Func<string, string> wordIndexSupplier, IDictionary<string, string> correction)
		{
			string correctWordIndex = wordIndexSupplier(word);
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
		private static bool IsInvalid(string content)
		{
			if (content.Length == 1 || int.TryParse(content[0].ToString(), out int _))
				return true;

			char first = content[0];
			if (first is '(' or '{' or '[' or '-' or '.')
				return true;

			char last = content.Last();
			if (last is ')' or '}' or ']')
				return true;

			return InvalidChars.Any(ch => content.Contains(ch, StringComparison.Ordinal));
		}

		private static readonly char[] InvalidChars = new char[] { ' ', ':', ';', '?', '!' };

		/// <summary>
		/// 단어 노드 목록들(한방 단어 노드 목록, 공격 단어 노드 목록 등)을 데이터베이스로부터 다시 로드합니다.
		/// </summary>
		private static void RefreshNodeLists(this AbstractDatabaseConnection connection)
		{
			var watch = new Stopwatch();
			watch.Start();
			Log.Information("Updating node lists...");
			try
			{
				PathManager.LoadNodeLists(connection);
				watch.Stop();
				Log.Information("Done refreshing node lists. Took {0}ms.", watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to refresh node lists");
			}
		}

		private static int DeduplicateDatabaseAndGetCount(this AbstractDatabaseConnection connection)
		{
			int count = 0;
			var watch = new Stopwatch();
			watch.Start();
			Log.Information("Deduplicating entries...");
			try
			{
				count = connection.DeduplicateDatabase();
				watch.Stop();
				Log.Information("Removed {0} duplicate entries. Took {1}ms.", count, watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Deduplication failed");
			}
			return count;
		}
	}
}
