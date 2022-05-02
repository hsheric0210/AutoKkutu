using AutoKkutu.Databases;
using log4net;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static AutoKkutu.Constants;

namespace AutoKkutu.Utils
{
	public static class BatchJobUtils
	{
		private const string _namespace = nameof(BatchJobUtils);
		private static ILog Logger = LogManager.GetLogger(_namespace);

		/// <summary>
		/// Check if the word is available in the current server using the official kkutu dictionary feature.
		/// </summary>
		/// <param name="word">The word to check</param>
		/// <returns>True if existence is verified, false otherwise.</returns>
		public static bool CheckOnline(string word)
		{
			Logger.InfoFormat("Finding '{0}' in kkutu dictionary...", word);

			// Enter the word to dictionary search field
			JSEvaluator.EvaluateJS($"document.getElementById('dict-input').value = '{word}'");

			// Click search button
			JSEvaluator.EvaluateJS("document.getElementById('dict-search').click()");

			// Wait for response
			Thread.Sleep(1500);

			// Query the response
			string result = JSEvaluator.EvaluateJS("document.getElementById('dict-output').innerHTML");
			Logger.InfoFormat("Server Response : {0}", result);
			if (string.IsNullOrWhiteSpace(result) || string.Equals(result, "404: 유효하지 않은 단어입니다.", StringComparison.OrdinalIgnoreCase))
			{
				Logger.WarnFormat("Can't find '{0}' in kkutu dict.", word);
				return false;
			}
			else if (string.Equals(result, "검색 중", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Warn("Invaild server response. Resend the request.");
				return CheckOnline(word);
			}
			else
			{
				Logger.InfoFormat("Successfully Find '{0}' in kkutu dict.", word);
				return true;
			}
		}

		public static void BatchAddWord(this CommonDatabaseConnection connection, string[] wordlist, BatchWordJobOptions batchFlags, WordFlags wordFlags)
		{
			if (wordlist == null)
				throw new ArgumentNullException(nameof(wordlist));

			bool onlineVerify = batchFlags.HasFlag(BatchWordJobOptions.VerifyBeforeAdd);
			if (onlineVerify && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("끄투 사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", _namespace, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DatabaseEvents.TriggerDatabaseImportStart(new DatabaseImportEventArgs("Batch Add Words"));

			Logger.InfoFormat("{0} elements queued.", wordlist.Length);

			AddWordInfo info = new AddWordInfo
			{
				WordFlags = wordFlags,
				NewEndNodeCount = 0,
				NewAttackNodeCount = 0
			};

			Task.Run(() =>
			{
				int SuccessCount = 0, DuplicateCount = 0, FailedCount = 0, NewEndNode = 0, NewAttackNode = 0;

				foreach (string word in wordlist)
				{
					if (string.IsNullOrWhiteSpace(word))
						continue;

					// Check word length
					if (word.Length <= 1)
					{
						Logger.WarnFormat("'{0}' is too short to add!", word);
						FailedCount++;
						continue;
					}

					if (!onlineVerify || CheckOnline(word))
						switch (connection.AddSingleWord(word, ref info))
						{
							case AddWordResult.Success:
								SuccessCount++;
								break;

							case AddWordResult.Duplicate:
								DuplicateCount++;
								break;

							default:
								FailedCount++;
								break;
						}
				}

				string message = $"{SuccessCount} succeed / {NewEndNode} new end nodes / {NewAttackNode} new attack nodes / {DuplicateCount} duplicated / {FailedCount} failed";
				Logger.InfoFormat("Database Operation Complete: {0}", message);
				MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{message}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);
				DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs("Batch Add Word", message));
			});
		}

		private enum AddWordResult
		{
			Success,
			Duplicate,
			Failed
		}

		private struct AddWordInfo
		{
			public WordFlags WordFlags;
			public int NewEndNodeCount;
			public int NewAttackNodeCount;
		}

		private static AddWordResult AddSingleWord(this CommonDatabaseConnection connection, string word, ref AddWordInfo info)
		{
			try
			{
				WordFlags flags = info.WordFlags;
				int newEndNodeCount = 0, newAttackNodeCount = 0;
				DatabaseUtils.CorrectFlags(word, ref flags, ref newEndNodeCount, ref newAttackNodeCount);

				info.NewEndNodeCount = newEndNodeCount;
				info.NewAttackNodeCount = newAttackNodeCount;

				Logger.InfoFormat("Adding'{0}' into database... (flags: {1})", word, flags);
				if (connection.AddWord(word, flags))
				{
					Logger.InfoFormat("Successfully Add '{0}' to database!", word);
					return AddWordResult.Success;
				}
				else
				{
					Logger.WarnFormat("'{0}' already exists on database", word);
					return AddWordResult.Duplicate;
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to add '{word}' to the database", ex);
				return AddWordResult.Failed;
			}
		}

		public static void BatchRemoveWord(this CommonDatabaseConnection connection, string[] wordlist)
		{
			if (wordlist == null)
				throw new ArgumentNullException(nameof(wordlist));

			DatabaseEvents.TriggerDatabaseImportStart(new DatabaseImportEventArgs("Batch Remove Word"));

			Logger.Info($"{wordlist.Length} elements queued.");

			Task.Run(() =>
			{
				int SuccessCount = 0, FailedCount = 0;
				foreach (string word in wordlist)
					if (connection.RemoveSingleWord(word))
						SuccessCount++;
					else
						FailedCount++;

				string message = $"{SuccessCount} deleted / {FailedCount} failed";
				Logger.Info($"Batch remove operation complete: {message}");
				MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{message}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);

				DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs("Batch Remove Word", message));
			});
		}

