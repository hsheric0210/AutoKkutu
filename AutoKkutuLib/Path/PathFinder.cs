using AutoKkutuLib.Extension;
using AutoKkutuLib.Node;
using Serilog;
using System.Diagnostics;

namespace AutoKkutuLib.Path;

public class PathFinder
{
	private readonly NodeManager nodeManager;
	private readonly PathFilter specialPathList;

	public IList<PathObject> TotalWordList
	{
		get; private set;
	} = new List<PathObject>();

	public IList<PathObject> AvailableWordList
	{
		get; private set;
	} = new List<PathObject>();

	public event EventHandler<PathFinderStateEventArgs>? OnStateChanged;
	public event EventHandler<PathUpdateEventArgs>? OnPathUpdated;

	public PathFinder(NodeManager nodeManager, PathFilter specialPathList)
	{
		this.nodeManager = nodeManager;
		this.specialPathList = specialPathList;
	}

	public void FindPath(GameMode gameMode, PathFinderParameter parameter, WordPreference preference)
	{
		// TODO: This check could be moved to caller site
		if (gameMode == GameMode.TypingBattle && !parameter.Options.HasFlag(PathFinderFlags.ManualSearch))
			return;

		// TODO: This implementation could be moved to caller site
		if (gameMode.IsFreeMode())
		{
			Task.Run(() => GenerateRandomPath(gameMode, parameter));
			return;
		}

		try
		{
			PresentedWord word = parameter.Word;
			if (nodeManager.GetEndNodeForMode(gameMode).Contains(word.Content) && (!word.CanSubstitution || nodeManager.GetEndNodeForMode(gameMode).Contains(word.Substitution!)))
			{
				Log.Warning("End node: {node1}, {node2}", word.Content, word.Substitution);
				// 진퇴양난
				Log.Warning(I18n.PathFinderFailed_Endword);
				// AutoKkutuMain.ResetPathList();
				//AutoKkutuMain.UpdateSearchState(null, true);
				//AutoKkutuMain.UpdateStatusMessage(StatusMessage.EndWord);
				OnStateChanged?.Invoke(this, new PathFinderStateEventArgs(PathFinderState.EndWord));
			}
			else
			{
				//AutoKkutuMain.UpdateStatusMessage(StatusMessage.Searching);
				OnStateChanged?.Invoke(this, new PathFinderStateEventArgs(PathFinderState.Finding));

				if (word.CanSubstitution)
					Log.Information(I18n.PathFinder_FindPath_Substituation, word.Content, word.Substitution);
				else
					Log.Information(I18n.PathFinder_FindPath, word.Content);

				// Enqueue search
				Task.Run(() => FindPathInternal(gameMode, parameter, preference));
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, I18n.PathFinderFailed_Exception);
		}
	}

	private void FindPathInternal(GameMode mode, PathFinderParameter parameter, WordPreference preference)
	{
		var stopWatch = new Stopwatch();
		stopWatch.Start();

		// Flush previous search result
		Reset();

		// Search words from database
		IList<PathObject>? totalWordList = null;
		try
		{
			totalWordList = nodeManager.DbConnection.Query.FindWord(mode, preference, parameter.MaxDisplayed).Execute(parameter);
			Log.Information(I18n.PathFinder_FoundPath, totalWordList.Count, parameter.Options.HasFlag(PathFinderFlags.UseAttackWord), parameter.Options.HasFlag(PathFinderFlags.UseEndWord));
		}
		catch (Exception e)
		{
			stopWatch.Stop();
			Log.Error(e, I18n.PathFinder_FindPath_Error);
			PathUpdated(new PathUpdateEventArgs(parameter, PathFindResult.Error, 0, 0, 0));
			return;
		}

		var totalWordCount = totalWordList.Count;

		// Limit the word list size
		stopWatch.Stop();

		TotalWordList = totalWordList;

		IList<PathObject> availableWordList = specialPathList.FilterPathList(totalWordList, parameter.ReuseAlreadyUsed);
		// If there's no word found (or all words was filtered out)
		if (availableWordList.Count == 0)
		{
			Log.Warning(I18n.PathFinder_FindPath_NotFound);
			PathUpdated(new PathUpdateEventArgs(parameter, PathFindResult.NotFound, totalWordCount, 0, Convert.ToInt32(stopWatch.ElapsedMilliseconds)));
			return;
		}

		// Update final lists
		AvailableWordList = availableWordList;

		Log.Information(I18n.PathFinder_FoundPath_Ready, TotalWordList.Count, stopWatch.ElapsedMilliseconds);
		PathUpdated(new PathUpdateEventArgs(parameter, PathFindResult.Found, totalWordCount, AvailableWordList.Count, Convert.ToInt32(stopWatch.ElapsedMilliseconds)));
	}

	public void GenerateRandomPath(
		GameMode mode,
		PathFinderParameter param)
	{
		var firstChar = mode == GameMode.LastAndFirstFree ? param.Word.Content : "";

		var stopwatch = new Stopwatch();
		stopwatch.Start();
		TotalWordList = new List<PathObject>();
		var random = new Random();
		var len = random.Next(64, 256);
		if (!string.IsNullOrWhiteSpace(param.MissionChar))
			TotalWordList.Add(new PathObject(firstChar + random.GenerateRandomString(random.Next(16, 64), false) + new string(param.MissionChar[0], len) + random.GenerateRandomString(random.Next(16, 64), false), WordCategories.None, len));
		for (var i = 0; i < 10; i++)
			TotalWordList.Add(new PathObject(firstChar + random.GenerateRandomString(len, false), WordCategories.None, 0));
		stopwatch.Stop();
		AvailableWordList = new List<PathObject>(TotalWordList);
		PathUpdated(new PathUpdateEventArgs(param, PathFindResult.Found, TotalWordList.Count, TotalWordList.Count, Convert.ToInt32(stopwatch.ElapsedMilliseconds)));
	}

	private void PathUpdated(PathUpdateEventArgs eventArgs) => OnPathUpdated?.Invoke(this, eventArgs);

	public void Reset()
	{
		TotalWordList.Clear();
		AvailableWordList.Clear();
	}
}
