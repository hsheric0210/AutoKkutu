using AutoKkutu.Constants;
using AutoKkutu.Database;
using AutoKkutu.Database.Extension;
using AutoKkutu.Utils;
using AutoKkutu.Utils.Extension;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace AutoKkutu.Modules.PathManager
{
	public class PathManager : IPathManager
	{
		private readonly AbstractDatabaseConnection DbConnection;

		public ICollection<string> AttackNodes
		{
			get; private set;
		} = null!;

		public ICollection<string> EndNodes
		{
			get; private set;
		} = null!;

		public ICollection<string> KKTAttackNodes
		{
			get; private set;
		} = null!;

		public ICollection<string> KKTEndNodes
		{
			get; private set;
		} = null!;

		public ICollection<string> KkutuAttackNodes
		{
			get; private set;
		} = null!;

		public ICollection<string> KkutuEndNodes
		{
			get; private set;
		} = null!;

		public ICollection<string> ReverseAttackNodes
		{
			get; private set;
		} = null!;

		public ICollection<string> ReverseEndNodes
		{
			get; private set;
		} = null!;

		/* Path lists */

		public ICollection<string> InexistentPathList { get; } = new HashSet<string>();

		public ICollection<string> NewPathList { get; } = new HashSet<string>();

		public ICollection<string> PreviousPath { get; } = new HashSet<string>();

		public ICollection<string> UnsupportedPathList { get; } = new HashSet<string>();

		public ReaderWriterLockSlim PathListLock
		{
			get;
		} = new();


		public PathManager(AbstractDatabaseConnection connection)
		{
			this.DbConnection = connection;

			try
			{
				UpdateNodeLists();
			}
			catch (Exception ex)
			{
				Log.Error(ex, I18n.PathFinder_Init_Error);
				DatabaseEvents.TriggerDatabaseError();
				throw;
			}
		}

		public void UpdateNodeLists()
		{
			AttackNodes = DbConnection.GetNodeList(DatabaseConstants.AttackNodeIndexTableName);
			EndNodes = DbConnection.GetNodeList(DatabaseConstants.EndNodeIndexTableName);
			ReverseAttackNodes = DbConnection.GetNodeList(DatabaseConstants.ReverseAttackNodeIndexTableName);
			ReverseEndNodes = DbConnection.GetNodeList(DatabaseConstants.ReverseEndNodeIndexTableName);
			KkutuAttackNodes = DbConnection.GetNodeList(DatabaseConstants.KkutuAttackNodeIndexTableName);
			KkutuEndNodes = DbConnection.GetNodeList(DatabaseConstants.KkutuEndNodeIndexTableName);
			KKTAttackNodes = DbConnection.GetNodeList(DatabaseConstants.KKTAttackNodeIndexTableName);
			KKTEndNodes = DbConnection.GetNodeList(DatabaseConstants.KKTEndNodeIndexTableName);
		}

		/* Path-controlling */

		// TODO: perform '리턴 모드' check by caller
		public void AddPreviousPath(string word)
		{
			if (!string.IsNullOrWhiteSpace(word))
				PreviousPath.Add(word);
		}

		public void AddToUnsupportedWord(string word, bool isNonexistent)
		{
			if (!string.IsNullOrWhiteSpace(word))
				try
				{
					PathListLock.EnterWriteLock();
					UnsupportedPathList.Add(word);
					if (isNonexistent)
						InexistentPathList.Add(word);
				}
				finally
				{
					PathListLock.ExitWriteLock();
				}
		}

		/* AutoDatabaseUpdate */

		public string? UpdateDatabase()
		{
			// fixme: this check should be performed by caller, not here.
			//if (!AutoKkutuMain.Configuration.AutoDBUpdateEnabled)
			//	return null;

			Log.Debug(I18n.PathFinder_AutoDBUpdate);
			try
			{
				PathListLock.EnterUpgradeableReadLock();
				int NewPathCount = NewPathList.Count;
				int InexistentPathCount = InexistentPathList.Count;
				if (NewPathCount + InexistentPathCount == 0)
					Log.Warning(I18n.PathFinder_AutoDBUpdate_Empty);
				else
				{
					Log.Debug(I18n.PathFinder_AutoDBUpdate_New, NewPathCount);
					int AddedPathCount = AddNewPaths();

					Log.Information(I18n.PathFinder_AutoDBUpdate_Remove, InexistentPathCount);

					int RemovedPathCount = RemoveInexistentPaths();
					string result = string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AutoDBUpdate_Result, AddedPathCount, NewPathCount, RemovedPathCount, InexistentPathCount);

					Log.Information(I18n.PathFinder_AutoDBUpdate_Finished, result);
					return result;
				}
			}
			finally
			{
				PathListLock.ExitUpgradeableReadLock();
			}

			return null;
		}

		private int AddNewPaths()
		{
			int count = 0;
			var listCopy = new List<string>(NewPathList);
			try
			{
				PathListLock.EnterWriteLock();
				NewPathList.Clear();
			}
			finally
			{
				PathListLock.ExitWriteLock();
			}

			foreach (string word in listCopy)
			{
				WordDbTypes flags = DatabaseUtils.GetFlags(word);

				try
				{
					Log.Debug(I18n.PathFinder_AddPath, word, flags);
					if (DbConnection.AddWord(word, flags))
					{
						Log.Information(I18n.PathFinder_AddPath_Success, word);
						count++;
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, I18n.PathFinder_AddPath_Failed, word);
				}
			}

			return count;
		}

		private int RemoveInexistentPaths()
		{
			int count = 0;
			var listCopy = new List<string>(InexistentPathList);
			try
			{
				PathListLock.EnterWriteLock();
				InexistentPathList.Clear();
			}
			finally
			{
				PathListLock.ExitWriteLock();
			}

			foreach (string word in listCopy)
				try
				{
					count += DbConnection.RequireNotNull().DeleteWord(word);
				}
				catch (Exception ex)
				{
					Log.Error(ex, I18n.PathFinder_RemoveInexistent_Failed, word);
				}

			return count;
		}

		/* Other utility things */

		// TODO: Remove 'noteType' parameter, replace it with enum instead.
		public bool CheckNodePresence(string? nodeType, string node, ICollection<string>? nodeList, WordDbTypes theFlag, ref WordDbTypes flags, bool tryAdd = false)
		{
			if (tryAdd && string.IsNullOrEmpty(nodeType) || string.IsNullOrWhiteSpace(node) || nodeList == null)
				return false;

			bool exists = nodeList.Contains(node);
			if (exists)
			{
				flags |= theFlag;
			}
			else if (tryAdd && flags.HasFlag(theFlag))
			{
				nodeList.Add(node);
				Log.Information(string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AddNode, nodeType, node));
				return true;
			}
			return false;
		}

		// TODO: Move to extension
		public string? ConvertToPresentedWord(GameMode mode, string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException("Parameter is null or blank", nameof(path));

			switch (mode)
			{
				case GameMode.LastAndFirst:
				case GameMode.KungKungTta:
				case GameMode.LastAndFirstFree:
					return path.GetLaFTailNode();

				case GameMode.FirstAndLast:
					return path.GetFaLHeadNode();

				case GameMode.MiddleAndFirst:
					if (path.Length > 2 && path.Length % 2 == 1)
						return path.GetMaFTailNode();
					break;

				case GameMode.Kkutu:
					return path.GetKkutuTailNode();

				case GameMode.TypingBattle:
					break;

				case GameMode.All:
					break;

				case GameMode.Free:
					break;
			}

			return null;
		}

		// 리턴 모드 검사를 여기서 하지 말고, PreviousPath에 단어들을 집어넣는 단계에서 행하는 것이 PreviousPath를 채우지 않기에 메모리 상으로도 더 이득이다.
		public IList<PathObject> CreateQualifiedWordList(IList<PathObject> wordList/*, bool returnMode*/)
		{
			if (wordList is null)
				throw new ArgumentNullException(nameof(wordList));

			var qualifiedList = new List<PathObject>();
			foreach (PathObject word in wordList)
				try
				{
					PathListLock.EnterReadLock();
					if (InexistentPathList.Contains(word.Content))
						word.RemoveQueued = true;
					if (UnsupportedPathList.Contains(word.Content))
						word.Excluded = true;
					else if (PreviousPath.Contains(word.Content))
						word.AlreadyUsed = true;
					else
						qualifiedList.Add(word);
				}
				finally
				{
					PathListLock.ExitReadLock();
				}

			return qualifiedList;
		}

		public void ResetPreviousPath() => PreviousPath.Clear();

		public ICollection<string> GetEndNodeForMode(GameMode mode) => mode switch
		{
			GameMode.FirstAndLast => ReverseEndNodes,
			GameMode.Kkutu => KkutuEndNodes,
			_ => EndNodes,
		};
	}
}
