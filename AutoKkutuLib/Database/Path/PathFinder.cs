using AutoKkutuLib.Extension;
using System.Collections.Immutable;
using System.Diagnostics;

namespace AutoKkutuLib.Database.Path;

public class PathFinder
{
	private readonly NodeManager nodeManager;
	private readonly PathFilter pathFilter;

	public event EventHandler<PathFinderStateEventArgs>? FindStateChanged;
	public event EventHandler<PathUpdateEventArgs>? PathUpdated;

	public PathFinder(NodeManager nodeManager, PathFilter pathFilter)
	{
		this.nodeManager = nodeManager;
		this.pathFilter = pathFilter;
	}

	/// <summary>
	/// 단어 검색을 요청합니다
	/// </summary>
	/// <param name="gameMode">단어를 검색할 게임 모드</param>
	/// <param name="param">단어 검색 옵션</param>
	/// <param name="preference">단어 검색 우선 순위</param>
	public void FindPath(GameMode gameMode, PathDetails param, WordPreference preference)
	{
		// TODO: This check could be moved to caller site
		if (gameMode == GameMode.TypingBattle && !param.HasFlag(PathFlags.DoNotAutoEnter)) // 타자 대결 모드에서는 단어 검색 수행 X (단, 수동 검색의 경우는 제외)
			return;

		// TODO: This implementation could be moved to caller site
		if (gameMode.IsFreeMode())
		{
			Task.Run(() => GenerateRandomPath(gameMode, param));
			return;
		}

		try
		{
			WordCondition condition = param;
			if (nodeManager.GetEndNodeForMode(gameMode).Contains(condition.Char) && (!condition.SubAvailable || nodeManager.GetEndNodeForMode(gameMode).Contains(condition.SubChar!)))
			{
				LibLogger.Warn<PathFinder>("End node: {node1}, {node2}", condition.Char, condition.SubChar);
				LibLogger.Warn<PathFinder>(I18n.PathFinderFailed_Endword);
				// AutoKkutuMain.ResetPathList();
				//AutoKkutuMain.UpdateSearchState(null, true);
				//AutoKkutuMain.UpdateStatusMessage(StatusMessage.EndWord);
				FindStateChanged?.Invoke(this, new PathFinderStateEventArgs(PathFinderState.EndWord));
			}
			else
			{
				FindStateChanged?.Invoke(this, new PathFinderStateEventArgs(PathFinderState.Finding));

				if (condition.SubAvailable)
					LibLogger.Info<PathFinder>(I18n.PathFinder_FindPath_Substituation, condition.Char, condition.SubChar);
				else
					LibLogger.Info<PathFinder>(I18n.PathFinder_FindPath, condition.Char);

				// Enqueue search
				Task.Run(() => FindPathInternal(gameMode, param, preference));
			}
		}
		catch (Exception ex)
		{
			LibLogger.Error<PathFinder>(ex, I18n.PathFinderFailed_Exception);
		}
	}

	private void FindPathInternal(GameMode mode, PathDetails parameter, WordPreference preference)
	{
		var stopWatch = new Stopwatch();
		stopWatch.Start();

		// Search words from database
		IImmutableList<PathObject> totalWordList;
		try
		{
			totalWordList = nodeManager.DbConnection.Query.FindWord(mode, preference).Execute(parameter);
			LibLogger.Info<PathFinder>(I18n.PathFinder_FoundPath, totalWordList.Count, parameter.HasFlag(PathFlags.UseAttackWord), parameter.HasFlag(PathFlags.UseEndWord));
		}
		catch (Exception e)
		{
			stopWatch.Stop();
			LibLogger.Error<PathFinder>(e, I18n.PathFinder_FindPath_Error);
			NotifyUpdate(new PathUpdateEventArgs(parameter, PathFindResultType.Error, ImmutableList<PathObject>.Empty, ImmutableList<PathObject>.Empty, 0));
			return;
		}

		stopWatch.Stop();

		var availableWordList = pathFilter.FilterPathList(totalWordList, parameter.ReuseAlreadyUsed);
		// If there's no word found (or all words was filtered out)
		if (availableWordList.Count == 0)
		{
			LibLogger.Warn<PathFinder>(I18n.PathFinder_FindPath_NotFound);
			NotifyUpdate(new PathUpdateEventArgs(parameter, PathFindResultType.NotFound, totalWordList, ImmutableList<PathObject>.Empty, Convert.ToInt32(stopWatch.ElapsedMilliseconds)));
			return;
		}

		// Update final lists
		LibLogger.Info<PathFinder>(I18n.PathFinder_FoundPath_Ready, totalWordList.Count, stopWatch.ElapsedMilliseconds);
		NotifyUpdate(new PathUpdateEventArgs(parameter, PathFindResultType.Found, totalWordList, availableWordList, Convert.ToInt32(stopWatch.ElapsedMilliseconds)));
	}

	public void GenerateRandomPath(
		GameMode mode,
		PathDetails param)
	{
		var firstChar = mode == GameMode.LastAndFirstFree ? param.Condition.Char : "";

		var stopwatch = new Stopwatch();
		stopwatch.Start();
		var generatedWordList = new List<PathObject>();
		var random = new Random();
		var len = random.Next(64, 256);
		if (!string.IsNullOrWhiteSpace(param.Condition.MissionChar))
			generatedWordList.Add(new PathObject(firstChar + random.NextString(random.Next(16, 64), false) + new string(param.Condition.MissionChar[0], len) + random.NextString(random.Next(16, 64), false), WordCategories.None, len));
		for (var i = 0; i < 10; i++)
			generatedWordList.Add(new PathObject(firstChar + random.NextString(len, false), WordCategories.None, 0));
		stopwatch.Stop();

		var list = generatedWordList.ToImmutableList();
		NotifyUpdate(new PathUpdateEventArgs(param, PathFindResultType.Found, list, list, Convert.ToInt32(stopwatch.ElapsedMilliseconds)));
	}


	private void NotifyUpdate(PathUpdateEventArgs eventArgs) => PathUpdated?.Invoke(this, eventArgs);
}
