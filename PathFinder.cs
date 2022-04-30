using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using static AutoKkutu.CommonDatabase;
using static AutoKkutu.Constants;
using AutoKkutu.Databases;

namespace AutoKkutu
{
	[Flags]
	public enum PathFinderFlags
	{
		NONE = 0,
		USING_END_WORD = 1,
		USING_ATTACK_WORD = 2,
		RETRIAL = 4,
		MANUAL_SEARCH = 8
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

		public static AutoKkutuConfiguration CurrentConfig;
		public static AutoKkutuColorPreference CurrentColorPreference;
		public static CommonDatabase Database;

		public static void Init(CommonDatabase database)
		{
			UpdateDatabase(database);
			try
			{
				UpdateNodeLists();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to update node lists", ex);
				if (DBError != null)
					DBError(null, EventArgs.Empty);
			}
		}

		public static void UpdateNodeLists()
		{

			AttackWordList = Database.GetNodeList(DatabaseConstants.AttackWordListTableName);
			EndWordList = Database.GetNodeList(DatabaseConstants.EndWordListTableName);
			ReverseAttackWordList = Database.GetNodeList(DatabaseConstants.ReverseAttackWordListTableName);
			ReverseEndWordList = Database.GetNodeList(DatabaseConstants.ReverseEndWordListTableName);
			KkutuAttackWordList = Database.GetNodeList(DatabaseConstants.KkutuAttackWordListTableName);
			KkutuEndWordList = Database.GetNodeList(DatabaseConstants.KkutuEndWordListTableName);
		}

		public static void UpdateConfig(AutoKkutuConfiguration newConfig)
		{
			CurrentConfig = newConfig;
		}

		public static void UpdateColorPreference(AutoKkutuColorPreference newColorPref)
		{
			CurrentColorPreference = newColorPref;
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
				AddedPathCount = AddNewPaths();

				InexistentPathCount = InexistentPathList.Count;
				Logger.InfoFormat("Get {0} elements from WrongPathList.", InexistentPathCount);
				RemovedPathCount = RemoveInexistentPaths();

				string result = $"{AddedPathCount} of {NewPathCount} added, {RemovedPathCount} of {InexistentPathCount} removed";
				Logger.Info($"Automatic DB Update complete ({result})");
				return result;
			}

			return null;
		}

		private static int RemoveInexistentPaths()
		{
			int count = 0;
			foreach (string word in InexistentPathList)
			{
				try
				{
					count += Database.DeleteWord(word);
				}
				catch (Exception ex)
				{
					Logger.Error($"Can't delete '{word}' from database", ex);
				}
			}

			InexistentPathList = new ConcurrentBag<string>();

			return count;
		}

