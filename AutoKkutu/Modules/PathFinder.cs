﻿using AutoKkutu.Constants;
using AutoKkutu.Database.Extension;
using AutoKkutu.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AutoKkutu.Modules
{
	public static class PathFinder
	{
		public static IList<PathObject> DisplayList
		{
			get; private set;
		} = new List<PathObject>();

		public static IList<PathObject> QualifiedList
		{
			get; private set;
		} = new List<PathObject>();

		public static event EventHandler<PathUpdatedEventArgs>? OnPathUpdated;

		public static void FindPath(GameMode mode, ResponsePresentedWord word, string missionChar, WordPreference preference, PathFinderOptions options)
		{
			if (word is null)
				throw new ArgumentNullException(nameof(word));

			if (ConfigEnums.IsFreeMode(mode))
			{
				RandomPath(mode, word, missionChar, options);
				return;
			}

			PathFinderOptions flags = options;
			if (word.CanSubstitution)
				Log.Information(I18n.PathFinder_FindPath_Substituation, word.Content, word.Substitution);
			else
				Log.Information(I18n.PathFinder_FindPath, word.Content);

			// Prevent watchdog thread from being blocked
			Task.Run(() =>
			{
				var stopWatch = new Stopwatch();
				stopWatch.Start();

				// Flush previous search result
				DisplayList = new List<PathObject>();
				QualifiedList = new List<PathObject>();

				// Search words from database
				IList<PathObject>? totalWordList = null;
				try
				{
					totalWordList = AutoKkutuMain.Database.Connection.FindWord(mode, word, missionChar, preference, options);
					Log.Information(I18n.PathFinder_FoundPath, totalWordList.Count, flags.HasFlag(PathFinderOptions.UseAttackWord), flags.HasFlag(PathFinderOptions.UseEndWord));
				}
				catch (Exception e)
				{
					stopWatch.Stop();
					Log.Error(e, I18n.PathFinder_FindPath_Error);
					NotifyPathUpdate(new PathUpdatedEventArgs(word, missionChar, PathFinderResult.Error, 0, 0, 0, flags));
					return;
				}

				int totalWordCount = totalWordList.Count;

				// Limit the word list size
				int maxCount = AutoKkutuMain.Configuration.MaxDisplayedWordCount;
				if (totalWordList.Count > maxCount)
					totalWordList = totalWordList.Take(maxCount).ToList();

				DisplayList = totalWordList;
				IList<PathObject> qualifiedWordList = PathManager.CreateQualifiedWordList(totalWordList);

				// If there's no word found (or all words was filtered out)
				if (qualifiedWordList.Count == 0)
				{
					stopWatch.Stop();
					Log.Warning(I18n.PathFinder_FindPath_NotFound);
					NotifyPathUpdate(new PathUpdatedEventArgs(word, missionChar, PathFinderResult.None, totalWordCount, 0, Convert.ToInt32(stopWatch.ElapsedMilliseconds), flags));
					return;
				}

				// Update final lists
				QualifiedList = qualifiedWordList;

				stopWatch.Stop();
				Log.Information(I18n.PathFinder_FoundPath_Ready, DisplayList.Count, stopWatch.ElapsedMilliseconds);
				NotifyPathUpdate(new PathUpdatedEventArgs(word, missionChar, PathFinderResult.Normal, totalWordCount, QualifiedList.Count, Convert.ToInt32(stopWatch.ElapsedMilliseconds), flags));
			});
		}

		private static void RandomPath(
			GameMode mode,
			ResponsePresentedWord word,
			string missionChar,
			PathFinderOptions options)
		{
			string firstChar = mode == GameMode.LastAndFirstFree ? word.Content : "";

			var watch = new Stopwatch();
			watch.Start();
			DisplayList = new List<PathObject>();
			var random = new Random();
			int len = random.Next(64, 256);
			if (!string.IsNullOrWhiteSpace(missionChar))
				DisplayList.Add(new PathObject(firstChar + RandomUtils.GenerateRandomString(random.Next(16, 64), false, random) + new string(missionChar[0], len) + RandomUtils.GenerateRandomString(random.Next(16, 64), false, random), WordCategories.None, len));
			for (int i = 0; i < 10; i++)
				DisplayList.Add(new PathObject(firstChar + RandomUtils.GenerateRandomString(len, false, random), WordCategories.None, 0));
			watch.Stop();
			QualifiedList = new List<PathObject>(DisplayList);
			NotifyPathUpdate(new PathUpdatedEventArgs(word, missionChar, PathFinderResult.Normal, DisplayList.Count, DisplayList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), options));
		}

		private static void NotifyPathUpdate(PathUpdatedEventArgs eventArgs) => OnPathUpdated?.Invoke(null, eventArgs);

		public static void ResetFinalList() => DisplayList = new List<PathObject>();
	}

	public class PathUpdatedEventArgs : EventArgs
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

		public PathUpdatedEventArgs(ResponsePresentedWord word, string missionChar, PathFinderResult arg, int totalWordCount = 0, int calcWordCount = 0, int time = 0, PathFinderOptions flags = PathFinderOptions.None)
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
}
