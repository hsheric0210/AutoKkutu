using AutoKkutuLib.Extension;
using AutoKkutuLib.Node;
using Serilog;
using System.Collections.Immutable;
using System.Diagnostics;

namespace AutoKkutuLib.Path;

public class PathFinder
{
	private readonly NodeManager nodeManager;
	private readonly PathFilter specialPathList;

	public event EventHandler<PathFinderStateEventArgs>? OnStateChanged;
	public event EventHandler<PathUpdateEventArgs>? OnPathUpdated;

	public PathFinder(NodeManager nodeManager, PathFilter specialPathList)
	{
		this.nodeManager = nodeManager;
		this.specialPathList = specialPathList;
	}

	/// <summary>
	/// 단어 검색을 요청합니다
	/// </summary>
	/// <param name="gameMode">단어를 검색할 게임 모드</param>
	/// <param name="parameter">단어 검색 옵션</param>
	/// <param name="preference">단어 검색 우선 순위</param>
	public void FindPath(GameMode gameMode, PathFinderParameter parameter, WordPreference preference)
	{
		// TODO: This check could be moved to caller site
		if (gameMode == GameMode.TypingBattle && !parameter.HasFlag(PathFinderFlags.ManualSearch))
			return;

		// TODO: This implementation could be moved to caller site
		if (gameMode.IsFreeMode())
		{
			Task.Run(() => GenerateRandomPath(gameMode, parameter));
			return;
		}

		try
		{
			WordCondition word = parameter;
			if (nodeManager.GetEndNodeForMode(gameMode).Contains(word.Char) && (!word.SubAvailable || nodeManager.GetEndNodeForMode(gameMode).Contains(word.SubChar!)))
			{
				Log.Warning("End node: {node1}, {node2}", word.Char, word.SubChar);
				Log.Warning(I18n.PathFinderFailed_Endword);
				// AutoKkutuMain.ResetPathList();
				//AutoKkutuMain.UpdateSearchState(null, true);
				//AutoKkutuMain.UpdateStatusMessage(StatusMessage.EndWord);
				OnStateChanged?.Invoke(this, new PathFinderStateEventArgs(PathFinderState.EndWord));
			}
			else
			{
				OnStateChanged?.Invoke(this, new PathFinderStateEventArgs(PathFinderState.Finding));

				if (word.SubAvailable)
					Log.Information(I18n.PathFinder_FindPath_Substituation, word.Char, word.SubChar);
				else
					Log.Information(I18n.PathFinder_FindPath, word.Char);

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

		// Search words from database
		IImmutableList<PathObject> totalWordList;
		try
		{
			totalWordList = nodeManager.DbConnection.Query.FindWord(mode, preference).Execute(parameter);
			Log.Information(I18n.PathFinder_FoundPath, totalWordList.Count, parameter.HasFlag(PathFinderFlags.UseAttackWord), parameter.HasFlag(PathFinderFlags.UseEndWord));
		}
		catch (Exception e)
		{
			stopWatch.Stop();
			Log.Error(e, I18n.PathFinder_FindPath_Error);
			PathUpdated(new PathUpdateEventArgs(parameter, PathFindResultType.Error, ImmutableList<PathObject>.Empty, ImmutableList<PathObject>.Empty, 0));
			return;
		}

		stopWatch.Stop();

		IImmutableList<PathObject> availableWordList = specialPathList.FilterPathList(totalWordList, parameter.ReuseAlreadyUsed);
		// If there's no word found (or all words was filtered out)
		if (availableWordList.Count == 0)
		{
			Log.Warning(I18n.PathFinder_FindPath_NotFound);
			PathUpdated(new PathUpdateEventArgs(parameter, PathFindResultType.NotFound, totalWordList, ImmutableList<PathObject>.Empty, Convert.ToInt32(stopWatch.ElapsedMilliseconds)));
			return;
		}

		// Update final lists
		Log.Information(I18n.PathFinder_FoundPath_Ready, totalWordList.Count, stopWatch.ElapsedMilliseconds);
		PathUpdated(new PathUpdateEventArgs(parameter, PathFindResultType.Found, totalWordList, availableWordList, Convert.ToInt32(stopWatch.ElapsedMilliseconds)));
	}

	public void GenerateRandomPath(
		GameMode mode,
		PathFinderParameter param)
	{
		var firstChar = mode == GameMode.LastAndFirstFree ? param.Condition.Char : "";

		var stopwatch = new Stopwatch();
		stopwatch.Start();
		var generatedWordList = new List<PathObject>();
		var random = new Random();
		var len = random.Next(64, 256);
		if (!string.IsNullOrWhiteSpace(param.Condition.MissionChar))
			generatedWordList.Add(new PathObject(firstChar + random.GenerateRandomString(random.Next(16, 64), false) + new string(param.Condition.MissionChar[0], len) + random.GenerateRandomString(random.Next(16, 64), false), WordCategories.None, len));
		for (var i = 0; i < 10; i++)
			generatedWordList.Add(new PathObject(firstChar + random.GenerateRandomString(len, false), WordCategories.None, 0));
		stopwatch.Stop();

		var list = generatedWordList.ToImmutableList();
		PathUpdated(new PathUpdateEventArgs(param, PathFindResultType.Found, list, list, Convert.ToInt32(stopwatch.ElapsedMilliseconds)));
	}

	private void PathUpdated(PathUpdateEventArgs eventArgs) => OnPathUpdated?.Invoke(this, eventArgs);
}
