﻿using AutoKkutu.Constants;
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

		public static ConcurrentBag<string> InexistentPathList { get; private set; } = new ConcurrentBag<string>();

		public static ConcurrentBag<string> NewPathList { get; private set; } = new ConcurrentBag<string>();

		public static ICollection<string> PreviousPath { get; private set; } = new List<string>();

		public static ICollection<string> UnsupportedPathList { get; } = new List<string>();

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
				UnsupportedPathList.Add(word);
				if (isNonexistent)
					InexistentPathList.Add(word);
			}
		}

		public static string? AutoDBUpdate()
		{
			if (!CurrentConfig.RequireNotNull().AutoDBUpdateEnabled)
				return null;

			Logger.Debug("Automatically update the DB based on last game.");
			if (NewPathList.Count + UnsupportedPathList.Count == 0)
			{
				Logger.Warn("No such element in autoupdate list.");
			}
			else
			{
				int NewPathCount = NewPathList.Count;
				Logger.Debug(CultureInfo.CurrentCulture, "Get {0} elements from NewPathList.", NewPathCount);
				int AddedPathCount = AddNewPaths();

				int InexistentPathCount = InexistentPathList.Count;
				Logger.Info(CultureInfo.CurrentCulture, "Get {0} elements from WrongPathList.", InexistentPathCount);

				int RemovedPathCount = RemoveInexistentPaths();
				string result = $"{AddedPathCount} of {NewPathCount} added, {RemovedPathCount} of {InexistentPathCount} removed";

				Logger.Info($"Automatic DB Update complete ({result})");
				return result;
			}

			return null;
		}

		public static bool CheckNodePresence(string? nodeType, string item, ICollection<string>? nodeList, WordDatabaseAttributes theFlag, ref WordDatabaseAttributes flags, bool add = false)
		{
			if (add && string.IsNullOrEmpty(nodeType) || string.IsNullOrWhiteSpace(item) || nodeList == null)
				return false;

			bool exists = nodeList.Contains(item);
			if (exists)
			{
				flags |= theFlag;
			}
			else if (add && flags.HasFlag(theFlag))
			{
				nodeList.Add(item);
				Logger.Info($"Added new {nodeType} node '{item}");
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

				case GameMode.MiddleAddFirst:
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
				Logger.Info(CultureInfo.CurrentCulture, "Finding path for {word} and {substituation}.", wordCondition.Content, wordCondition.Substitution);
			else
				Logger.Info(CultureInfo.CurrentCulture, "Finding path for {word}.", wordCondition.Content);

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
					Logger.Info(CultureInfo.CurrentCulture, "Found {0} words. (AttackWord: {1}, EndWord: {2})", totalWordList.Count, flags.HasFlag(PathFinderOptions.UseAttackWord), flags.HasFlag(PathFinderOptions.UseEndWord));
				}
				catch (Exception e)
				{
					watch.Stop();
					Logger.Error(e, "Failed to Find Path");
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
					Logger.Warn("Can't find any path.");
					NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, PathFinderResult.None, totalWordCount, 0, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
					return;
				}

				// Update final lists
				QualifiedList = qualifiedWordList;

				watch.Stop();
				Logger.Info(CultureInfo.CurrentCulture, "Total {0} words are ready. (Took {1}ms)", DisplayList.Count, watch.ElapsedMilliseconds);
				NotifyPathUpdate(new UpdatedPathEventArgs(wordCondition, missionChar, PathFinderResult.Normal, totalWordCount, QualifiedList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
			});
		}

		private static IList<PathObject> CreateQualifiedWordList(IList<PathObject> wordList)
		{
			var qualifiedList = new List<PathObject>();
			foreach (PathObject word in wordList)
			{
				if (UnsupportedPathList.Contains(word.Content))
					word.Unsupported = true;
				else if (CurrentConfig?.ReturnModeEnabled != true && PreviousPath.Contains(word.Content))
					word.AlreadyUsed = true;
				else
					qualifiedList.Add(word);
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
				Logger.Error(ex, "Failed to update node lists");
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
			foreach (string word in NewPathList)
			{
				WordDatabaseAttributes flags = DatabaseUtils.GetFlags(word);

				try
				{
					Logger.Debug(CultureInfo.CurrentCulture, "Check and add {word} into database. (flags: {flags})", word, flags);
					if (Connection.RequireNotNull().AddWord(word, flags))
					{
						Logger.Info(CultureInfo.CurrentCulture, "Added {word} into database.", word);
						count++;
					}
				}
				catch (Exception ex)
				{
					Logger.Error(ex, CultureInfo.CurrentCulture, "Can't add {word} to database", word);
				}
			}

			NewPathList = new ConcurrentBag<string>();

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
			foreach (string word in InexistentPathList)
			{
				try
				{
					count += Connection.RequireNotNull().DeleteWord(word);
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Can't delete {word} from database", word);
				}
			}

			InexistentPathList = new ConcurrentBag<string>();

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

		public string Decorations => Unsupported ? "Underline,Strikethrough,Overline" : AlreadyUsed ? "Strikethrough" : "None";

		public bool AlreadyUsed
		{
			get; set;
		}

		public bool Unsupported
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
			string mission = isMissionWord ? $"미션({missionCharCount}) " : string.Empty;
			string tooltipPrefix;
			if (flags.HasFlag(WordAttributes.EndWord))
			{
				tooltipPrefix = $"한방 {mission}단어: ";
				Color = (isMissionWord ? colorPref.EndMissionWordColor : colorPref.EndWordColor).ToString(CultureInfo.InvariantCulture);
				PrimaryImage = @"images\skull.png";
			}
			else if (flags.HasFlag(WordAttributes.AttackWord))
			{
				tooltipPrefix = $"공격 {mission}단어: ";
				Color = (isMissionWord ? colorPref.AttackMissionWordColor : colorPref.AttackWordColor).ToString(CultureInfo.InvariantCulture);
				PrimaryImage = @"images\attack.png";
			}
			else
			{
				Color = isMissionWord ? colorPref.MissionWordColor.ToString(CultureInfo.InvariantCulture) : "#FFFFFFFF";
				tooltipPrefix = isMissionWord ? $"미션({missionCharCount}) 단어: " : string.Empty;
				PrimaryImage = string.Empty;
			}
			SecondaryImage = isMissionWord ? @"images\mission.png" : string.Empty;

			ToolTip = tooltipPrefix + content;
		}

		public void MakeAttack(GameMode mode, CommonDatabaseConnection connection)
		{
			string node = ToNode(mode);
			connection.DeleteNode(node, GetEndWordListTableName(mode));
			if (connection.AddNode(node, GetAttackWordListTableName(mode)))
				Logger.Info(CultureInfo.CurrentCulture, "Successfully marked node {node} as attack word. [Gamemode: {gameMode}]", node, mode);
			else
				Logger.Warn(CultureInfo.CurrentCulture, "Node {node} is already marked as attack word. [Gamemode: {gameMode}]", node, mode);
		}

		public void MakeEnd(GameMode mode, CommonDatabaseConnection connection)
		{
			string node = ToNode(mode);
			connection.DeleteNode(node, GetAttackWordListTableName(mode));
			if (connection.AddNode(node, GetEndWordListTableName(mode)))
				Logger.Info(CultureInfo.CurrentCulture, "Successfully marked node {node} as end word. [Gamemode: {gameMode}]", node, mode);
			else
				Logger.Warn(CultureInfo.CurrentCulture, "Node {node} is already marked as end word. [Gamemode: {gameMode}]", node, mode);
		}

		public void MakeNormal(GameMode mode, CommonDatabaseConnection connection)
		{
			string node = ToNode(mode);
			bool a = connection.DeleteNode(node, GetEndWordListTableName(mode)) > 0;
			bool b = connection.DeleteNode(node, GetAttackWordListTableName(mode)) > 0;
			if (a || b)
				Logger.Info(CultureInfo.CurrentCulture, "Successfully marked node {node} as normal word. [Gamemode: {gameMode}]", node, mode);
			else
				Logger.Warn(CultureInfo.CurrentCulture, "Node {node} is already marked as normal word. [Gamemode: {gameMode}]", node, mode);
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

				case GameMode.MiddleAddFirst:
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
