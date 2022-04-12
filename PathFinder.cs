using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace AutoKkutu
{
	// TODO: 미션 감지 및 단어 선호도 조정 기능 추가
	[Flags]
	public enum PathFinderFlags
	{
		NONE = 0,
		USING_END_WORD = 1,
		RETRIAL = 2
	}

	class PathFinder
	{
		const string RANDOM_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

		private static readonly ILog Logger = LogManager.GetLogger("PathFinder");

		public static List<string> EndWordList;
		public static List<PathObject> FinalList;
		public static List<string> PreviousPath = new List<string>();
		public static List<string> UnsupportedPathList = new List<string>();
		public static ConcurrentBag<string> InexistentPathList = new ConcurrentBag<string>();
		public static ConcurrentBag<string> NewPathList = new ConcurrentBag<string>();

		public static event EventHandler<PathFinder.UpdatedPathEventArgs> UpdatedPath;

		public static Config CurrentConfig;
		public static void Init()
		{
			try
			{
				EndWordList = DatabaseManager.GetEndWordList();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to Get End word", ex);
			}
		}

		public static void UpdateConfig(Config newConfig)
		{
			CurrentConfig = newConfig;
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
					bool isEndWord = EndWordList.Contains(word.Last().ToString());
					try
					{
						Logger.Debug($"Check and add '{word}' into database.");
						if (DatabaseManager.AddWord(word, isEndWord))
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
						RemovedPathCount += DatabaseManager.DeleteWord(word);
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

		private static List<PathObject> QualifyList(List<PathObject> input)
		{
			var result = new List<PathObject>();
			foreach (PathObject o in input)
			{
				if (UnsupportedPathList.Contains(o.Content))
					Logger.DebugFormat("Excluded '{0}' because it is wrong word.", o.Content);
				else if (!CurrentConfig.ReturnMode && PreviousPath.Contains(o.Content))
					Logger.DebugFormat("Excluded '{0}' because it is previously used.", o.Content);
				else
					result.Add(o);
			}
			return result;
		}

		private static void RandomPath(CommonHandler.ResponsePresentedWord word, string missionChar, bool first, PathFinderFlags flags)
		{
			string firstChar = first ? word.Content : "";

			FinalList = new List<PathObject>();
			if (!string.IsNullOrWhiteSpace(missionChar))
				FinalList.Add(new PathObject(firstChar + new string(missionChar[0], 256), false));
			Random random = new Random();
			for (int i = 0; i < 10; i++)
				FinalList.Add(new PathObject(firstChar + new string(Enumerable.Repeat(RANDOM_CHARS, 256).Select(s => s[random.Next(s.Length)]).ToArray()), false));
			if (UpdatedPath != null)
				UpdatedPath(null, new UpdatedPathEventArgs(word, missionChar, FindResult.Normal, FinalList.Count, FinalList.Count, 0, flags));
		}

		public static void FindPath(CommonHandler.ResponsePresentedWord word, string missionChar, WordPreference wordPreference, GameMode mode, PathFinderFlags flags)
		{
			if (ConfigEnums.IsFreeMode(mode))
			{
				RandomPath(word, missionChar, mode == GameMode.Free_Last_and_First, flags);
				return;
			}

			if (word.CanSubstitution)
				Logger.InfoFormat("Finding path for {0} ({1}).", word.Content, word.Substitution);
			else
				Logger.InfoFormat("Finding path for {0}.", word.Content);

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
					if (wordPreference == WordPreference.WORD_LENGTH)
					{
						WordList = DatabaseManager.FindWord(word, missionChar, flags.HasFlag(PathFinderFlags.USING_END_WORD) ? 2 : 0, mode);
						Logger.InfoFormat("Found {0} words.", WordList.Count);
					}
					else
					{
						if (flags.HasFlag(PathFinderFlags.USING_END_WORD))
						{
							WordList = DatabaseManager.FindWord(word, missionChar, 1, mode);
							Logger.InfoFormat("Find {0} words (EndWord inclued).", WordList.Count);
						}
						else
						{
							WordList = DatabaseManager.FindWord(word, missionChar, 0, mode);
							Logger.InfoFormat("Found {0} words (EndWord excluded).", WordList.Count);
						}
						// TODO: Attack word search here
						// AttackWord = DatabaseManager.FindWord(i, 3); // 3 for only attack words
						// Logger.Info(string.Format("Found {0} attack words.", NormalWord.Count), PATHFINDER_WORKER_THREAD_NAME);
					}
				}
				catch (Exception e)
				{
					watch.Stop();
					Logger.Error("Failed to Find Path", e);
					if (UpdatedPath != null)
						UpdatedPath(null, new UpdatedPathEventArgs(word, missionChar, FindResult.Error, 0, 0, 0, flags));
				}
				QualifiedNormalList = QualifyList(WordList);

				if (QualifiedNormalList.Count == 0)
				{
					watch.Stop();
					Logger.Warn("Can't find any path.");
					if (UpdatedPath != null)
						UpdatedPath(null, new UpdatedPathEventArgs(word, missionChar, FindResult.None, WordList.Count, 0, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
					return;
				}
				if (QualifiedNormalList.Count > 20)
					QualifiedNormalList = QualifiedNormalList.Take(20).ToList();

				FinalList = QualifiedNormalList;
				watch.Stop();
				Logger.InfoFormat("Total {0} words are ready. ({1}ms)", FinalList.Count, watch.ElapsedMilliseconds);
				if (UpdatedPath != null)
					UpdatedPath(null, new UpdatedPathEventArgs(word, missionChar, FindResult.Normal, WordList.Count, FinalList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
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

			public bool IsEndWord
			{
				get; private set;
			}

			public PathObject(string _content, bool _isEndWord)
			{
				Content = _content;
				Title = _content;
				if (IsEndWord = _isEndWord)
					ToolTip = "이 단어는 한방 단어로, 이을 수 있는 다음 단어가 없습니다.";
				else
					ToolTip = _content;
			}
		}
	}
}
