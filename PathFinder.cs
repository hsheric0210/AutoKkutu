﻿using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text;
using static AutoKkutu.CommonDatabase;
using static AutoKkutu.Constants;
using AutoKkutu.Databases;

namespace AutoKkutu
{
	// TODO: 미션 감지 및 단어 선호도 조정 기능 추가
	[Flags]
	public enum PathFinderFlags
	{
		NONE = 0,
		USING_END_WORD = 1,
		USING_ATTACK_WORD = 2,
		RETRIAL = 4,
	}

	public class PathFinder
	{
		private static readonly ILog Logger = LogManager.GetLogger("PathFinder");

		public static List<string> AttackWordList;
		public static List<string> EndWordList;

		public static List<string> ReverseAttackWordList;
		public static List<string> ReverseEndWordList;

		public static List<string> KkutuAttackWordList;
		public static List<string> KkutuEndWordList;

		public static List<PathObject> FinalList;

		public static List<string> PreviousPath = new List<string>();
		public static List<string> UnsupportedPathList = new List<string>();

		public static ConcurrentBag<string> InexistentPathList = new ConcurrentBag<string>();
		public static ConcurrentBag<string> NewPathList = new ConcurrentBag<string>();

		public static event EventHandler<PathFinder.UpdatedPathEventArgs> onPathUpdated;

		public static Configuration CurrentConfig;
		public static CommonDatabase Database;
		
		public static void Init(CommonDatabase database)
		{
			UpdateDatabase(database);
			try
			{
				AttackWordList = Database.GetNodeList(DatabaseConstants.AttackWordListName);
				EndWordList = Database.GetNodeList(DatabaseConstants.EndWordListName);
				ReverseAttackWordList = Database.GetNodeList(DatabaseConstants.ReverseAttackWordListName);
				ReverseEndWordList = Database.GetNodeList(DatabaseConstants.ReverseEndWordListName);
				KkutuAttackWordList = Database.GetNodeList(DatabaseConstants.KkutuAttackWordListName);
				KkutuEndWordList = Database.GetNodeList(DatabaseConstants.KkutuEndWordListName);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to Get End word", ex);
			}
		}

		public static void UpdateConfig(Configuration newConfig)
		{
			CurrentConfig = newConfig;
		}

		public static void UpdateDatabase(CommonDatabase database)
		{
			Database = database;
		}

		public static bool CheckNodePresence(string nodeType, string item, List<string> nodeList, WordFlags theFlag, ref WordFlags flags, bool add = false)
		{
			bool exists = nodeList.Contains(item);
			if (exists)
				flags |= theFlag;
			else if (add && flags.HasFlag(theFlag))
			{
				nodeList.Add(item);
				Logger.Info($"Added new {nodeType} node '{item}");
				return true;
			}
			return false;
		}

		public static string AutoDBUpdate()
		{
			if (!CurrentConfig.AutoDBUpdate)
				return null;

			int NewPathCount = 0;
			int AddedPathCount = 0;
			int InexistentPathCount = 0;
			int RemovedPathCount = 0;

			Logger.Debug("Automatically update the DB based on last game.");
			if (NewPathList.Count + UnsupportedPathList.Count == 0)
				Logger.Warn("No such element in autoupdate list.");
			else
			{
				NewPathCount = NewPathList.Count;
				Logger.DebugFormat("Get {0} elements from NewPathList.", NewPathCount);
				foreach (string word in NewPathList)
				{
					WordFlags flags = Utils.GetFlags(word);

					try
					{
						Logger.Debug($"Check and add '{word}' into database. (flags: {flags})");
						if (Database.AddWord(word, flags))
						{
							Logger.Info($"Added '{word}' into database.");
							AddedPathCount++;
						}
					}
					catch (Exception ex)
					{
						Logger.Error($"Can't add '{word}' to database", ex);
					}
				}
				NewPathList = new ConcurrentBag<string>();

				InexistentPathCount = InexistentPathList.Count;
				Logger.InfoFormat("Get {0} elements from WrongPathList.", InexistentPathCount);
				foreach (string word in InexistentPathList)
				{
					try
					{
						RemovedPathCount += Database.DeleteWord(word);
					}
					catch (Exception ex)
					{
						Logger.Error($"Can't delete '{word}' from database", ex);
					}
				}
				InexistentPathList = new ConcurrentBag<string>();

				string result = $"{AddedPathCount} of {NewPathCount} added, {RemovedPathCount} of {InexistentPathCount} removed";
				Logger.Info($"Automatic DB Update complete ({result})");

				return result;
			}

			return null;
		}