		private static int AddNewPaths()
		{
			int count = 0;
			foreach (string word in NewPathList)
			{
				WordFlags flags = Utils.GetFlags(word);

				try
				{
					Logger.Debug($"Check and add '{word}' into database. (flags: {flags})");
					if (Database.AddWord(word, flags))
					{
						Logger.Info($"Added '{word}' into database.");
						count++;
					}
				}
				catch (Exception ex)
				{
					Logger.Error($"Can't add '{word}' to database", ex);
				}
			}

			NewPathList = new ConcurrentBag<string>();

			return count;
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
				FinalList.Add(new PathObject(firstChar + new string(missionChar[0], 256), PathObjectFlags.None));
			Random random = new Random();
			for (int i = 0; i < 10; i++)
				FinalList.Add(new PathObject(firstChar + Utils.GenerateRandomString(256, false, random), PathObjectFlags.None));
			watch.Stop();
			NotifyPathUpdate(new UpdatedPathEventArgs(word, missionChar, FindResult.Normal, FinalList.Count, FinalList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
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

				// Flush previous search result
				FinalList = new List<PathObject>();

				// Search words from database
				var WordList = new List<PathObject>();
				try
				{
					WordList = Database.FindWord(wordCondition, missionChar, flags, wordPreference, mode);
					Logger.InfoFormat("Found {0} words. (AttackWord: {1}, EndWord: {2})", WordList.Count, flags.HasFlag(PathFinderFlags.USING_ATTACK_WORD), flags.HasFlag(PathFinderFlags.USING_END_WORD));
				}
				catch (Exception e)
				{
					watch.Stop();
					Logger.Error("Failed to Find Path", e);
					NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, FindResult.Error, 0, 0, 0, flags));
				}

				// Filter out words
				var QualifiedNormalList = (from word in WordList
									   let wordContent = word.Content
									   where (!UnsupportedPathList.Contains(wordContent) && (CurrentConfig.ReturnMode || !PreviousPath.Contains(wordContent)))
									   select word).ToList();

				// If there's no word found (or all words was filtered out)
				if (QualifiedNormalList.Count == 0)
				{
					watch.Stop();
					Logger.Warn("Can't find any path.");
					NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, FindResult.None, WordList.Count, 0, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
					return;
				}

				// Limit the word list size
				int maxCount = CurrentConfig.MaxWords;
				if (QualifiedNormalList.Count > maxCount)
					QualifiedNormalList = QualifiedNormalList.Take(maxCount).ToList();

				// Update final list
				FinalList = QualifiedNormalList;

				watch.Stop();
				Logger.InfoFormat("Total {0} words are ready. ({1}ms)", FinalList.Count, watch.ElapsedMilliseconds);
				NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, FindResult.Normal, WordList.Count, FinalList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
			});
		}

		private static void NotifyPathUpdate(UpdatedPathEventArgs eventArgs)
		{
			if (onPathUpdated != null)
				onPathUpdated(null, eventArgs);
		}

		public static string ConvertToPresentedWord(string path)
		{
			switch (CurrentConfig.Mode)
			{
				case GameMode.Last_and_First:
				case GameMode.Kung_Kung_Tta:
				case GameMode.Free_Last_and_First:
					return Utils.GetLaFTailNode(path);
				case GameMode.First_and_Last:
					return Utils.GetFaLHeadNode(path);
				case GameMode.Middle_and_First:
					if (path.Length > 2 && path.Length % 2 == 1)
						return Utils.GetMaFNode(path);
					break;
				case GameMode.Kkutu:
					return Utils.GetKkutuTailNode(path);
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

			public string Color
			{
				get; private set;
			}

			public string PrimaryImage
			{
				get; private set;
			}
			public string SecondaryImage
			{
				get; private set;
			}

			public bool MakeEndAvailable
			{
				get; private set;
			}

			public bool MakeAttackAvailable
			{
				get; private set;
			}

			public bool MakeNormalAvailable
			{
				get; private set;
			}

			public PathObject(string _content, PathObjectFlags _flags)
			{
				Content = _content;
				Title = _content;

				MakeEndAvailable = !_flags.HasFlag(PathObjectFlags.EndWord);
				MakeAttackAvailable = !_flags.HasFlag(PathObjectFlags.AttackWord);
				MakeNormalAvailable = !MakeEndAvailable || !MakeAttackAvailable;

				bool isMissionWord = _flags.HasFlag(PathObjectFlags.MissionWord);
				string tooltipPrefix = "";
				string mission = isMissionWord ? "미션 " : "";
				if (_flags.HasFlag(PathObjectFlags.EndWord))
				{
					tooltipPrefix = $"한방 {mission}단어: ";
					Color = (isMissionWord ? CurrentColorPreference.EndMissionWordColor : CurrentColorPreference.EndWordColor).ToString();
					PrimaryImage = @"images\skull.png";
				}
				else if (_flags.HasFlag(PathObjectFlags.AttackWord))
				{
					tooltipPrefix = $"공격 {mission}단어: ";
					Color = (isMissionWord ? CurrentColorPreference.AttackMissionWordColor : CurrentColorPreference.AttackWordColor).ToString();
					PrimaryImage = @"images\attack.png";
				}
				else
				{
					Color = isMissionWord ? CurrentColorPreference.MissionWordColor.ToString() : "#FFFFFFFF";
					tooltipPrefix = isMissionWord ? "미션 단어: " : "";
				}
				SecondaryImage = isMissionWord ? @"images\mission.png" : "";

				ToolTip = tooltipPrefix + _content;
			}

			private string GetEndWordListTableName(GameMode mode)
			{
				switch (mode)
				{
					case GameMode.First_and_Last:
						return DatabaseConstants.ReverseEndWordListTableName;
					case GameMode.Kkutu:
						return DatabaseConstants.KkutuEndWordListTableName;
				}

				return DatabaseConstants.EndWordListTableName;
			}

			private string GetAttackWordListTableName(GameMode mode)
			{
				switch (mode)
				{
					case GameMode.First_and_Last:
						return DatabaseConstants.ReverseAttackWordListTableName;
					case GameMode.Kkutu:
						return DatabaseConstants.KkutuAttackWordListTableName;
				}

				return DatabaseConstants.AttackWordListTableName;
			}

			private string ToNode(GameMode mode)
			{
				switch (mode)
				{
					case GameMode.First_and_Last:
						return Utils.GetFaLTailNode(Content);
					case GameMode.Middle_and_First:
						if (Content.Length % 2 == 1)
							return Utils.GetMaFNode(Content);
						break;
					case GameMode.Kkutu:
						if (Content.Length > 2)
							return Utils.GetKkutuTailNode(Content);
						break;
				}

				return Utils.GetLaFTailNode(Content);
			}

			public void MakeEnd(GameMode mode, CommonDatabase database)
			{
				string node = ToNode(mode);
				database.DeleteNode(node, GetAttackWordListTableName(mode));
				if (database.AddNode(node, GetEndWordListTableName(mode)))
					Logger.InfoFormat("Successfully marked node '{0}' as EndWord.", node);
				else
					Logger.WarnFormat("Node '{0}' is already marked as EndWord.", node);
			}

			public void MakeAttack(GameMode mode, CommonDatabase database)
			{
				string node = ToNode(mode);
				database.DeleteNode(node, GetEndWordListTableName(mode));
				if (database.AddNode(node, GetAttackWordListTableName(mode)))
					Logger.InfoFormat("Successfully marked node '{0}' as AttackWord.", node);
				else
					Logger.WarnFormat("Node '{0}' is already marked as AttackWord.", node);
			}

			public void MakeNormal(GameMode mode, CommonDatabase database)
			{
				string node = ToNode(mode);
				var a = database.DeleteNode(node, GetEndWordListTableName(mode)) > 0;
				var b = database.DeleteNode(node, GetAttackWordListTableName(mode)) > 0;
				if (a || b)
					Logger.InfoFormat("Successfully marked node '{0}' as NormalWord.", node);
				else
					Logger.WarnFormat("Node '{0}' is already marked as NormalWord.", node);
			}
		}
	}

	[Flags]
	public enum PathObjectFlags
	{
		None = 0,
		EndWord = 1 << 0,
		AttackWord = 1 << 1,
		MissionWord = 1 << 2
	}
}
