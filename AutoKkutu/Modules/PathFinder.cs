using AutoKkutu.Constants;
using AutoKkutu.Databases.Extension;
using AutoKkutu.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		public static void FindPath(FindWordInfo info)
		{
			if (ConfigEnums.IsFreeMode(info.Mode))
			{
				RandomPath(info);
				return;
			}

			ResponsePresentedWord wordCondition = info.Word;
			string missionChar = info.MissionChar;
			PathFinderOptions flags = info.PathFinderFlags;
			if (wordCondition.CanSubstitution)
				Log.Information(I18n.PathFinder_FindPath_Substituation, wordCondition.Content, wordCondition.Substitution);
			else
				Log.Information(I18n.PathFinder_FindPath, wordCondition.Content);

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
					totalWordList = AutoKkutuMain.Database.DefaultConnection.FindWord(info);
					Log.Information(I18n.PathFinder_FoundPath, totalWordList.Count, flags.HasFlag(PathFinderOptions.UseAttackWord), flags.HasFlag(PathFinderOptions.UseEndWord));
				}
				catch (Exception e)
				{
					watch.Stop();
					Log.Error(e, I18n.PathFinder_FindPath_Error);
					NotifyPathUpdate(new PathUpdatedEventArgs(wordCondition, missionChar, PathFinderResult.Error, 0, 0, 0, flags));
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
					watch.Stop();
					Log.Warning(I18n.PathFinder_FindPath_NotFound);
					NotifyPathUpdate(new PathUpdatedEventArgs(wordCondition, missionChar, PathFinderResult.None, totalWordCount, 0, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
					return;
				}

				// Update final lists
				QualifiedList = qualifiedWordList;

				watch.Stop();
				Log.Information(I18n.PathFinder_FoundPath_Ready, DisplayList.Count, watch.ElapsedMilliseconds);
				NotifyPathUpdate(new PathUpdatedEventArgs(wordCondition, missionChar, PathFinderResult.Normal, totalWordCount, QualifiedList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), flags));
			});
		}

		private static void RandomPath(FindWordInfo info)
		{
			ResponsePresentedWord word = info.Word;
			string missionChar = info.MissionChar;
			string firstChar = info.Mode == GameMode.LastAndFirstFree ? word.Content : "";

			var watch = new Stopwatch();
			watch.Start();
			DisplayList = new List<PathObject>();
			if (!string.IsNullOrWhiteSpace(missionChar))
				DisplayList.Add(new PathObject(firstChar + new string(missionChar[0], 256), WordAttributes.None, 256));
			var random = new Random();
			for (int i = 0; i < 10; i++)
				DisplayList.Add(new PathObject(firstChar + RandomUtils.GenerateRandomString(256, false, random), WordAttributes.None, 256));
			watch.Stop();
			NotifyPathUpdate(new PathUpdatedEventArgs(word, missionChar, PathFinderResult.Normal, DisplayList.Count, DisplayList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), info.PathFinderFlags));
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
