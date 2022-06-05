using AutoKkutu.Constants;
using AutoKkutu.Databases;
using AutoKkutu.Databases.Extension;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AutoKkutu.Utils
{
	public static class DatabaseCheckUtils
	{
		public static readonly Logger Logger = LogManager.GetLogger(nameof(DatabaseCheckUtils));

		/// <summary>
		/// 데이터베이스의 무결성을 검증하고, 문제를 발견하면 수정합니다.
		/// </summary>
		/// <param name="UseOnlineDB">온라인 검사(끄투 사전을 통한 검사)를 진행하는지의 여부</param>
		public static void CheckDB(this DatabaseWithDefaultConnection database, bool UseOnlineDB)
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

					int totalElementCount = Convert.ToInt32(database.DefaultConnection.RequireNotNull().ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListTableName}"), CultureInfo.InvariantCulture);
					Logger.Info(CultureInfo.CurrentCulture, "Database has Total {0} elements.", totalElementCount);

					int currentElementIndex = 0, DeduplicatedCount = 0, RemovedCount = 0, FixedCount = 0;

					var deletionList = new List<string>();
					Dictionary<string, string> wordFixList = new(), wordIndexCorrection = new(), reverseWordIndexCorrection = new(), kkutuIndexCorrection = new();
					var flagCorrection = new Dictionary<string, (int, int)>();

					Logger.Info("Opening auxiliary SQLite connection...");
					using (CommonDatabaseConnection auxiliaryConnection = database.OpenSecondaryConnection())
					{
						// Deduplicate
						DeduplicatedCount = auxiliaryConnection.DeduplicateDatabaseAndGetCount();

						// Refresh node lists
						auxiliaryConnection.RefreshNodeLists();

						// Check for errorsd
						using (CommonDatabaseCommand _command = auxiliaryConnection.CreateCommand($"SELECT * FROM {DatabaseConstants.WordListTableName} ORDER BY({DatabaseConstants.WordColumnName}) DESC"))
						{
							using DbDataReader reader = _command.ExecuteReader();
							Logger.Info("Searching problems...");

							int wordOrdinal = reader.GetOrdinal(DatabaseConstants.WordColumnName);
							watch.Start();
							while (reader.Read())
							{
								currentElementIndex++;
								string content = reader.GetString(wordOrdinal);
								Logger.Info(CultureInfo.CurrentCulture, "Total {0} of {1} ('{2}')", totalElementCount, currentElementIndex, content);

								// Check word validity
								if (IsInvalid(content))
								{
									Logger.Info("Not a valid word; Will be removed.");
									deletionList.Add(content);
									continue;
								}

								// Online verify
								if (UseOnlineDB && !auxiliaryConnection.CheckElementOnline(content))
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
							Logger.Info(CultureInfo.CurrentCulture, "Done searching problems. Took {0}ms.", watch.ElapsedMilliseconds);
						}

						watch.Restart();

						// Start fixing
						RemovedCount += auxiliaryConnection.DeleteElements(deletionList);
						FixedCount += auxiliaryConnection.FixIndex(wordFixList, DatabaseConstants.WordColumnName, null);
						FixedCount += auxiliaryConnection.FixIndex(wordIndexCorrection, DatabaseConstants.WordIndexColumnName, DatabaseUtils.GetLaFHeadNode);
						FixedCount += auxiliaryConnection.FixIndex(reverseWordIndexCorrection, DatabaseConstants.ReverseWordIndexColumnName, DatabaseUtils.GetFaLHeadNode);
						FixedCount += auxiliaryConnection.FixIndex(kkutuIndexCorrection, DatabaseConstants.KkutuWordIndexColumnName, DatabaseUtils.GetKkutuHeadNode);
						FixedCount += auxiliaryConnection.FixFlag(flagCorrection);

						watch.Stop();
						Logger.Info(CultureInfo.CurrentCulture, "Done fixing problems. Took {0}ms.", watch.ElapsedMilliseconds);

						auxiliaryConnection.ExecuteVacuum();
					}

					Logger.Info(CultureInfo.CurrentCulture, "Database check completed: Total {0} / Removed {1} / Deduplicated {2} / Fixed {3}.", totalElementCount, RemovedCount, DeduplicatedCount, FixedCount);

					DatabaseEvents.TriggerDatabaseIntegrityCheckDone(new DataBaseIntegrityCheckDoneEventArgs($"{RemovedCount + DeduplicatedCount} 개 항목 제거됨 / {FixedCount} 개 항목 수정됨"));
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Exception while checking database");
				}
			});
		}

		/// <summary>
		/// (지원되는 DBMS에 한해) Vacuum 작업을 실행합니다.
		/// </summary>
		private static void ExecuteVacuum(this CommonDatabaseConnection connection)
		{
			var watch = new Stopwatch();
			Logger.Info("Executing vacuum...");
			watch.Restart();
			connection.PerformVacuum();
			watch.Stop();
			Logger.Info(CultureInfo.CurrentCulture, "Vacuum took {0}ms.", watch.ElapsedMilliseconds);
		}

		private static int FixFlag(this CommonDatabaseConnection connection, Dictionary<string, (int, int)> FlagCorrection)
		{
			int count = 0;
			using (CommonDatabaseCommand command = connection.CreateCommand($"UPDATE {DatabaseConstants.WordListTableName} SET flags = @flags WHERE {DatabaseConstants.WordColumnName} = @word;"))
			{
				command.AddParameters(connection.CreateParameter(CommonDatabaseType.CharacterVarying, byte.MaxValue, "@word", "_"));
				command.AddParameters(connection.CreateParameter(CommonDatabaseType.SmallInt, "@flags", 0));
				command.TryPrepare();

				foreach (KeyValuePair<string, (int, int)> pair in FlagCorrection)
				{
					command.UpdateParameter("@word", pair.Key);
					command.UpdateParameter("@flags", pair.Value.Item2);

					if (command.ExecuteNonQuery() > 0)
					{
						Logger.Info(CultureInfo.CurrentCulture, "Fixed {column:l} of {word}: from {from} to {to}.", DatabaseConstants.FlagsColumnName, pair.Key, (WordDatabaseAttributes)pair.Value.Item1, (WordDatabaseAttributes)pair.Value.Item2);
						count++;
					}
				}
			}

			return count;
		}

		private static int FixIndex(this CommonDatabaseConnection connection, Dictionary<string, string> WordIndexCorrection, string indexColumnName, Func<string, string>? correctIndexSupplier)
		{
			int count = 0;
			using (CommonDatabaseCommand command = connection.CreateCommand($"UPDATE {DatabaseConstants.WordListTableName} SET {indexColumnName} = @correctIndex WHERE {DatabaseConstants.WordColumnName} = @word;"))
			{
				var parameters = new CommonDatabaseParameter[2] {
					connection.CreateParameter(CommonDatabaseType.CharacterVarying, byte.MaxValue, "@word", "_"), connection.CreateParameter(CommonDatabaseType.Character, 1, "@correctIndex", 0)
				};
				if (indexColumnName.Equals(DatabaseConstants.KkutuWordIndexColumnName, StringComparison.OrdinalIgnoreCase))
					parameters[1] = connection.CreateParameter(CommonDatabaseType.CharacterVarying, 2, "@correctIndex", 0);
				command.AddParameters(parameters);
				command.TryPrepare();

				foreach (KeyValuePair<string, string> pair in WordIndexCorrection)
				{
					string correctWordIndex;
					if (correctIndexSupplier == null)
						correctWordIndex = pair.Value;
					else
						correctWordIndex = correctIndexSupplier(pair.Key);

					command.UpdateParameter("@word", pair.Key);
					command.UpdateParameter("@correctIndex", correctWordIndex);
					if (command.ExecuteNonQuery() > 0)
					{
						if (correctIndexSupplier == null)
							Logger.Info(CultureInfo.CurrentCulture, "Fixed {column}: from {from} to {to}.", indexColumnName, pair.Key, correctWordIndex);
						else
							Logger.Info(CultureInfo.CurrentCulture, "Fixed {column} of {word}: from {from} to {to}.", indexColumnName, pair.Key, pair.Value, correctWordIndex);

						count++;
					}
				}
			}

			return count;
		}

		private static int DeleteElements(this CommonDatabaseConnection connection, IEnumerable<string> DeletionList)
		{
			int count = 0;
			using (CommonDatabaseCommand command = connection.CreateCommand($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = @word"))
			{
				command.AddParameters(connection.CreateParameter(CommonDatabaseType.CharacterVarying, byte.MaxValue, "@word", "_"));
				command.TryPrepare();

				foreach (string word in DeletionList)
				{
					command.UpdateParameter("@word", word);
					if (command.ExecuteNonQuery() > 0)
					{
						Logger.Info(CultureInfo.CurrentCulture, "Removed {word} from database.", word);
						count++;
					}
				}
			}

			return count;
		}

		private static void CheckFlagsColumn(DbDataReader reader, Dictionary<string, (int, int)> FlagCorrection)
		{
			string content = reader.GetString(reader.GetOrdinal(DatabaseConstants.WordColumnName));
			WordDatabaseAttributes correctFlags = DatabaseUtils.GetFlags(content);
			int _correctFlags = (int)correctFlags;
			int currentFlags = reader.GetInt32(reader.GetOrdinal(DatabaseConstants.FlagsColumnName));
			if (_correctFlags != currentFlags)
			{
				Logger.Info(CultureInfo.CurrentCulture, "Invaild flags, will be fixed to {flags}.", correctFlags);
				FlagCorrection.Add(content, (currentFlags, _correctFlags));
			}
		}

		private static void CheckIndexColumn(DbDataReader reader, string indexColumnName, Func<string, string> correctIndexSupplier, Dictionary<string, string> toBeCorrectedTo)
		{
			string content = reader.GetString(reader.GetOrdinal(DatabaseConstants.WordColumnName));
			string correctWordIndex = correctIndexSupplier(content);
			string currentWordIndex = reader.GetString(reader.GetOrdinal(indexColumnName));
			if (correctWordIndex != currentWordIndex)
			{
				Logger.Info(CultureInfo.CurrentCulture, "Invaild {indexColumn} column, will be fixed to {correctIndex}.", indexColumnName, correctWordIndex);
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

			char first = content[0];
			if (first is '(' or '{' or '[' or '-' or '.')
				return true;

			char last = content.Last();
			if (last is ')' or '}' or ']')
				return true;

			return content.Contains(' ', StringComparison.Ordinal) || content.Contains(':', StringComparison.Ordinal);
		}

		/// <summary>
		/// 단어 노드 목록들(한방 단어 노드 목록, 공격 단어 노드 목록 등)을 데이터베이스로부터 다시 로드합니다.
		/// </summary>
		private static void RefreshNodeLists(this CommonDatabaseConnection connection)
		{
			var watch = new Stopwatch();
			watch.Start();
			Logger.Info("Updating node lists...");
			try
			{
				PathFinder.UpdateNodeLists(connection);
				watch.Stop();
				Logger.Info(CultureInfo.CurrentCulture, "Done refreshing node lists. Took {0}ms.", watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to refresh node lists");
			}
		}

		private static int DeduplicateDatabaseAndGetCount(this CommonDatabaseConnection connection)
		{
			int count = 0;
			var watch = new Stopwatch();
			watch.Start();
			Logger.Info("Deduplicating entries...");
			try
			{
				count = connection.DeduplicateDatabase();
				watch.Stop();
				Logger.Info(CultureInfo.CurrentCulture, "Removed {0} duplicate entries. Took {1}ms.", count, watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Deduplication failed");
			}
			return count;
		}

		/// <summary>
		/// 끄투 사전 기능을 이용하여 단어가 해당 서버의 데이터베이스에 존재하는지 검사하고, 만약 존재하지 않는다면 데이터베이스에서 삭제합니다.
		/// </summary>
		/// <param name="word">검사할 단어</param>
		/// <returns>해당 단어가 서버에서 사용할 수 있는지의 여부</returns>
		private static bool CheckElementOnline(this CommonDatabaseConnection connection, string word)
		{
			bool result = BatchJobUtils.CheckOnline(word.Trim());
			if (!result)
				connection.ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = @word", connection.CreateParameter("@word", word));

			return result;
		}
	}
}
