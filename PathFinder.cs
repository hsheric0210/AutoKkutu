using AutoKkutu.Constants;
using AutoKkutu.Databases;
using AutoKkutu.Databases.Extension;
using AutoKkutu.Utils;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public static class PathFinder
	{
		private static readonly Logger Logger = LogManager.GetLogger(nameof(PathFinder));

		public static ICollection<string>? AttackWordList
		{
			get; private set;
		}

		public static ICollection<string>? EndWordList
		{
			get; private set;
		}

		public static ICollection<string>? KKTAttackWordList
		{
			get; private set;
		}

		public static ICollection<string>? KKTEndWordList
		{
			get; private set;
		}

		public static ICollection<string>? KkutuAttackWordList
		{
			get; private set;
		}

		public static ICollection<string>? KkutuEndWordList
		{
			get; private set;
		}

		public static ICollection<string>? ReverseAttackWordList
		{
			get; private set;
		}

		public static ICollection<string>? ReverseEndWordList
		{
			get; private set;
		}

		public static CommonDatabaseConnection? Connection
		{
			get; private set;
		}

		public static AutoKkutuColorPreference? CurrentColorPreference
		{
			get; private set;
		}

		public static AutoKkutuConfiguration? CurrentConfig
		{
			get; private set;
		}

		public static IList<PathObject> DisplayList
		{
			get; private set;
		} = new List<PathObject>();

		public static IList<PathObject> QualifiedList
		{
			get; private set;
		} = new List<PathObject>();

		public static ICollection<string> InexistentPathList { get; } = new List<string>();

		public static ICollection<string> NewPathList { get; } = new List<string>();

		public static ICollection<string> PreviousPath { get; private set; } = new List<string>();

		public static ICollection<string> UnsupportedPathList { get; } = new List<string>();

		public static readonly ReaderWriterLockSlim PathListLock = new();

		public static event EventHandler<UpdatedPathEventArgs>? OnPathUpdated;

		public static void AddPreviousPath(string word)
		{
			if (!string.IsNullOrWhiteSpace(word))
				PreviousPath.Add(word);
		}

		public static void AddToUnsupportedWord(string word, bool isNonexistent)
		{
			if (!string.IsNullOrWhiteSpace(word))
			{
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
		}

		public static string? AutoDBUpdate()
		{
			if (!CurrentConfig.RequireNotNull().AutoDBUpdateEnabled)
				return null;

			Logger.Debug(I18n.PathFinder_AutoDBUpdate);
			try
			{
				PathListLock.EnterUpgradeableReadLock();
				int NewPathCount = NewPathList.Count;
				int InexistentPathCount = InexistentPathList.Count;
				if (NewPathCount + InexistentPathCount == 0)
				{
					Logger.Warn(I18n.PathFinder_AutoDBUpdate_Empty);
				}
				else
				{
					Logger.Debug(CultureInfo.CurrentCulture, I18n.PathFinder_AutoDBUpdate_New, NewPathCount);
					int AddedPathCount = AddNewPaths();

					Logger.Info(CultureInfo.CurrentCulture, I18n.PathFinder_AutoDBUpdate_Remove, InexistentPathCount);

					int RemovedPathCount = RemoveInexistentPaths();
					string result = string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AutoDBUpdate_Result, AddedPathCount, NewPathCount, RemovedPathCount, InexistentPathCount);

					Logger.Info(CultureInfo.CurrentCulture, I18n.PathFinder_AutoDBUpdate_Finished, result);
					return result;
				}
			}
			finally
			{
				PathListLock.ExitUpgradeableReadLock();
			}

			return null;
		}

		public static bool CheckNodePresence(string? nodeType, string item, ICollection<string>? nodeList, WordDatabaseAttributes theFlag, ref WordDatabaseAttributes flags, bool tryAdd = false)
		{
			if (tryAdd && string.IsNullOrEmpty(nodeType) || string.IsNullOrWhiteSpace(item) || nodeList == null)
				return false;

			bool exists = nodeList.Contains(item);
			if (exists)
			{
				flags |= theFlag;
			}
			else if (tryAdd && flags.HasFlag(theFlag))
			{
				nodeList.Add(item);
				Logger.Info(string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AddNode, nodeType, item));
				return true;
			}
			return false;
		}

		public static string? ConvertToPresentedWord(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException("Parameter is null or blank", nameof(path));

			switch (CurrentConfig.RequireNotNull().GameMode)
			{
				case GameMode.LastAndFirst:
				case GameMode.KungKungTta:
				case GameMode.LastAndFirstFree:
					return path.GetLaFTailNode();

				case GameMode.FirstAndLast:
					return path.GetFaLHeadNode();

				case GameMode.MiddleAndFirst:
					if (path.Length > 2 && path.Length % 2 == 1)
						return path.GetMaFNode();
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

		public static void FindPath(FindWordInfo info)
		{
			CurrentConfig.RequireNotNull();

			if (ConfigEnums.IsFreeMode(info.Mode))
			{
				RandomPath(info);
				return;
			}

			ResponsePresentedWord wordCondition = info.Word;
			string missionChar = info.MissionChar;
			PathFinderOptions flags = info.PathFinderFlags;
			if (wordCondition.CanSubstitution)
				Logger.Info(CultureInfo.CurrentCulture, I18n.PathFinder_FindPath_Substituation, wordCondition.Content, wordCondition.Substitution);
			else
				Logger.Info(CultureInfo.CurrentCulture, I18n.PathFinder_FindPath, wordCondition.Content);

			// Prevent watchdog thread from being blocked
			Task.Run(() =>
			{
				var watch = new Stopwatch();
				watch.Start();

				// Flush previous search result
				DisplayList = new List<PathObject>();
				QualifiedList = new List<PathObject>();

				// Search words from database
				IList<PathObject>? totalWordList = null;
				try
				{
					totalWordList = Connection.RequireNotNull().FindWord(info);
					Logger.Info(CultureInfo.CurrentCulture, I18n.PathFinder_FoundPath, totalWordList.Count, flags.HasFlag(PathFinderOptions.UseAttackWord), flags.HasFlag(PathFinderOptions.UseEndWord));
				}
				catch (Exception e)
				{
					watch.Stop();
					Logger.Error(e, I18n.PathFinder_FindPath_Error);
					NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, PathFinderResult.Error, 0, 0, 0, flags));
					return;
				}

				int totalWordCount = totalWordList.Count;

				// Limit the word list size
				int maxCount = CurrentConfig.MaxDisplayedWordCount;
				if (totalWordList.Count > maxCount)
					totalWordList = totalWordList.Take(maxCount).ToList();

				DisplayList = totalWordList;
				IList<PathObject> qualifiedWordList = CreateQualifiedWordList(totalWordList);

				// If there's no word found (or all words was filtered out)
				if (qualifiedWordList.Count == 0)
				{
					watch.Stop();
					Logger.Warn(I18n.PathFinder_FindPath_NotFound);
					NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, PathFinderResult.None, totalWordCount, 0, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
					return;
				}

				// Update final lists
				QualifiedList = qualifiedWordList;

				watch.Stop();
				Logger.Info(CultureInfo.CurrentCulture, I18n.PathFinder_FoundPath_Ready, DisplayList.Count, watch.ElapsedMilliseconds);
				NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, PathFinderResult.Normal, totalWordCount, QualifiedList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
			});
		}

		private static IList<PathObject> CreateQualifiedWordList(IList<PathObject> wordList)
		{
			var qualifiedList = new List<PathObject>();
			foreach (PathObject word in wordList)
			{
				try
				{
					PathListLock.EnterReadLock();
					if (InexistentPathList.Contains(word.Content))
						word.RemoveQueued = true;
					if (UnsupportedPathList.Contains(word.Content))
						word.Excluded = true;
					else if (CurrentConfig?.ReturnModeEnabled != true && PreviousPath.Contains(word.Content))
						word.AlreadyUsed = true;
					else
						qualifiedList.Add(word);
				}
				finally
				{
					PathListLock.ExitReadLock();
				}
			}
			return qualifiedList;
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
				Logger.Error(ex, I18n.PathFinder_Init_Error);
				DatabaseEvents.TriggerDatabaseError();
			}
		}

		public static void UpdateColorPreference(AutoKkutuColorPreference newColorPref) => CurrentColorPreference = newColorPref;

		public static void UpdateConfig(AutoKkutuConfiguration newConfig) => CurrentConfig = newConfig;

		public static void UpdateNodeLists(CommonDatabaseConnection connection)
		{
			AttackWordList = connection.GetNodeList(DatabaseConstants.AttackWordListTableName);
			EndWordList = connection.GetNodeList(DatabaseConstants.EndWordListTableName);
			ReverseAttackWordList = connection.GetNodeList(DatabaseConstants.ReverseAttackWordListTableName);
			ReverseEndWordList = connection.GetNodeList(DatabaseConstants.ReverseEndWordListTableName);
			KkutuAttackWordList = connection.GetNodeList(DatabaseConstants.KkutuAttackWordListTableName);
			KkutuEndWordList = connection.GetNodeList(DatabaseConstants.KkutuEndWordListTableName);
			KKTAttackWordList = connection.GetNodeList(DatabaseConstants.KKTAttackWordListTableName);
			KKTEndWordList = connection.GetNodeList(DatabaseConstants.KKTEndWordListTableName);
		}

		private static int AddNewPaths()
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
				WordDatabaseAttributes flags = DatabaseUtils.GetFlags(word);

				try
				{
					Logger.Debug(CultureInfo.CurrentCulture, I18n.PathFinder_AddPath, word, flags);
					if (Connection.RequireNotNull().AddWord(word, flags))
					{
						Logger.Info(CultureInfo.CurrentCulture, I18n.PathFinder_AddPath_Success, word);
						count++;
					}
				}
				catch (Exception ex)
				{
					Logger.Error(ex, CultureInfo.CurrentCulture, I18n.PathFinder_AddPath_Failed, word);
				}
			}

			return count;
		}

		private static void NotifyPathUpdate(UpdatedPathEventArgs eventArgs) => OnPathUpdated?.Invoke(null, eventArgs);

		private static void RandomPath(FindWordInfo info)
		{
			ResponsePresentedWord word = info.Word;
			string missionChar = info.MissionChar;
			string firstChar = (info.Mode == GameMode.LastAndFirstFree) ? word.Content : "";

			var watch = new Stopwatch();
			watch.Start();
			DisplayList = new List<PathObject>();
			if (!string.IsNullOrWhiteSpace(missionChar))
				DisplayList.Add(new PathObject(firstChar + new string(missionChar[0], 256), WordAttributes.None, 256));
			var random = new Random();
			for (int i = 0; i < 10; i++)
				DisplayList.Add(new PathObject(firstChar + RandomUtils.GenerateRandomString(256, false, random), WordAttributes.None, 256));
			watch.Stop();
			NotifyPathUpdate(new UpdatedPathEventArgs(word, missionChar, PathFinderResult.Normal, DisplayList.Count, DisplayList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), info.PathFinderFlags));
		}

		private static int RemoveInexistentPaths()
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
			{
				try
				{
					count += Connection.RequireNotNull().DeleteWord(word);
				}
				catch (Exception ex)
				{
					Logger.Error(ex, I18n.PathFinder_RemoveInexistent_Failed, word);
				}
			}

			return count;
		}

		public static void ResetPreviousPath() => PreviousPath = new List<string>();

		public static void ResetFinalList() => DisplayList = new List<PathObject>();
	}

	public class PathObject
	{
		public string Color
		{
			get;
		}

		public string Content
		{
			get;
		}

		public bool MakeAttackAvailable
		{
			get;
		}

		public bool MakeEndAvailable
		{
			get;
		}

		public bool MakeNormalAvailable
		{
			get;
		}

		public string PrimaryImage
		{
			get;
		}

		public string SecondaryImage
		{
			get;
		}

		public string Title
		{
			get;
		}

		public string ToolTip
		{
			get;
		}

		public string Decorations => AlreadyUsed || Excluded || RemoveQueued ? "Strikethrough" : "None";

		public string FontWeight => RemoveQueued ? "Bold" : "Normal";

		public string FontStyle => RemoveQueued ? "Italic" : "Normal";

		public bool AlreadyUsed
		{
			get; set;
		}

		public bool Excluded
		{
			get; set;
		}

		public bool RemoveQueued
		{
			get; set;
		}

		private static readonly Logger Logger = LogManager.GetLogger(nameof(PathFinder));

		public PathObject(string content, WordAttributes flags, int missionCharCount)
		{
			AutoKkutuColorPreference colorPref = PathFinder.CurrentColorPreference.RequireNotNull();

			Content = content;
			Title = content;

			MakeEndAvailable = !flags.HasFlag(WordAttributes.EndWord);
			MakeAttackAvailable = !flags.HasFlag(WordAttributes.AttackWord);
			MakeNormalAvailable = !MakeEndAvailable || !MakeAttackAvailable;

			bool isMissionWord = flags.HasFlag(WordAttributes.MissionWord);
			string tooltipPrefix;
			if (flags.HasFlag(WordAttributes.EndWord))
			{
				tooltipPrefix = isMissionWord ? I18n.PathTooltip_EndMission : I18n.PathTooltip_End;
				Color = (isMissionWord ? colorPref.EndMissionWordColor : colorPref.EndWordColor).ToString(CultureInfo.InvariantCulture);
				PrimaryImage = @"images\skull.png";
			}
			else if (flags.HasFlag(WordAttributes.AttackWord))
			{
				tooltipPrefix = isMissionWord ? I18n.PathTooltip_AttackMission : I18n.PathTooltip_Attack;
				Color = (isMissionWord ? colorPref.AttackMissionWordColor : colorPref.AttackWordColor).ToString(CultureInfo.InvariantCulture);
				PrimaryImage = @"images\attack.png";
			}
			else
			{
				Color = isMissionWord ? colorPref.MissionWordColor.ToString(CultureInfo.InvariantCulture) : "#FFFFFFFF";
				tooltipPrefix = isMissionWord ? I18n.PathTooltip_Mission : I18n.PathTooltip_Normal;
				PrimaryImage = string.Empty;
			}
			SecondaryImage = isMissionWord ? @"images\mission.png" : string.Empty;

			ToolTip = string.Format(CultureInfo.CurrentCulture, tooltipPrefix, isMissionWord ? new object[2] { content, missionCharCount } : new object[1] { content });
		}

		public void MakeAttack(GameMode mode, CommonDatabaseConnection connection)
		{
			string node = ToNode(mode);
			connection.DeleteNode(node, GetEndWordListTableName(mode));
			if (connection.AddNode(node, GetAttackWordListTableName(mode)))
				Logger.Info(CultureInfo.CurrentCulture, I18n.PathMark_Success, node, I18n.PathMark_Attack, mode);
			else
				Logger.Warn(CultureInfo.CurrentCulture, I18n.PathMark_AlreadyDone, node, I18n.PathMark_Attack, mode);
		}

		public void MakeEnd(GameMode mode, CommonDatabaseConnection connection)
		{
			string node = ToNode(mode);
			connection.DeleteNode(node, GetAttackWordListTableName(mode));
			if (connection.AddNode(node, GetEndWordListTableName(mode)))
				Logger.Info(CultureInfo.CurrentCulture, I18n.PathMark_Success, node, I18n.PathMark_End, mode);
			else
				Logger.Warn(CultureInfo.CurrentCulture, I18n.PathMark_AlreadyDone, node, I18n.PathMark_End, mode);
		}

		public void MakeNormal(GameMode mode, CommonDatabaseConnection connection)
		{
			string node = ToNode(mode);
			bool endWord = connection.DeleteNode(node, GetEndWordListTableName(mode)) > 0;
			bool attackWord = connection.DeleteNode(node, GetAttackWordListTableName(mode)) > 0;
			if (endWord || attackWord)
				Logger.Info(CultureInfo.CurrentCulture, I18n.PathMark_Success, node, I18n.PathMark_Normal, mode);
			else
				Logger.Warn(CultureInfo.CurrentCulture, I18n.PathMark_AlreadyDone, node, I18n.PathMark_Normal, mode);
		}

		private static string GetAttackWordListTableName(GameMode mode) => mode switch
		{
			GameMode.FirstAndLast => DatabaseConstants.ReverseAttackWordListTableName,
			GameMode.Kkutu => DatabaseConstants.KkutuAttackWordListTableName,
			_ => DatabaseConstants.AttackWordListTableName,
		};

		private static string GetEndWordListTableName(GameMode mode) => mode switch
		{
			GameMode.FirstAndLast => DatabaseConstants.ReverseEndWordListTableName,
			GameMode.Kkutu => DatabaseConstants.KkutuEndWordListTableName,
			_ => DatabaseConstants.EndWordListTableName,
		};

		private string ToNode(GameMode mode)
		{
			switch (mode)
			{
				case GameMode.FirstAndLast:
					return Content.GetFaLTailNode();

				case GameMode.MiddleAndFirst:
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
			get;
		}

		public PathFinderOptions Flags
		{
			get;
		}

		public string MissionChar
		{
			get;
		}

		public PathFinderResult Result
		{
			get;
		}

		public int Time
		{
			get;
		}

		public int TotalWordCount
		{
			get;
		}

		public ResponsePresentedWord Word
		{
			get;
		}

		public UpdatedPathEventArgs(ResponsePresentedWord word, string missionChar, PathFinderResult arg, int totalWordCount = 0, int calcWordCount = 0, int time = 0, PathFinderOptions flags = PathFinderOptions.None)
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
	public enum PathFinderOptions
	{
		None = 0,
		UseEndWord = 1 << 0,
		UseAttackWord = 1 << 1,
		AutoFixed = 1 << 2,
		ManualSearch = 1 << 3
	}

	public enum PathFinderResult
	{
		Normal,
		None,
		Error
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

		public PathFinderOptions PathFinderFlags
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

		public override bool Equals(object? obj) => obj is FindWordInfo other && Equals(other);

		public bool Equals(FindWordInfo other) =>
				MissionChar.Equals(other.MissionChar, StringComparison.OrdinalIgnoreCase)
				&& Mode == other.Mode
				&& PathFinderFlags == other.PathFinderFlags
				&& Word == other.Word
				&& WordPreference == other.WordPreference;

		public override int GetHashCode() => HashCode.Combine(MissionChar, Mode, PathFinderFlags, Word, WordPreference);
	}
}
