using AutoKkutu.Constants;
using AutoKkutu.Databases;
using AutoKkutu.Databases.Extension;
using AutoKkutu.Modules;
using AutoKkutu.Utils;
using NLog;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AutoKkutu.Utils
{
	public static class BatchJobUtils
	{
		private const string _namespace = nameof(BatchJobUtils);
		private static readonly Logger Logger = LogManager.GetLogger(_namespace);

		/// <summary>
		/// Check if the word is available in the current server using the official kkutu dictionary feature.
		/// </summary>
		/// <param name="word">The word to check</param>
		/// <returns>True if existence is verified, false otherwise.</returns>
		public static bool CheckOnline(string word)
		{
			Logger.Info(CultureInfo.CurrentCulture, I18n.BatchJob_CheckOnline, word);

			// Enter the word to dictionary search field
			JSEvaluator.EvaluateJS($"document.getElementById('dict-input').value = '{word}'");

			// Click search button
			JSEvaluator.EvaluateJS("document.getElementById('dict-search').click()");

			// Wait for response
			Thread.Sleep(1500);

			// Query the response
			string result = JSEvaluator.EvaluateJS("document.getElementById('dict-output').innerHTML");
			Logger.Info(CultureInfo.CurrentCulture, I18n.BatchJob_CheckOnline_Response, result);
			if (string.IsNullOrWhiteSpace(result) || string.Equals(result, "404: 유효하지 않은 단어입니다.", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Warn(CultureInfo.CurrentCulture, I18n.BatchJob_CheckOnline_NotFound, word);
				return false;
			}
			else if (string.Equals(result, "검색 중", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Warn(I18n.BatchJob_CheckOnline_InvalidResponse);
				return CheckOnline(word);
			}
			else
			{
				Logger.Info(CultureInfo.CurrentCulture, I18n.BatchJob_CheckOnline_Found, word);
				return true;
			}
		}

		private struct BatchAddWordInfo
		{
			public int SuccessCount;
			public int DuplicateCount;
			public int FailedCount;
			public int NewEndNode;
			public int NewAttackNode;
		}

		public static void BatchAddWord(this CommonDatabaseConnection connection, string[] wordlist, BatchWordJobOptions batchFlags, WordDatabaseAttributes WordDatabaseAttributes)
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

			Logger.Info(CultureInfo.CurrentCulture, "{0} elements queued.", wordlist.Length);

			var info = new AddWordInfo
			{
				WordDatabaseAttributes = WordDatabaseAttributes,
				NewEndNodeCount = 0,
				NewAttackNodeCount = 0
			};

			Task.Run(() =>
			{
				BatchAddWordInfo result = PerformBatchAddWord(connection, wordlist, onlineVerify, ref info);

				string message = $"{result.SuccessCount} succeed / {result.NewEndNode} new end nodes / {result.NewAttackNode} new attack nodes / {result.DuplicateCount} duplicated / {result.FailedCount} failed";
				Logger.Info(CultureInfo.CurrentCulture, "Database Operation Complete: {0}", message);
				DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs("Batch Add Word", message));
				MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{message}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			});
		}

		private static BatchAddWordInfo PerformBatchAddWord(CommonDatabaseConnection connection, string[] wordlist, bool onlineVerify, ref AddWordInfo info)
		{
			var result = new BatchAddWordInfo();
			foreach (string word in wordlist)
			{
				if (string.IsNullOrWhiteSpace(word))
					continue;

				// Check word length
				if (word.Length <= 1)
				{
					Logger.Warn(CultureInfo.CurrentCulture, "{word} is too short to add!", word);
					result.FailedCount++;
					continue;
				}

				if (!onlineVerify || CheckOnline(word))
				{
					switch (connection.AddSingleWord(word, ref info))
					{
						case AddWordResult.Success:
							result.SuccessCount++;
							break;

						case AddWordResult.Duplicate:
							result.DuplicateCount++;
							break;

						default:
							result.FailedCount++;
							break;
					}
				}

				result.NewEndNode += info.NewEndNodeCount;
				result.NewAttackNode += info.NewAttackNodeCount;
			}

			return result;
		}

		private enum AddWordResult
		{
			Success,
			Duplicate,
			Failed
		}

		private struct AddWordInfo
		{
			public WordDatabaseAttributes WordDatabaseAttributes;
			public int NewEndNodeCount;
			public int NewAttackNodeCount;
		}

		private static AddWordResult AddSingleWord(this CommonDatabaseConnection connection, string word, ref AddWordInfo info)
		{
			try
			{
				WordDatabaseAttributes flags = info.WordDatabaseAttributes;
				int newEndNodeCount = 0, newAttackNodeCount = 0;
				DatabaseUtils.CorrectFlags(word, ref flags, ref newEndNodeCount, ref newAttackNodeCount);

				info.NewEndNodeCount = newEndNodeCount;
				info.NewAttackNodeCount = newAttackNodeCount;

				Logger.Info(CultureInfo.CurrentCulture, "Adding {word} into database... (flags: {flags})", word, flags);
				if (connection.AddWord(word, flags))
				{
					Logger.Info(CultureInfo.CurrentCulture, "Successfully Add {word} to database!", word);
					return AddWordResult.Success;
				}
				else
				{
					Logger.Warn(CultureInfo.CurrentCulture, "{word} already exists on database", word);
					return AddWordResult.Duplicate;
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, CultureInfo.CurrentCulture, "Failed to add {word} to the database", word);
				return AddWordResult.Failed;
			}
		}

		public static void BatchRemoveWord(this CommonDatabaseConnection connection, string[] wordlist)
		{
			if (wordlist == null)
				throw new ArgumentNullException(nameof(wordlist));

			DatabaseEvents.TriggerDatabaseImportStart(new DatabaseImportEventArgs("Batch Remove Word"));

			Logger.Info(CultureInfo.CurrentCulture, "{0} elements queued.", wordlist.Length);

			Task.Run(() =>
			{
				int SuccessCount = 0, FailedCount = 0;
				foreach (string word in wordlist)
				{
					if (connection.RemoveSingleWord(word))
						SuccessCount++;
					else
						FailedCount++;
				}

				string message = $"{SuccessCount} deleted / {FailedCount} failed";
				Logger.Info(CultureInfo.CurrentCulture, "Batch remove operation complete: {0}", message);
				DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs("Batch Remove Word", message));
				MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{message}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
				Logger.Error(ex, "Failed to remove {word} from the database", word);
				return false;
			}
		}

		public static void BatchAddNode(this CommonDatabaseConnection connection, string content, bool remove, NodeDatabaseAttributes type)
		{
			if (connection == null || string.IsNullOrWhiteSpace(content))
				return;

			string[] NodeList = content.Trim().Split(Environment.NewLine.ToCharArray());

			int SuccessCount = 0;
			int DuplicateCount = 0;
			int FailedCount = 0;

			DatabaseEvents.TriggerDatabaseImportStart(new DatabaseImportEventArgs(remove ? "Batch Remove Node" : "Batch Add Node"));

			Logger.Info(CultureInfo.CurrentCulture, "{0} elements queued.", NodeList.Length);
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
						Logger.Info(CultureInfo.CurrentCulture, "Successfully add node {node}!", node[0]);
						SuccessCount++;
					}
					else
					{
						Logger.Warn(CultureInfo.CurrentCulture, "{node} already exists.", node[0]);
						DuplicateCount++;
					}
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Failed to add node {node}!", node[0]);
					FailedCount++;
				}
			}

			string message = $"{SuccessCount} succeed / {DuplicateCount} duplicated / {FailedCount} failed";
			Logger.Info(CultureInfo.CurrentCulture, "Database Operation Complete: {0}", message);
			DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs(remove ? "Batch Remove Node" : "Batch Add Node", message));
			MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{message}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		/// <summary>
		/// 데이터베이스에 노드를 추가합니다.
		/// </summary>
		/// <param name="node">추가할 노드</param>
		/// <param name="types">추가할 노드의 속성들</param>
		/// <returns>데이터베이스에 추가된 노드의 총 갯수</returns>
		public static bool AddNode(this CommonDatabaseConnection connection, string node, NodeDatabaseAttributes types)
		{
			if (connection == null || string.IsNullOrWhiteSpace(node))
				return false;

			bool result = false;

			// 한방 단어
			result |= types.HasFlag(NodeDatabaseAttributes.EndWord) && connection.AddNode(node, DatabaseConstants.EndWordListTableName);

			// 공격 단어
			result |= types.HasFlag(NodeDatabaseAttributes.AttackWord) && connection.AddNode(node, DatabaseConstants.AttackWordListTableName);

			// 앞말잇기 한방 단어
			result |= types.HasFlag(NodeDatabaseAttributes.ReverseEndWord) && connection.AddNode(node, DatabaseConstants.ReverseEndWordListTableName);

			// 앞말잇기 공격 단어
			result |= types.HasFlag(NodeDatabaseAttributes.ReverseAttackWord) && connection.AddNode(node, DatabaseConstants.ReverseAttackWordListTableName);

			// 끄투 한방 단어
			result |= types.HasFlag(NodeDatabaseAttributes.KkutuEndWord) && connection.AddNode(node, DatabaseConstants.KkutuEndWordListTableName);

			// 끄투 공격 단어
			result |= types.HasFlag(NodeDatabaseAttributes.KkutuAttackWord) && connection.AddNode(node, DatabaseConstants.KkutuAttackWordListTableName);

			// 쿵쿵따 한방 단어
			result |= types.HasFlag(NodeDatabaseAttributes.KKTEndWord) && connection.AddNode(node, DatabaseConstants.KKTEndWordListTableName);

			// 쿵쿵따 공격 단어
			result |= types.HasFlag(NodeDatabaseAttributes.KKTAttackWord) && connection.AddNode(node, DatabaseConstants.KKTAttackWordListTableName);

			return result;
		}

		/// <summary>
		/// 데이터베이스에서 노드를 삭제합니다.
		/// </summary>
		/// <param name="node">삭제할 노드</param>
		/// <param name="types">삭제할 노드의 속성들</param>
		/// <returns>데이터베이스에서 삭제된 노드의 총 갯수</returns>
		public static int DeleteNode(this CommonDatabaseConnection connection, string node, NodeDatabaseAttributes types)
		{
			if (connection == null || string.IsNullOrWhiteSpace(node))
				return -1;

			int count = 0;

			// 한방 단어
			if (types.HasFlag(NodeDatabaseAttributes.EndWord))
				count += connection.DeleteNode(node, DatabaseConstants.EndWordListTableName);

			// 공격 단어
			if (types.HasFlag(NodeDatabaseAttributes.AttackWord))
				count += connection.DeleteNode(node, DatabaseConstants.AttackWordListTableName);

			// 앞말잇기 한방 단어
			if (types.HasFlag(NodeDatabaseAttributes.ReverseEndWord))
				count += connection.DeleteNode(node, DatabaseConstants.ReverseEndWordListTableName);

			// 앞말잇기 공격 단어
			if (types.HasFlag(NodeDatabaseAttributes.ReverseAttackWord))
				count += connection.DeleteNode(node, DatabaseConstants.ReverseAttackWordListTableName);

			// 끄투 한방 단어
			if (types.HasFlag(NodeDatabaseAttributes.KkutuEndWord))
				count += connection.DeleteNode(node, DatabaseConstants.KkutuEndWordListTableName);

			// 끄투 공격 단어
			if (types.HasFlag(NodeDatabaseAttributes.KkutuAttackWord))
				count += connection.DeleteNode(node, DatabaseConstants.KkutuAttackWordListTableName);

			// 쿵쿵따 한방 단어
			if (types.HasFlag(NodeDatabaseAttributes.KKTEndWord))
				count += connection.DeleteNode(node, DatabaseConstants.KKTEndWordListTableName);

			// 쿵쿵따 공격 단어
			if (types.HasFlag(NodeDatabaseAttributes.KKTAttackWord))
				count += connection.DeleteNode(node, DatabaseConstants.KKTAttackWordListTableName);

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
