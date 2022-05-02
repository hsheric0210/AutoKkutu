using AutoKkutu.Databases;
using AutoKkutu.Utils;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static AutoKkutu.Constants;

namespace AutoKkutu
{
	public class PathFinder
	{
		// Node lists
		public static ICollection<string> AttackWordList;

		public static CommonDatabaseConnection Connection;
		public static AutoKkutuColorPreference CurrentColorPreference;
		public static AutoKkutuConfiguration CurrentConfig;
		public static ICollection<string> EndWordList;
		public static List<PathObject> FinalList;
		public static ConcurrentBag<string> InexistentPathList = new ConcurrentBag<string>();
		public static ICollection<string> KkutuAttackWordList;
		public static ICollection<string> KkutuEndWordList;
		public static ConcurrentBag<string> NewPathList = new ConcurrentBag<string>();

		// AutoDBUpdate
		public static List<string> PreviousPath = new List<string>();

		public static ICollection<string> ReverseAttackWordList;
		public static ICollection<string> ReverseEndWordList;
		public static List<string> UnsupportedPathList = new List<string>();
		private static readonly ILog Logger = LogManager.GetLogger(nameof(PathFinder));
		// Events
		public static event EventHandler<UpdatedPathEventArgs> onPathUpdated;

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

		public static string AutoDBUpdate()
		{
			if (!CurrentConfig.AutoDBUpdateEnabled)
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

		public static bool CheckNodePresence(string nodeType, string item, ICollection<string> nodeList, WordFlags theFlag, ref WordFlags flags, bool add = false)
		{
			if (string.IsNullOrEmpty(nodeType))
				throw new ArgumentNullException(nameof(nodeType));
			if (string.IsNullOrWhiteSpace(item))
				throw new ArgumentException("Argument is null or blank", nameof(nodeType));
			if (nodeList == null)
				throw new ArgumentNullException(nameof(nodeList));

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

		public static string ConvertToPresentedWord(string path)
		{
			switch (CurrentConfig.GameMode)
			{
				case GameMode.Last_and_First:
				case GameMode.Kung_Kung_Tta:
				case GameMode.Free_Last_and_First:
					return path.GetLaFTailNode();

				case GameMode.First_and_Last:
					return path.GetFaLHeadNode();

				case GameMode.Middle_and_First:
					if (path.Length > 2 && path.Length % 2 == 1)
						return path.GetMaFNode();
					break;

				case GameMode.Kkutu:
					return path.GetKkutuTailNode();

				case GameMode.Typing_Battle:
					break;

				case GameMode.All:
					break;

				case GameMode.Free:
					break;
			}

			return null;
		}

		public static void FindPath(FindWordInfo info)
		{
			if (ConfigEnums.IsFreeMode(info.Mode))
			{
				RandomPath(info);
				return;
			}

			var wordCondition = info.Word;
			var missionChar = info.MissionChar;
			var flags = info.PathFinderFlags;
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
				ICollection<PathObject> WordList = null;
				try
				{
					WordList = Connection.FindWord(info);
					Logger.InfoFormat("Found {0} words. (AttackWord: {1}, EndWord: {2})", WordList.Count, flags.HasFlag(PathFinderInfo.UseAttackWord), flags.HasFlag(PathFinderInfo.UseEndWord));
				}
				catch (Exception e)
				{
					watch.Stop();
					Logger.Error("Failed to Find Path", e);
					NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, PathFinderResult.Error, 0, 0, 0, flags));
					return;
				}

				// Filter out words
				var QualifiedNormalList = (from word in WordList
										   let wordContent = word.Content
										   where (!UnsupportedPathList.Contains(wordContent) && (CurrentConfig.ReturnModeEnabled || !PreviousPath.Contains(wordContent)))
										   select word).ToList();

				// If there's no word found (or all words was filtered out)
				if (QualifiedNormalList.Count == 0)
				{
					watch.Stop();
					Logger.Warn("Can't find any path.");
					NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, PathFinderResult.None, WordList.Count, 0, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
					return;
				}

				// Limit the word list size
				int maxCount = CurrentConfig.MaxDisplayedWordCount;
				if (QualifiedNormalList.Count > maxCount)
					QualifiedNormalList = QualifiedNormalList.Take(maxCount).ToList();

				// Update final list
				FinalList = QualifiedNormalList;

				watch.Stop();
				Logger.InfoFormat("Total {0} words are ready. ({1}ms)", FinalList.Count, watch.ElapsedMilliseconds);
				NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, PathFinderResult.Normal, WordList.Count, FinalList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
			});
		}

		public static void Init(CommonDatabaseConnection connection)
		{
			Connection = connection;

			try
			{
				UpdateNodeLists(connection);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to update node lists", ex);
				DatabaseEvents.TriggerDatabaseError();
			}
		}

		public static void UpdateColorPreference(AutoKkutuColorPreference newColorPref)
		{
			CurrentColorPreference = newColorPref;
		}

		public static void UpdateConfig(AutoKkutuConfiguration newConfig)
		{
			CurrentConfig = newConfig;
		}

		public static void UpdateNodeLists(CommonDatabaseConnection connection)
		{
			AttackWordList = connection.GetNodeList(DatabaseConstants.AttackWordListTableName);
			EndWordList = connection.GetNodeList(DatabaseConstants.EndWordListTableName);
			ReverseAttackWordList = connection.GetNodeList(DatabaseConstants.ReverseAttackWordListTableName);
			ReverseEndWordList = connection.GetNodeList(DatabaseConstants.ReverseEndWordListTableName);
			KkutuAttackWordList = connection.GetNodeList(DatabaseConstants.KkutuAttackWordListTableName);
			KkutuEndWordList = connection.GetNodeList(DatabaseConstants.KkutuEndWordListTableName);
		}

		private static int AddNewPaths()
		{
			int count = 0;
			foreach (string word in NewPathList)
			{
				WordFlags flags = DatabaseUtils.GetFlags(word);

				try
				{
					Logger.Debug($"Check and add '{word}' into database. (flags: {flags})");
					if (Connection.AddWord(word, flags))
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

		private static void NotifyPathUpdate(UpdatedPathEventArgs eventArgs)
		{
			if (onPathUpdated != null)
				onPathUpdated(null, eventArgs);
		}

		private static void RandomPath(FindWordInfo info)
		{
			var word = info.Word;
			var missionChar = info.MissionChar;
			string firstChar = (info.Mode == GameMode.Free_Last_and_First) ? word.Content : "";

			var watch = new Stopwatch();
			watch.Start();
			FinalList = new List<PathObject>();
			if (!string.IsNullOrWhiteSpace(missionChar))
				FinalList.Add(new PathObject(firstChar + new string(missionChar[0], 256), PathObjectOptions.None, 256));
			Random random = new Random();
			for (int i = 0; i < 10; i++)
				FinalList.Add(new PathObject(firstChar + RandomUtils.GenerateRandomString(256, false, random), PathObjectOptions.None, 256));
			watch.Stop();
			NotifyPathUpdate(new UpdatedPathEventArgs(word, missionChar, PathFinderResult.Normal, FinalList.Count, FinalList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), info.PathFinderFlags));
		}

		private static int RemoveInexistentPaths()
		{
			int count = 0;
			foreach (string word in InexistentPathList)
			{
				try
				{
					count += Connection.DeleteWord(word);
				}
				catch (Exception ex)
				{
					Logger.Error($"Can't delete '{word}' from database", ex);
				}
			}

			InexistentPathList = new ConcurrentBag<string>();

			return count;
		}
	}

	public class PathObject
	{
		public string Color
		{
			get; private set;
		}

		public string Content
		{
			get; private set;
		}

		public bool MakeAttackAvailable
		{
			get; private set;
		}

		public bool MakeEndAvailable
		{
			get; private set;
		}

		public bool MakeNormalAvailable
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

		public string Title
		{
			get; private set;
		}

		public string ToolTip
		{
			get; private set;
		}

		private static readonly ILog Logger = LogManager.GetLogger(nameof(PathFinder));

		public PathObject(string content, PathObjectOptions flags, int missionCharCount)
		{
			var colorPref = PathFinder.CurrentColorPreference;

			Content = content;
			Title = content;

			MakeEndAvailable = !flags.HasFlag(PathObjectOptions.EndWord);
			MakeAttackAvailable = !flags.HasFlag(PathObjectOptions.AttackWord);
			MakeNormalAvailable = !MakeEndAvailable || !MakeAttackAvailable;

			bool isMissionWord = flags.HasFlag(PathObjectOptions.MissionWord);
			string tooltipPrefix = "";
			string mission = isMissionWord ? $"미션({missionCharCount}) " : "";
			if (flags.HasFlag(PathObjectOptions.EndWord))
			{
				tooltipPrefix = $"한방 {mission}단어: ";
				Color = (isMissionWord ? colorPref.EndMissionWordColor : colorPref.EndWordColor).ToString(CultureInfo.InvariantCulture);
				PrimaryImage = @"images\skull.png";
			}
			else if (flags.HasFlag(PathObjectOptions.AttackWord))
			{
				tooltipPrefix = $"공격 {mission}단어: ";
				Color = (isMissionWord ? colorPref.AttackMissionWordColor : colorPref.AttackWordColor).ToString(CultureInfo.InvariantCulture);
				PrimaryImage = @"images\attack.png";
			}
			else
			{
				Color = isMissionWord ? colorPref.MissionWordColor.ToString(CultureInfo.InvariantCulture) : "#FFFFFFFF";
				tooltipPrefix = isMissionWord ? $"미션({missionCharCount}) 단어: " : "";
			}
			SecondaryImage = isMissionWord ? @"images\mission.png" : "";

			ToolTip = tooltipPrefix + content;
		}

		public void MakeAttack(GameMode mode, CommonDatabaseConnection connection)
		{
			string node = ToNode(mode);
			connection.DeleteNode(node, GetEndWordListTableName(mode));
			if (connection.AddNode(node, GetAttackWordListTableName(mode)))
				Logger.InfoFormat("Successfully marked node '{0}' as AttackWord.", node);
			else
				Logger.WarnFormat("Node '{0}' is already marked as AttackWord.", node);
		}

		public void MakeEnd(GameMode mode, CommonDatabaseConnection connection)
		{
			string node = ToNode(mode);
			connection.DeleteNode(node, GetAttackWordListTableName(mode));
			if (connection.AddNode(node, GetEndWordListTableName(mode)))
				Logger.InfoFormat("Successfully marked node '{0}' as EndWord.", node);
			else
				Logger.WarnFormat("Node '{0}' is already marked as EndWord.", node);
		}

		public void MakeNormal(GameMode mode, CommonDatabaseConnection connection)
		{
			string node = ToNode(mode);
			var a = connection.DeleteNode(node, GetEndWordListTableName(mode)) > 0;
			var b = connection.DeleteNode(node, GetAttackWordListTableName(mode)) > 0;
			if (a || b)
				Logger.InfoFormat("Successfully marked node '{0}' as NormalWord.", node);
			else
				Logger.WarnFormat("Node '{0}' is already marked as NormalWord.", node);
		}

		private static string GetAttackWordListTableName(GameMode mode)
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

		private static string GetEndWordListTableName(GameMode mode)
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

		private string ToNode(GameMode mode)
		{
			switch (mode)
			{
				case GameMode.First_and_Last:
					return Content.GetFaLTailNode();

				case GameMode.Middle_and_First:
					if (Content.Length % 2 == 1)
						return Content.GetMaFNode();
					break;

				case GameMode.Kkutu:
					if (Content.Length > 2)
						return Content.GetKkutuTailNode();
					break;
			}

			return Content.GetLaFTailNode();
		}
	}

	public class UpdatedPathEventArgs : EventArgs
	{
		public int CalcWordCount
		{
			get; private set;
		}

		public PathFinderInfo Flags
		{
			get; private set;
		}

		public string MissionChar
		{
			get; private set;
		}

		public PathFinderResult Result
		{
			get; private set;
		}

		public int Time
		{
			get; private set;
		}

		public int TotalWordCount
		{
			get; private set;
		}

		public ResponsePresentedWord Word
		{
			get; private set;
		}

		public UpdatedPathEventArgs(ResponsePresentedWord word, string missionChar, PathFinderResult arg, int totalWordCount = 0, int calcWordCount = 0, int time = 0, PathFinderInfo flags = PathFinderInfo.None)
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

	[Flags]
	public enum PathFinderInfo
	{
		None = 0,
		UseEndWord = 1 << 0,
		UseAttackWord = 1 << 1,
		Retrial = 1 << 2,
		ManualSearch = 1 << 3
	}

	public enum PathFinderResult
	{
		Normal,
		None,
		Error
	}

	[Flags]
	public enum PathObjectOptions
	{
		None = 0,
		EndWord = 1 << 0,
		AttackWord = 1 << 1,
		MissionWord = 1 << 2
	}

	public struct FindWordInfo : IEquatable<FindWordInfo>
	{
		public string MissionChar
		{
			get; set;
		}

		public GameMode Mode
		{
			get; set;
		}

		public PathFinderInfo PathFinderFlags
		{
			get; set;
		}

		public ResponsePresentedWord Word
		{
			get; set;
		}

		public WordPreference WordPreference
		{
			get; set;
		}

		public static bool operator !=(FindWordInfo left, FindWordInfo right) => !(left == right);

		public static bool operator ==(FindWordInfo left, FindWordInfo right) => left.Equals(right);

		public override bool Equals(object obj)
		{
			if (!(obj is FindWordInfo))
				return false;

			return Equals((FindWordInfo)obj);
		}

		public bool Equals(FindWordInfo other) =>
				MissionChar.Equals(other.MissionChar, StringComparison.OrdinalIgnoreCase)
				&& Mode == other.Mode
				&& PathFinderFlags == other.PathFinderFlags
				&& Word == other.Word
				&& WordPreference == other.WordPreference;

		public override int GetHashCode() => HashCode.Combine(MissionChar, Mode, PathFinderFlags, Word, WordPreference);
	}
}
