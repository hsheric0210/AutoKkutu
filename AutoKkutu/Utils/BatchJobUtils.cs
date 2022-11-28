using AutoKkutu.Constants;
using AutoKkutu.Databases;
using AutoKkutu.Databases.Extension;
using AutoKkutu.Modules;
using AutoKkutu.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AutoKkutu.Utils
{
	public static class BatchJobUtils
	{
		private const string _namespace = nameof(BatchJobUtils);

		/// <summary>
		/// Check if the word is available in the current server using the official kkutu dictionary feature.
		/// </summary>
		/// <param name="word">The word to check</param>
		/// <returns>True if existence is verified, false otherwise.</returns>
		public static bool CheckOnline(string word)
		{
			Log.Information(I18n.BatchJob_CheckOnline, word);

			// Enter the word to dictionary search field
			JSEvaluator.EvaluateJS(nameof(CheckOnline), $"document.getElementById('dict-input').value = '{word}'");

			// Click search button
			JSEvaluator.EvaluateJS(nameof(CheckOnline), "document.getElementById('dict-search').click()");

			// Wait for response
			Thread.Sleep(1500);

			// Query the response
			string result = JSEvaluator.EvaluateJS(nameof(CheckOnline), "document.getElementById('dict-output').innerHTML");
			Log.Information(I18n.BatchJob_CheckOnline_Response, result);
			if (string.IsNullOrWhiteSpace(result) || string.Equals(result, "404: 유효하지 않은 단어입니다.", StringComparison.OrdinalIgnoreCase))
			{
				Log.Warning(I18n.BatchJob_CheckOnline_NotFound, word);
				return false;
			}
			else if (string.Equals(result, "검색 중", StringComparison.OrdinalIgnoreCase))
			{
				Log.Warning(I18n.BatchJob_CheckOnline_InvalidResponse);
				return CheckOnline(word);
			}
			else
			{
				Log.Information(I18n.BatchJob_CheckOnline_Found, word);
				return true;
			}
		}

		private struct BatchAddWordInfo
		{
			public int SuccessCount;
			public int DuplicateCount;
			public int FailedCount;
			public int NewEndWord;
			public int NewAttackWord;
		}

		public static void BatchAddWord(this DbSet<WordModel> table, string[] wordlist, BatchWordJobOptions batchFlags, WordDbTypes WordDatabaseAttributes)
		{
			if (wordlist == null)
				throw new ArgumentNullException(nameof(wordlist));

			bool onlineVerify = batchFlags.HasFlag(BatchWordJobOptions.VerifyBeforeAdd);
			if (onlineVerify && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS(nameof(BatchAddWord), "document.getElementById('dict-output').style")))
			{
				MessageBox.Show("끄투 사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", _namespace, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DatabaseEvents.TriggerDatabaseImportStart(new DatabaseImportEventArgs("Batch Add Words"));

			Log.Information("{0} elements queued.", wordlist.Length);

			var info = new AddWordInfo
			{
				WordDatabaseAttributes = WordDatabaseAttributes,
				NewEndWordCount = 0,
				NewAttackWordCount = 0
			};

			Task.Run(() =>
			{
				BatchAddWordInfo result = PerformBatchAddWord(table, wordlist, onlineVerify, ref info);

				string message = $"{result.SuccessCount} succeed / {result.NewEndWord} new end nodes / {result.NewAttackWord} new attack nodes / {result.DuplicateCount} duplicated / {result.FailedCount} failed";
				Log.Information("Database Operation Complete: {0}", message);
				DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs("Batch Add Word", message));
				MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{message}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			});
		}

		private static BatchAddWordInfo PerformBatchAddWord(DbSet<WordModel> table, string[] wordlist, bool onlineVerify, ref AddWordInfo info)
		{
			var result = new BatchAddWordInfo();
			foreach (string word in wordlist)
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

				if (!onlineVerify || CheckOnline(word))
				{
					switch (table.AddSingleWord(word, ref info))
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

				result.NewEndWord += info.NewEndWordCount;
				result.NewAttackWord += info.NewAttackWordCount;
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
			public WordDbTypes WordDatabaseAttributes;
			public int NewEndWordCount;
			public int NewAttackWordCount;
		}

		private static AddWordResult AddSingleWord(this DbSet<WordModel> table, string word, ref AddWordInfo info)
		{
			try
			{
				WordDbTypes flags = info.WordDatabaseAttributes;
				int newEndNodeCount = 0, newAttackNodeCount = 0;
				DatabaseUtils.CorrectFlags(word, ref flags, ref newEndNodeCount, ref newAttackNodeCount);

				info.NewEndWordCount = newEndNodeCount;
				info.NewAttackWordCount = newAttackNodeCount;

				Log.Information("Adding {word} into database... (flags: {flags})", word, flags);
				if (table.AddWord(word, flags))
				{
					Log.Information("Successfully Add {word} to database!", word);
					return AddWordResult.Success;
				}
				else
				{
					Log.Warning("{word} already exists on database", word);
					return AddWordResult.Duplicate;
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to add {word} to the database", word);
				return AddWordResult.Failed;
			}
		}

		public static void BatchRemoveWord(this DbSet<WordModel> table, string[] wordlist)
		{
			if (wordlist == null)
				throw new ArgumentNullException(nameof(wordlist));

			DatabaseEvents.TriggerDatabaseImportStart(new DatabaseImportEventArgs("Batch Remove Word"));

			Log.Information("{0} elements queued.", wordlist.Length);

			Task.Run(() =>
			{
				int SuccessCount = 0, FailedCount = 0;
				foreach (string word in wordlist)
				{
					if (table.TryDeleteWord(word))
						SuccessCount++;
					else
						FailedCount++;
				}

				string message = $"{SuccessCount} deleted / {FailedCount} failed";
				Log.Information("Batch remove operation complete: {0}", message);
				DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs("Batch Remove Word", message));
				MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{message}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			});
		}

		private static bool TryDeleteWord(this DbSet<WordModel> table, string word)
		{
			if (string.IsNullOrWhiteSpace(word))
				return false;

			try
			{
				return table.DeleteWord(word) > 0;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to remove {word} from the database", word);
				return false;
			}
		}

		public static void BatchAddNode(this PathDbContext context, string content, bool remove, NodeTypes type)
		{
			if (context is null || string.IsNullOrWhiteSpace(content))
				return;

			string[] NodeList = content.Trim().Split(Environment.NewLine.ToCharArray());

			int SuccessCount = 0;
			int DuplicateCount = 0;
			int FailedCount = 0;

			DatabaseEvents.TriggerDatabaseImportStart(new DatabaseImportEventArgs(remove ? "Batch Remove Node" : "Batch Add Node"));

			Log.Information("{0} elements queued.", NodeList.Length);
			foreach (string node in NodeList)
			{
				if (string.IsNullOrWhiteSpace(node))
					continue;

				try
				{
					if (remove)
					{
						SuccessCount += context.DeleteNode(node, type);
					}
					else if (context.AddNode(node, type))
					{
						Log.Information("Successfully add node {node}!", node[0]);
						SuccessCount++;
					}
					else
					{
						Log.Warning("{node} already exists.", node[0]);
						DuplicateCount++;
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to add node {node}!", node[0]);
					FailedCount++;
				}
			}

			string message = $"{SuccessCount} succeed / {DuplicateCount} duplicated / {FailedCount} failed";
			Log.Information("Database Operation Complete: {0}", message);
			DatabaseEvents.TriggerDatabaseImportDone(new DatabaseImportEventArgs(remove ? "Batch Remove Node" : "Batch Add Node", message));
			MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{message}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		/// <summary>
		/// 데이터베이스에 노드를 추가합니다.
		/// </summary>
		/// <param name="node">추가할 노드</param>
		/// <param name="types">추가할 노드의 속성들</param>
		/// <returns>데이터베이스에 추가된 노드의 총 갯수</returns>
		public static bool AddNode(this PathDbContext context, string node, NodeTypes types)
		{
			if (context == null || string.IsNullOrWhiteSpace(node))
				return false;

			bool result = false;

			// 한방 단어
			result |= types.HasFlag(NodeTypes.EndWord) && context.EndWordIndex.AddNode(node);

			// 공격 단어
			result |= types.HasFlag(NodeTypes.AttackWord) && context.AttackWordIndex.AddNode(node);

			// 앞말잇기 한방 단어
			result |= types.HasFlag(NodeTypes.ReverseEndWord) && context.ReverseEndWordIndex.AddNode(node);

			// 앞말잇기 공격 단어
			result |= types.HasFlag(NodeTypes.ReverseAttackWord) && context.ReverseAttackWordIndex.AddNode(node);

			// 끄투 한방 단어
			result |= types.HasFlag(NodeTypes.KkutuEndWord) && context.KkutuEndWordIndex.AddNode(node);

			// 끄투 공격 단어
			result |= types.HasFlag(NodeTypes.KkutuAttackWord) && context.KkutuAttackWordIndex.AddNode(node);

			// 쿵쿵따 한방 단어
			result |= types.HasFlag(NodeTypes.KKTEndWord) && context.EndWordIndex.AddNode(node);

			// 쿵쿵따 공격 단어
			result |= types.HasFlag(NodeTypes.KKTAttackWord) && context.AttackWordIndex.AddNode(node);

			return result;
		}

		/// <summary>
		/// 데이터베이스에서 노드를 삭제합니다.
		/// </summary>
		/// <param name="node">삭제할 노드</param>
		/// <param name="types">삭제할 노드의 속성들</param>
		/// <returns>데이터베이스에서 삭제된 노드의 총 갯수</returns>
		public static int DeleteNode(this PathDbContext context, string node, NodeTypes types)
		{
			if (context == null || string.IsNullOrWhiteSpace(node))
				return -1;

			int count = 0;

			// 한방 단어
			if (types.HasFlag(NodeTypes.EndWord))
				count += context.EndWordIndex.DeleteNode(node);

			// 공격 단어
			if (types.HasFlag(NodeTypes.AttackWord))
				count += context.AttackWordIndex.DeleteNode(node);

			// 앞말잇기 한방 단어
			if (types.HasFlag(NodeTypes.ReverseEndWord))
				count += context.ReverseEndWordIndex.DeleteNode(node);

			// 앞말잇기 공격 단어
			if (types.HasFlag(NodeTypes.ReverseAttackWord))
				count += context.ReverseAttackWordIndex.DeleteNode(node);

			// 끄투 한방 단어
			if (types.HasFlag(NodeTypes.KkutuEndWord))
				count += context.KkutuEndWordIndex.DeleteNode(node);

			// 끄투 공격 단어
			if (types.HasFlag(NodeTypes.KkutuAttackWord))
				count += context.KkutuAttackWordIndex.DeleteNode(node);

			// 쿵쿵따 한방 단어
			if (types.HasFlag(NodeTypes.KKTEndWord))
				count += context.EndWordIndex.DeleteNode(node);

			// 쿵쿵따 공격 단어
			if (types.HasFlag(NodeTypes.KKTAttackWord))
				count += context.AttackWordIndex.DeleteNode(node);

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