		private static bool RemoveSingleWord(this CommonDatabaseConnection connection, string word)
		{
			if (string.IsNullOrWhiteSpace(word))
				return false;

			try
			{
				return connection.DeleteWord(word) > 0;
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to remove '{word}' from the database", ex);
				return false;
			}
		}

		public static void BatchAddNode(this CommonDatabaseConnection connection, string content, bool remove, NodeFlags type)
		{
			if (connection == null || string.IsNullOrWhiteSpace(content))
				return;

			var NodeList = content.Trim().Split(Environment.NewLine.ToCharArray());

			int SuccessCount = 0;
			int DuplicateCount = 0;
			int FailedCount = 0;

			DatabaseEvents.TriggerDatabaseImportStart(new DatabaseImportEventArgs(remove ? "Batch Remove Node" : "Batch Add Node"));

			Logger.Info($"{NodeList.Length} elements queued.");
			foreach (string node in NodeList)
			{
				if (string.IsNullOrWhiteSpace(node))
					continue;

				try
				{
					if (remove)
					{
						SuccessCount += connection.DeleteNode(node, type);
					}
					else if (connection.AddNode(node, type))
					{
						Logger.InfoFormat("Successfully add node '{0}'!", node[0]);
						SuccessCount++;
					}
					else
					{
						Logger.WarnFormat("'{0}' is already exists", node[0]);
						DuplicateCount++;
					}
				}
				catch (Exception ex)
				{
					Logger.Error($"Failed to add node '{node[0]}'!", ex);
					FailedCount++;
				}
			}

			string message = $"{SuccessCount} succeed / {DuplicateCount} duplicated / {FailedCount} failed";
			Logger.InfoFormat("Database Operation Complete: {0}", message);
			MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{message}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);

			DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs(remove ? "Batch Remove Node" : "Batch Add Node", message));
		}

		/// <summary>
		/// 데이터베이스에 노드를 추가합니다.
		/// </summary>
		/// <param name="node">추가할 노드</param>
		/// <param name="types">추가할 노드의 속성들</param>
		/// <returns>데이터베이스에 추가된 노드의 총 갯수</returns>
		public static bool AddNode(this CommonDatabaseConnection connection, string node, NodeFlags types)
		{
			if (connection == null || string.IsNullOrWhiteSpace(node))
				return false;

			bool result = false;

			// 한방 단어
			if (types.HasFlag(NodeFlags.EndWord))
				result = connection.AddNode(node, DatabaseConstants.EndWordListTableName) || result;

			// 공격 단어
			if (types.HasFlag(NodeFlags.AttackWord))
				result = connection.AddNode(node, DatabaseConstants.AttackWordListTableName) || result;

			// 앞말잇기 한방 단어
			if (types.HasFlag(NodeFlags.ReverseEndWord))
				result = connection.AddNode(node, DatabaseConstants.ReverseEndWordListTableName) || result;

			// 앞말잇기 공격 단어
			if (types.HasFlag(NodeFlags.ReverseAttackWord))
				result = connection.AddNode(node, DatabaseConstants.ReverseAttackWordListTableName) || result;

			// 끄투 한방 단어
			if (types.HasFlag(NodeFlags.KkutuEndWord))
				result = connection.AddNode(node, DatabaseConstants.KkutuEndWordListTableName) || result;

			// 끄투 공격 단어
			if (types.HasFlag(NodeFlags.KkutuAttackWord))
				result = connection.AddNode(node, DatabaseConstants.KkutuAttackWordListTableName) || result;

			return result;
		}

		/// <summary>
		/// 데이터베이스에서 노드를 삭제합니다.
		/// </summary>
		/// <param name="node">삭제할 노드</param>
		/// <param name="types">삭제할 노드의 속성들</param>
		/// <returns>데이터베이스에서 삭제된 노드의 총 갯수</returns>
		public static int DeleteNode(this CommonDatabaseConnection connection, string node, NodeFlags types)
		{
			if (connection == null || string.IsNullOrWhiteSpace(node))
				return -1;

			int count = 0;

			// 한방 단어
			if (types.HasFlag(NodeFlags.EndWord))
				count += connection.DeleteNode(node, DatabaseConstants.EndWordListTableName);

			// 공격 단어
			if (types.HasFlag(NodeFlags.AttackWord))
				count += connection.DeleteNode(node, DatabaseConstants.AttackWordListTableName);

			// 앞말잇기 한방 단어
			if (types.HasFlag(NodeFlags.ReverseEndWord))
				count += connection.DeleteNode(node, DatabaseConstants.ReverseEndWordListTableName);

			// 앞말잇기 공격 단어
			if (types.HasFlag(NodeFlags.ReverseAttackWord))
				count += connection.DeleteNode(node, DatabaseConstants.ReverseAttackWordListTableName);

			// 끄투 한방 단어
			if (types.HasFlag(NodeFlags.KkutuEndWord))
				count += connection.DeleteNode(node, DatabaseConstants.KkutuEndWordListTableName);

			// 끄투 공격 단어
			if (types.HasFlag(NodeFlags.KkutuAttackWord))
				count += connection.DeleteNode(node, DatabaseConstants.KkutuAttackWordListTableName);

			return count;
		}
	}

	[Flags]
	public enum BatchWordJobOptions
	{
		None = 0,

		/// <summary>
		/// Remove words from the database.
		/// </summary>
		Remove = 1 << 0,

		/// <summary>
		/// Check if the word really exists and available in current server before adding it to the database.
		/// </summary>
		VerifyBeforeAdd = 1 << 1
	}
}