		public static void AddPreviousPath(string word)
		{
			if (!string.IsNullOrWhiteSpace(word))
				PreviousPath.Add(word);
		}

		public static void AddToUnsupportedWord(string word, bool isNonexistent)
		{
			if (!string.IsNullOrWhiteSpace(word))
			{
				UnsupportedPathList.Add(word);
				if (isNonexistent)
					InexistentPathList.Add(word);
			}
		}

		private static void RandomPath(CommonHandler.ResponsePresentedWord word, string missionChar, bool first, PathFinderFlags flags)
		{
			string firstChar = first ? word.Content : "";

			var watch = new Stopwatch();
			watch.Start();
			FinalList = new List<PathObject>();
			if (!string.IsNullOrWhiteSpace(missionChar))
				FinalList.Add(new PathObject(firstChar + new string(missionChar[0], 256), WordFlags.None));
			Random random = new Random();
			for (int i = 0; i < 10; i++)
				FinalList.Add(new PathObject(firstChar + Utils.GenerateRandomString(256, false, random), WordFlags.None));
			watch.Stop();
			if (onPathUpdated != null)
				onPathUpdated(null, new UpdatedPathEventArgs(word, missionChar, FindResult.Normal, FinalList.Count, FinalList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
		}

		public static void FindPath(CommonHandler.ResponsePresentedWord wordCondition, string missionChar, WordPreference wordPreference, GameMode mode, PathFinderFlags flags)
		{
			if (ConfigEnums.IsFreeMode(mode))
			{
				RandomPath(wordCondition, missionChar, mode == GameMode.Free_Last_and_First, flags);
				return;
			}

			if (wordCondition.CanSubstitution)
				Logger.InfoFormat("Finding path for {0} ({1}).", wordCondition.Content, wordCondition.Substitution);
			else
				Logger.InfoFormat("Finding path for {0}.", wordCondition.Content);

			// Prevent watchdog thread from being blocked
			Task.Run(() =>
			{
				var watch = new Stopwatch();
				watch.Start();
				FinalList = new List<PathObject>();
				var WordList = new List<PathObject>();
				var QualifiedNormalList = new List<PathObject>();
				try
				{
					WordList = Database.FindWord(wordCondition, missionChar, flags, wordPreference, mode);
					Logger.InfoFormat("Found {0} words. (AttackWord: {1}, EndWord: {2})", WordList.Count, flags.HasFlag(PathFinderFlags.USING_ATTACK_WORD), flags.HasFlag(PathFinderFlags.USING_END_WORD));

					//if (wordPreference == WordPreference.WORD_LENGTH)
					//{
					//	WordList = DatabaseManager.FindWord(wordCondition, missionChar, flags, false, mode);
					//	Logger.InfoFormat("Found {0} words.", WordList.Count);
					//}
					//else
					//{
					//	if (flags.HasFlag(PathFinderFlags.USING_END_WORD))
					//	{
					//		WordList = DatabaseManager.FindWord(wordCondition, missionChar, flags | PathFinderFlags.USING_END_WORD, true, mode);
					//		Logger.InfoFormat("Find {0} words (EndWord inclued).", WordList.Count);
					//	}
					//	else
					//	{
					//		WordList = DatabaseManager.FindWord(wordCondition, missionChar, (flags & ~PathFinderFlags.USING_END_WORD), false, mode);
					//		Logger.InfoFormat("Found {0} words (EndWord excluded).", WordList.Count);
					//	}
					//}
				}
				catch (Exception e)
				{
					watch.Stop();
					Logger.Error("Failed to Find Path", e);
					if (onPathUpdated != null)
						onPathUpdated(null, new UpdatedPathEventArgs(wordCondition, missionChar, FindResult.Error, 0, 0, 0, flags));
				}
				QualifiedNormalList = (from word in WordList
									   let wordContent = word.Content
									   where (!UnsupportedPathList.Contains(wordContent) && (CurrentConfig.ReturnMode || !PreviousPath.Contains(wordContent)))
									   select word).ToList();

				if (QualifiedNormalList.Count == 0)
				{
					watch.Stop();
					Logger.Warn("Can't find any path.");
					if (onPathUpdated != null)
						onPathUpdated(null, new UpdatedPathEventArgs(wordCondition, missionChar, FindResult.None, WordList.Count, 0, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
					return;
				}

				int maxCount = CurrentConfig.MaxWords;
				if (QualifiedNormalList.Count > maxCount)
					QualifiedNormalList = QualifiedNormalList.Take(maxCount).ToList();

				FinalList = QualifiedNormalList;
				watch.Stop();
				Logger.InfoFormat("Total {0} words are ready. ({1}ms)", FinalList.Count, watch.ElapsedMilliseconds);
				if (onPathUpdated != null)
					onPathUpdated(null, new UpdatedPathEventArgs(wordCondition, missionChar, FindResult.Normal, WordList.Count, FinalList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
			});
		}

		public static string ConvertToWord(string path)
		{
			switch (CurrentConfig.Mode)
			{
				case GameMode.Last_and_First:
				case GameMode.Kung_Kung_Tta:
				case GameMode.Free_Last_and_First:
					return path.First().ToString();
				case GameMode.First_and_Last:
					return path.Last().ToString();
				case GameMode.Middle_and_First:
					break;
				case GameMode.Kkutu:
					if (path.Length >= 2)
						return path.Substring(0, 2);
					return path.First().ToString();
				case GameMode.Typing_Battle:
					break;
				case GameMode.All:
					break;
				case GameMode.Free:
					break;
			}
			return null;
		}

		public enum FindResult
		{
			Normal,
			None,
			Error
		}

		public class UpdatedPathEventArgs : EventArgs
		{
			public CommonHandler.ResponsePresentedWord Word;
			public string MissionChar;
			public FindResult Result;
			public int TotalWordCount;
			public int CalcWordCount;
			public int Time;
			public PathFinderFlags Flags;

			public UpdatedPathEventArgs(CommonHandler.ResponsePresentedWord word, string missionChar, FindResult arg, int totalWordCount = 0, int calcWordCount = 0, int time = 0, PathFinderFlags flags = PathFinderFlags.NONE)
			{
				Word = word;
				MissionChar = missionChar;
				Result = arg;
				TotalWordCount = totalWordCount;
				CalcWordCount = calcWordCount;
				Time = time;
				Flags = flags;
			}
		}

		public class PathObject
		{
			public string Title
			{
				get; private set;
			}

			public string ToolTip
			{
				get; private set;
			}

			public string Content
			{
				get; private set;
			}

			public PathObject(string _content, WordFlags _wordFlags)
			{
				Content = _content;
				Title = _content;
				// TODO: 한방 단어나 공격 단어의 경우, 리스트에 렌더링할 때 다른 색으로 렌더링함으로서 강조 효과 주기
				if (_wordFlags.HasFlag(WordFlags.EndWord))
					ToolTip = "이 단어는 한방 단어로, 이을 수 있는 다음 단어가 없습니다.";
				else
					ToolTip = _content;
			}
		}
	}
}
