using AutoKkutu.Constants;
using AutoKkutu.Database.Extension;
using AutoKkutu.Modules.PathManager;
using AutoKkutu.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace AutoKkutu.Modules.PathFinder
{
	[ModuleDependency(typeof(IPathManager))]
	public class PathFinderCore
	{
		private readonly IPathManager PathManager;

		public IList<PathObject> DisplayList
		{
			get; private set;
		} = new List<PathObject>();

		public IList<PathObject> QualifiedList
		{
			get; private set;
		} = new List<PathObject>();

		public event EventHandler<PathUpdateEventArgs>? OnPathUpdated;

		public PathFinderCore(IPathManager pathManager)
		{
			PathManager = pathManager;
		}

		// TODO: This method should located in caller site, not in this module
		private void ApplyWordFilterFlags(ref PathFinderOptions flags)
		{
			// Setup flag
			if (GetConfig().EndWordEnabled && (flags.HasFlag(PathFinderOptions.ManualSearch) || PathManager.PreviousPath.Count > 0))  // 첫 턴 한방 방지
				flags |= PathFinderOptions.UseEndWord;
			else
				flags &= ~PathFinderOptions.UseEndWord;
			if (GetConfig().AttackWordAllowed)
				flags |= PathFinderOptions.UseAttackWord;
			else
				flags &= ~PathFinderOptions.UseAttackWord;
		}

		public void Find(GameMode mode, PathFinderParameters param, WordPreference pref)
		{
			if (mode == GameMode.TypingBattle && !param.Options.HasFlag(PathFinderOptions.ManualSearch))
				return;

			try
			{
				PresentedWord word = param.Word;
				if (!ConfigEnums.IsFreeMode(mode) && PathManager.GetEndNodeForMode(mode).Contains(word.Content) && (!word.CanSubstitution || PathManager.GetEndNodeForMode(mode).Contains(word.Substitution!)))
				{
					// 진퇴양난
					Log.Warning(I18n.PathFinderFailed_Endword);
					// AutoKkutuMain.ResetPathList();
					AutoKkutuMain.UpdateSearchState(null, true);
					AutoKkutuMain.UpdateStatusMessage(StatusMessage.EndWord);
				}
				else
				{
					AutoKkutuMain.UpdateStatusMessage(StatusMessage.Searching);

					// These two codes could be handled by CALLER, not PathFinder
					ApplyWordFilterFlags(ref flags);
					string missionFixChar = GetConfig().MissionAutoDetectionEnabled && missionChar != null ? missionChar : "";

					// Enqueue search
					FindInternal(word, mode, missionFixChar, pref, flags);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, I18n.PathFinderFailed_Exception);
			}
		}

		public void FindInternal(GameMode mode, PathFinderParameters param, WordPreference preference)
		{
			if (ConfigEnums.IsFreeMode(mode))
			{
				GenerateRandomPath(mode, param);
				return;
			}

			PathFinderOptions flags = param.Options;
			PresentedWord word = param.Word;
			if (word.CanSubstitution)
				Log.Information(I18n.PathFinder_FindPath_Substituation, word.Content, word.Substitution);
			else
				Log.Information(I18n.PathFinder_FindPath, word.Content);

			// Prevent watchdog thread from being blocked
			Task.Run(() =>
			{
				var stopwatch = new Stopwatch();
				stopwatch.Start();

				// Flush previous search result
				DisplayList = new List<PathObject>();
				QualifiedList = new List<PathObject>();

				// Search words from database
				IList<PathObject>? totalWordList = null;
				try
				{
					totalWordList = DbConnection.FindWord(mode, param, preference);
					Log.Information(I18n.PathFinder_FoundPath, totalWordList.Count, flags.HasFlag(PathFinderOptions.UseAttackWord), flags.HasFlag(PathFinderOptions.UseEndWord));
				}
				catch (Exception e)
				{
					stopwatch.Stop();
					Log.Error(e, I18n.PathFinder_FindPath_Error);
					NotifyPathUpdate(new PathUpdateEventArgs(param, PathFindResult.Error, 0, 0, 0));
					return;
				}

				int totalWordCount = totalWordList.Count;

				// Limit the word list size
				int maxCount = AutoKkutuMain.Configuration.MaxDisplayedWordCount;
				if (totalWordList.Count > maxCount)
					totalWordList = totalWordList.Take(maxCount).ToList();
				stopwatch.Stop();

				DisplayList = totalWordList;
				// TODO: Detach with 'Mediator' pattern
				IList<PathObject> qualifiedWordList = PathManager.CreateQualifiedWordList(totalWordList);

				// If there's no word found (or all words was filtered out)
				if (qualifiedWordList.Count == 0)
				{
					Log.Warning(I18n.PathFinder_FindPath_NotFound);
					NotifyPathUpdate(new PathUpdateEventArgs(param, PathFindResult.NotFound, totalWordCount, 0, Convert.ToInt32(stopwatch.ElapsedMilliseconds)));
					return;
				}

				// Update final lists
				QualifiedList = qualifiedWordList;

				Log.Information(I18n.PathFinder_FoundPath_Ready, DisplayList.Count, stopwatch.ElapsedMilliseconds);
				NotifyPathUpdate(new PathUpdateEventArgs(param, PathFindResult.Found, totalWordCount, QualifiedList.Count, Convert.ToInt32(stopwatch.ElapsedMilliseconds)));
			});
		}

		public void GenerateRandomPath(
			GameMode mode,
			PathFinderParameters param)
		{
			string firstChar = mode == GameMode.LastAndFirstFree ? param.Word.Content : "";

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			DisplayList = new List<PathObject>();
			var random = new Random();
			int len = random.Next(64, 256);
			if (!string.IsNullOrWhiteSpace(param.MissionChar))
				DisplayList.Add(new PathObject(firstChar + RandomUtils.GenerateRandomString(random.Next(16, 64), false, random) + new string(param.MissionChar[0], len) + RandomUtils.GenerateRandomString(random.Next(16, 64), false, random), WordCategories.None, len));
			for (int i = 0; i < 10; i++)
				DisplayList.Add(new PathObject(firstChar + RandomUtils.GenerateRandomString(len, false, random), WordCategories.None, 0));
			stopwatch.Stop();
			QualifiedList = new List<PathObject>(DisplayList);
			NotifyPathUpdate(new PathUpdateEventArgs(param, PathFindResult.Found, DisplayList.Count, DisplayList.Count, Convert.ToInt32(stopwatch.ElapsedMilliseconds)));
		}

		private void NotifyPathUpdate(PathUpdateEventArgs eventArgs) => OnPathUpdated?.Invoke(null, eventArgs);

		public void ResetFinalList() => DisplayList = new List<PathObject>();
	}
}
