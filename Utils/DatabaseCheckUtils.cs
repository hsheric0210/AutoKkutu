using AutoKkutu.Databases;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static AutoKkutu.Constants;

namespace AutoKkutu
{
	public static class DatabaseCheckUtils
	{
		public static readonly ILog Logger = LogManager.GetLogger(nameof(DatabaseCheckUtils));

		public static EventHandler DBCheckStart;
		public static EventHandler<CheckDBDoneArgs> DBCheckDone;

		/// <summary>
		/// 데이터베이스의 무결성을 검증하고, 문제를 발견하면 수정합니다.
		/// </summary>
		/// <param name="UseOnlineDB">온라인 검사(끄투 사전을 통한 검사)를 진행하는지의 여부</param>
		public static void CheckDB(this CommonDatabase database, bool UseOnlineDB)
		{
			if (UseOnlineDB && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 여십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (DBCheckStart != null)
				DBCheckStart(null, EventArgs.Empty);

			Task.Run(() =>
			{
				try
				{
					var watch = new Stopwatch();

					int totalElementCount = Convert.ToInt32(database.ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListTableName}"));
					Logger.InfoFormat("Database has Total {0} elements.", totalElementCount);

					int currentElementIndex = 0, DeduplicatedCount = 0, RemovedCount = 0, FixedCount = 0;

					var deletionList = new List<string>();
					Dictionary<string, string> wordFixList = new Dictionary<string, string>(), wordIndexCorrection = new Dictionary<string, string>(), reverseWordIndexCorrection = new Dictionary<string, string>(), kkutuIndexCorrection = new Dictionary<string, string>();
					var flagCorrection = new Dictionary<string, (int, int)>();

					Logger.Info("Opening auxiliary SQLite connection...");
					using (var auxiliaryConnection = database.OpenSecondaryConnection())
					{
						// Deduplicate
						database.DeduplicateDatabaseAndGetCount(auxiliaryConnection, ref DeduplicatedCount);

						// Refresh node lists
						RefreshNodeLists();

						// Check for errorsd
						using (CommonDatabaseReader reader = database.ExecuteReader($"SELECT * FROM {DatabaseConstants.WordListTableName} ORDER BY({DatabaseConstants.WordColumnName}) DESC", auxiliaryConnection))
						{
							Logger.Info("Searching problems...");

							watch.Start();
							while (reader.Read())
							{
								currentElementIndex++;
								string content = reader.GetString(DatabaseConstants.WordColumnName);
								Logger.InfoFormat("Total {0} of {1} ('{2}')", totalElementCount, currentElementIndex, content);

								// Check word validity
								if (IsInvalid(content))
								{
									Logger.Info("Not a valid word; Will be removed.");
									deletionList.Add(content);
									continue;
								}

								// Online verify
								if (UseOnlineDB && !database.CheckElementOnline(content))
								{
									deletionList.Add(content);
									continue;
								}

								// Check WordIndex tag
								CheckIndexColumn(reader, DatabaseConstants.WordIndexColumnName, DatabaseUtils.GetLaFHeadNode, wordIndexCorrection);

								// Check ReverseWordIndex tag
								CheckIndexColumn(reader, DatabaseConstants.ReverseWordIndexColumnName, DatabaseUtils.GetFaLHeadNode, reverseWordIndexCorrection);

								// Check KkutuIndex tag
								CheckIndexColumn(reader, DatabaseConstants.KkutuWordIndexColumnName, DatabaseUtils.GetKkutuHeadNode, kkutuIndexCorrection);

								// Check Flags
								CheckFlagsColumn(reader, flagCorrection);
							}
							watch.Stop();
							Logger.InfoFormat("Done searching problems. Took {0}ms.", watch.ElapsedMilliseconds);
						}

						watch.Restart();

						// Start fixing
						database.DeleteElements(deletionList, ref RemovedCount);
						database.FixIndex(wordFixList, DatabaseConstants.WordColumnName, null, ref FixedCount);
						database.FixIndex(wordIndexCorrection, DatabaseConstants.WordIndexColumnName, DatabaseUtils.GetLaFHeadNode, ref FixedCount);
						database.FixIndex(reverseWordIndexCorrection, DatabaseConstants.ReverseWordIndexColumnName, DatabaseUtils.GetFaLHeadNode, ref FixedCount);
						database.FixIndex(kkutuIndexCorrection, DatabaseConstants.KkutuWordIndexColumnName, DatabaseUtils.GetKkutuHeadNode, ref FixedCount);
						database.FixFlag(flagCorrection, ref FixedCount);

						watch.Stop();
						Logger.InfoFormat("Done fixing problems. Took {0}ms.", watch.ElapsedMilliseconds);

						database.ExecuteVacuum();
					}

					Logger.InfoFormat("Database check completed: Total {0} / Removed {1} / Fixed {2}.", totalElementCount, RemovedCount, FixedCount);

					if (DBCheckDone != null)
						DBCheckDone(null, new CheckDBDoneArgs($"{RemovedCount} 개 항목 제거됨 / {FixedCount} 개 항목 수정됨"));
				}
				catch (Exception ex)
				{
					Logger.Error($"Exception while checking database", ex);
				}
			});
		}

		/// <summary>
		/// (지원되는 DBMS에 한해) Vacuum 작업을 실행합니다.
		/// </summary>
		private static void ExecuteVacuum(this CommonDatabase database)
		{
			var watch = new Stopwatch();
			Logger.Info("Executing vacuum...");
			watch.Restart();
			database.PerformVacuum();
			watch.Stop();
			Logger.InfoFormat("Vacuum took {0}ms.", watch.ElapsedMilliseconds);
		}

		private static void FixFlag(this CommonDatabase database, Dictionary<string, (int, int)> FlagCorrection, ref int FixedCount)
		{
			foreach (var pair in FlagCorrection)
			{
				Logger.InfoFormat("Fixed {0} of '{1}': from {2} to {3}.", DatabaseConstants.FlagsColumnName, pair.Key, (WordFlags)pair.Value.Item1, (WordFlags)pair.Value.Item2);
				database.ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET flags = {pair.Value.Item2} WHERE {DatabaseConstants.WordColumnName} = '{pair.Key}';");
				FixedCount++;
			}
		}

		private static void FixIndex(this CommonDatabase database, Dictionary<string, string> WordIndexCorrection, string indexColumnName, Func<string, string> correctIndexSupplier, ref int FixedCount)
		{
			foreach (var pair in WordIndexCorrection)
			{
				string correctWordIndex;
				if (correctIndexSupplier == null)
				{
					correctWordIndex = pair.Value;
					Logger.InfoFormat("Fixed {0}: from '{1}' to '{2}'.", indexColumnName, pair.Key, correctWordIndex);
				}
				else
				{
					correctWordIndex = correctIndexSupplier(pair.Key);
					Logger.InfoFormat("Fixed {0} of '{1}': from '{2}' to '{3}'.", indexColumnName, pair.Key, pair.Value, correctWordIndex);
				}
				database.ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET {indexColumnName} = '{correctWordIndex}' WHERE {DatabaseConstants.WordColumnName} = '{pair.Key}';");
				FixedCount++;
			}
		}

		private static void DeleteElements(this CommonDatabase database, IEnumerable<string> DeletionList, ref int RemovedCount)
		{
			foreach (string content in DeletionList)
			{
				Logger.InfoFormat("Removed '{0}' from database.", content);
				database.ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '" + content + "'");
				RemovedCount++;
			}
		}

		private static void CheckFlagsColumn(CommonDatabaseReader reader, Dictionary<string, (int, int)> FlagCorrection)
		{
			string content = reader.GetString(DatabaseConstants.WordColumnName);
			WordFlags correctFlags = DatabaseUtils.GetFlags(content);
			int _correctFlags = (int)correctFlags;
			int currentFlags = reader.GetInt32(DatabaseConstants.FlagsColumnName);
			if (_correctFlags != currentFlags)
			{
				Logger.InfoFormat("Invaild flags; Will be fixed to '{0}'.", correctFlags);
				FlagCorrection.Add(content, (currentFlags, _correctFlags));
			}
		}

		private static void CheckIndexColumn(CommonDatabaseReader reader, string indexColumnName, Func<string, string> correctIndexSupplier, Dictionary<string, string> toBeCorrectedTo)
		{
			string content = reader.GetString(DatabaseConstants.WordColumnName);
			string correctWordIndex = correctIndexSupplier(content);
			string currentWordIndex = reader.GetString(indexColumnName);
			if (correctWordIndex != currentWordIndex)
			{
				Logger.InfoFormat("Invaild '{0}' column; Will be fixed to '{1}'.", indexColumnName, correctWordIndex);
				toBeCorrectedTo.Add(content, currentWordIndex);
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

			char first = content.First();
			if (first == '(' || first == '{' || first == '[' || first == '-' || first == '.')
				return true;

			char last = content.Last();
			if (last == ')' || last == '}' || last == ']')
				return true;

			return content.Contains(" ") || content.Contains(":");
		}

		/// <summary>
		/// 단어 노드 목록들(한방 단어 노드 목록, 공격 단어 노드 목록 등)을 데이터베이스로부터 다시 로드합니다.
		/// </summary>
		private static void RefreshNodeLists()
		{
			var watch = new Stopwatch();
			watch.Start();
			Logger.Info("Updating node lists...");
			try
			{
				PathFinder.UpdateNodeLists();
				watch.Stop();
				Logger.InfoFormat("Done refreshing node lists. Took {0}ms.", watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to refresh node lists", ex);
			}
		}

		private static void DeduplicateDatabaseAndGetCount(this CommonDatabase database, IDisposable auxiliaryConnection, ref int DeduplicatedCount)
		{
			var watch = new Stopwatch();
			watch.Start();
			Logger.Info("Deduplicating entries...");
			try
			{
				DeduplicatedCount = database.DeduplicateDatabase(auxiliaryConnection);
				watch.Stop();
				Logger.InfoFormat("Removed {0} duplicate entries. Took {1}ms.", DeduplicatedCount, watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Logger.Error("Deduplication failed", ex);
			}
		}


		/// <summary>
		/// 끄투 사전 기능을 이용하여 단어가 해당 서버의 데이터베이스에 존재하는지 검사하고, 만약 존재하지 않는다면 데이터베이스에서 삭제합니다.
		/// </summary>
		/// <param name="word">검사할 단어</param>
		/// <returns>해당 단어가 서버에서 사용할 수 있는지의 여부</returns>
		private static bool CheckElementOnline(this CommonDatabase database, string word)
		{
			bool result = BatchJobUtils.CheckOnline(word.Trim());
			if (!result)
				database.ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = '{word}'");
			return result;
		}
	}

	public class CheckDBDoneArgs : EventArgs
	{
		public string Result;

		public CheckDBDoneArgs(string result)
		{
			Result = result;
		}
	}
}
