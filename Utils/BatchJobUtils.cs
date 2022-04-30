using AutoKkutu.Databases;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static AutoKkutu.Constants;

namespace AutoKkutu
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
			if (string.IsNullOrWhiteSpace(result) || string.Equals(result, "404: 유효하지 않은 단어입니다.", StringComparison.InvariantCulture))
			{
				Logger.WarnFormat("Can't find '{0}' in kkutu dict.", word);
				return false;
			}
			else if (string.Equals(result, "검색 중", StringComparison.InvariantCulture))
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

		public static void BatchAddWord(this CommonDatabase database, string[] wordlist, BatchWordJobFlags mode, WordFlags flags)
		{
			bool onlineVerify = mode.HasFlag(BatchWordJobFlags.VerifyBeforeAdd);
			if (onlineVerify && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("끄투 사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", _namespace, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (CommonDatabase.ImportStart != null)
				CommonDatabase.ImportStart(null, new DBImportEventArgs("Batch Add Word"));

			Logger.InfoFormat("{0} elements queued.", wordlist.Length);

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
						Logger.ErrorFormat("'{0}' is too short to add!", word);
						FailedCount++;
						continue;
					}

					if (!onlineVerify || CheckOnline(word))
					{
						try
						{
							DatabaseUtils.CorrectFlags(word, ref flags, ref NewEndNode, ref NewAttackNode);

							Logger.InfoFormat("Adding'{0}' into database... (flags: {1})", word, flags);
							if (database.AddWord(word, flags))
							{
								SuccessCount++;
								Logger.InfoFormat("Successfully Add '{0}' to database!", word);
							}
							else
							{
								DuplicateCount++;
								Logger.WarnFormat("'{0}' already exists on database", word);
							}
						}
						catch (Exception ex)
						{
							Logger.Error($"Failed to add '{word}' to the database", ex);
							FailedCount++;
						}
					}
				}

				Logger.Info($"Database Operation Complete. {SuccessCount} added / {NewEndNode} new end nodes / {NewAttackNode} new attack nodes / {DuplicateCount} duplicated / {FailedCount} failed.");

				string StatusMessage = $"{SuccessCount} 개 추가 성공 / {NewEndNode} 개의 새로운 한방 노드 / {NewAttackNode} 개의 새로운 공격 노드 / {DuplicateCount} 개 중복 / {FailedCount} 개 실패";
				MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{StatusMessage}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);

				if (CommonDatabase.ImportDone != null)
					CommonDatabase.ImportDone(null, new DBImportEventArgs("Batch Add Word"));
			});
		}

		public static void BatchRemoveWord(this CommonDatabase database, string[] wordlist)
		{
			if (CommonDatabase.ImportStart != null)
				CommonDatabase.ImportStart(null, new DBImportEventArgs("Batch Remove Word"));

			Logger.Info($"{wordlist.Length} elements queued.");

			Task.Run(() =>
			{
				int SuccessCount = 0, FailedCount = 0;

				foreach (string word in wordlist)
				{
					if (string.IsNullOrWhiteSpace(word))
						continue;

					try
					{
						if (database.DeleteWord(word) > 0)
							SuccessCount++;
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed to remove '{word}' from the database", ex);
						FailedCount++;
					}
				}

				Logger.Info($"Batch remove operation complete: {SuccessCount} removed / {FailedCount} failed.");

				string StatusMessage = $"{SuccessCount} 개 제거 성공 / {FailedCount} 개 제거 실패";
				MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{StatusMessage}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);

				if (CommonDatabase.ImportDone != null)
					CommonDatabase.ImportDone(null, new DBImportEventArgs("Batch Remove Word"));
			});
		}

		public static void BatchAddNode(this CommonDatabase database, string content, bool remove, NodeFlags type)
		{
			var NodeList = content.Trim().Split(Environment.NewLine.ToCharArray());

			int SuccessCount = 0;
			int DuplicateCount = 0;
			int FailedCount = 0;

			if (CommonDatabase.ImportStart != null)
				CommonDatabase.ImportStart(null, new DBImportEventArgs(remove ? "Batch Remove Node" : "Batch Add Node"));

			Logger.Info($"{NodeList.Length} elements queued.");
			foreach (string node in NodeList)
			{
				if (string.IsNullOrWhiteSpace(node))
					continue;
				try
				{
					if (remove)
					{
						SuccessCount += database.DeleteNode(node, type);
					}
					else if (database.AddNode(node, type))
					{
						Logger.Info(string.Format("Successfully add node '{0}'!", node[0]));
						SuccessCount++;
					}
					else
					{
						Logger.Warn($"'{node[0]}' is already exists");
						DuplicateCount++;
					}
				}
				catch (Exception ex)
				{
					Logger.Error($"Failed to add node '{node[0]}'!", ex);
					FailedCount++;
				}
			}

			Logger.Info($"Database Operation Complete. {SuccessCount} Success / {DuplicateCount} Duplicated / {FailedCount} Failed.");
			string message = $"성공적으로 작업을 수행했습니다. \n{SuccessCount} 개 성공 / {DuplicateCount} 개 중복 / {FailedCount} 개 실패";
			MessageBox.Show(message, _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			
			if (CommonDatabase.ImportDone != null)
				CommonDatabase.ImportDone(null, new DBImportEventArgs(remove ? "Batch Remove Node" : "Batch Add Node", message));
		}

		/// <summary>
		/// 데이터베이스에 노드를 추가합니다.
		/// </summary>
		/// <param name="node">추가할 노드</param>
		/// <param name="types">추가할 노드의 속성들</param>
		/// <returns>데이터베이스에 추가된 노드의 총 갯수</returns>
		public static bool AddNode(this CommonDatabase database, string node, NodeFlags types)
		{
			bool result = false;

			// 한방 단어
			if (types.HasFlag(NodeFlags.EndWord))
				result = database.AddNode(node, DatabaseConstants.EndWordListTableName) || result;

			// 공격 단어
			if (types.HasFlag(NodeFlags.AttackWord))
				result = database.AddNode(node, DatabaseConstants.AttackWordListTableName) || result;

			// 앞말잇기 한방 단어
			if (types.HasFlag(NodeFlags.ReverseEndWord))
				result = database.AddNode(node, DatabaseConstants.ReverseEndWordListTableName) || result;

			// 앞말잇기 공격 단어
			if (types.HasFlag(NodeFlags.ReverseAttackWord))
				result = database.AddNode(node, DatabaseConstants.ReverseAttackWordListTableName) || result;

			// 끄투 한방 단어
			if (types.HasFlag(NodeFlags.KkutuEndWord))
				result = database.AddNode(node, DatabaseConstants.KkutuEndWordListTableName) || result;

			// 끄투 공격 단어
			if (types.HasFlag(NodeFlags.KkutuAttackWord))
				result = database.AddNode(node, DatabaseConstants.KkutuAttackWordListTableName) || result;

			return result;
		}

		/// <summary>
		/// 데이터베이스에서 노드를 삭제합니다.
		/// </summary>
		/// <param name="node">삭제할 노드</param>
		/// <param name="types">삭제할 노드의 속성들</param>
		/// <returns>데이터베이스에서 삭제된 노드의 총 갯수</returns>
		public static int DeleteNode(this CommonDatabase database, string node, NodeFlags types)
		{
			int count = 0;

			// 한방 단어
			if (types.HasFlag(NodeFlags.EndWord))
				count += database.DeleteNode(node, DatabaseConstants.EndWordListTableName);

			// 공격 단어
			if (types.HasFlag(NodeFlags.AttackWord))
				count += database.DeleteNode(node, DatabaseConstants.AttackWordListTableName);

			// 앞말잇기 한방 단어
			if (types.HasFlag(NodeFlags.ReverseEndWord))
				count += database.DeleteNode(node, DatabaseConstants.ReverseEndWordListTableName);

			// 앞말잇기 공격 단어
			if (types.HasFlag(NodeFlags.ReverseAttackWord))
				count += database.DeleteNode(node, DatabaseConstants.ReverseAttackWordListTableName);

			// 끄투 한방 단어
			if (types.HasFlag(NodeFlags.KkutuEndWord))
				count += database.DeleteNode(node, DatabaseConstants.KkutuEndWordListTableName);

			// 끄투 공격 단어
			if (types.HasFlag(NodeFlags.KkutuAttackWord))
				count += database.DeleteNode(node, DatabaseConstants.KkutuAttackWordListTableName);

			return count;
		}

	}

	[Flags]
	public enum BatchWordJobFlags
	{
		Default = 0,

		/// <summary>
		/// Remove words from the database.
		/// </summary>
		Remove = 1 << 0,

		/// <summary>
		/// Check if the word really exists and available in current server before adding it to the database.
		/// </summary>
		VerifyBeforeAdd = 1 << 1
	}

	public class BatchJobEventArgs : EventArgs
	{
		public string Name;

		public BatchJobEventArgs(string name) => Name = name;
	}
}
