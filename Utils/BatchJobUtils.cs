using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static AutoKkutu.Constants;

namespace AutoKkutu.Utils
{
	public class BatchJobUtils
	{
		private const string _namespace = nameof(BatchJobUtils);
		private static ILog Logger = LogManager.GetLogger(_namespace);

		public static EventHandler<BatchJobEventArgs> BatchJobStart;
		public static EventHandler<BatchJobEventArgs> BatchJobDone;

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

		public static void BatchAddWord(CommonDatabase database, string[] wordlist, BatchWordJobFlags mode, WordFlags flags)
		{
			bool onlineVerify = mode.HasFlag(BatchWordJobFlags.VerifyBeforeAdd);
			if (onlineVerify && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("끄투 사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", _namespace, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (BatchJobStart != null)
				BatchJobStart(null, new BatchJobEventArgs("Add"));

			Logger.Info($"{wordlist.Length} elements queued.");

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
						Logger.Error($"'{word}' is too short to add!");
						FailedCount++;
						continue;
					}

					if (!onlineVerify || CheckOnline(word))
					{
						try
						{
							DatabaseUtils.CorrectFlags(word, ref flags, ref NewEndNode, ref NewAttackNode);

							Logger.Info($"Adding'{word}' into database... (flags: {flags})");
							if (database.AddWord(word, flags))
							{
								SuccessCount++;
								Logger.Info($"Successfully Add '{word}' to database!");
							}
							else
							{
								DuplicateCount++;
								Logger.Warn($"'{word}' already exists on database");
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

				if (BatchJobDone != null)
					BatchJobDone(null, new BatchJobEventArgs("Add"));
			});
		}

		public static void BatchRemoveWord(CommonDatabase database, string[] wordlist)
		{
			if (BatchJobStart != null)
				BatchJobStart(null, new BatchJobEventArgs("Remove"));

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

				if (BatchJobDone != null)
					BatchJobDone(null, new BatchJobEventArgs("Remove"));
			});
		}

		public static void BatchAddNode(CommonDatabase database, string content, bool remove, NodeFlags type)
		{
			var NodeList = content.Trim().Split(Environment.NewLine.ToCharArray());

			int SuccessCount = 0;
			int DuplicateCount = 0;
			int FailedCount = 0;

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
			MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{SuccessCount} 개 성공 / {DuplicateCount} 개 중복 / {FailedCount} 개 실패", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
