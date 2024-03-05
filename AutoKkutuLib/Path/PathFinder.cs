using AutoKkutuLib.Database.Helper;
using AutoKkutuLib.Extension;
using System.Collections.Immutable;
using System.Diagnostics;

namespace AutoKkutuLib.Path;

public class PathFinder
{
	public event EventHandler<PathFinderStateEventArgs> StateChanged;

	private readonly NodeManager nodeManager;
	private readonly PathFilter pathFilter;

	private GameMode gameMode = GameMode.LastAndFirst;
	private PathDetails details;
	private WordPreference preference;

	public PathFinder(NodeManager nodeManager, PathFilter pathFilter)
	{
		this.nodeManager = nodeManager;
		this.pathFilter = pathFilter;
	}

	/// <summary>
	/// 단어 검색을 할 게임 모드를 지정합니다.
	/// </summary>
	/// <param name="gameMode">게임 모드</param>
	/// <returns>빌더 인스턴스</returns>
	public PathFinder SetGameMode(GameMode gameMode)
	{
		this.gameMode = gameMode;
		return this;
	}

	/// <summary>
	/// 단어 검색 시 세부 조건을 지정합니다.
	/// </summary>
	/// <param name="details">단어 검색 세부 조건</param>
	/// <returns>빌더 인스턴스</returns>
	public PathFinder SetPathDetails(PathDetails details)
	{
		this.details = details;
		return this;
	}

	/// <summary>
	/// 단어 검색 시 단어를 정렬할 우선 순위를 설정합니다.
	/// </summary>
	/// <param name="preference">단어 정렬 우선 순위 데이터</param>
	/// <returns>빌더 인스턴스</returns>
	public PathFinder SetWordPreference(WordPreference preference)
	{
		this.preference = preference;
		return this;
	}

	/// <summary>
	/// 단어 검색을 시작합니다. 검색이 완료되면 지정된 콜백 함수가 호출됩니다.
	/// </summary>
	/// <param name="callback">검색 완료 시 호출할 콜백 함수입니다.</param>
	public void BeginFind(Action<PathFindResult> callback)
		=> Task.Run(async () => callback(await BeginFind()));

	/// <summary>
	/// 단어 검색을 시작합니다. 결과는 비동기적으로 반환됩니다.
	/// </summary>
	/// <returns>단어 검색 결과입니다.</returns>
	public async ValueTask<PathFindResult> BeginFind()
	{
		// TODO: This check could be moved to caller site
		if (gameMode == GameMode.TypingBattle && !details.HasFlag(PathFlags.DoNotAutoEnter)) // 타자 대결 모드에서는 단어 검색 수행 X (단, 수동 검색의 경우는 제외)
			return PathFindResult.Empty(details);

		if (gameMode.IsFreeMode())
			return GenerateRandomPath();

		var stopWatch = new Stopwatch();
		try
		{
			WordCondition condition = details;

			// End node check
			if (IsEndNode())
			{
				LibLogger.Warn<PathFinder>("End node: {node1}, {node2}", condition.Char, condition.SubChar);
				LibLogger.Warn<PathFinder>(I18n.PathFinderFailed_Endword);

				//AutoKkutuMain.ResetPathList();
				//AutoKkutuMain.UpdateSearchState(null, true);
				//AutoKkutuMain.UpdateStatusMessage(StatusMessage.EndWord);

				StateChanged?.Invoke(this, new PathFinderStateEventArgs(PathFinderState.EndWord));
				return PathFindResult.EndWord(details);
			}

			StateChanged?.Invoke(this, new PathFinderStateEventArgs(PathFinderState.Finding));

			if (condition.SubAvailable)
				LibLogger.Info<PathFinder>(I18n.PathFinder_FindPath_Substituation, condition.Char, condition.SubChar);
			else
				LibLogger.Info<PathFinder>(I18n.PathFinder_FindPath, condition.Char);

			// Begin the finding
			stopWatch.Start();
			var totalWordList = nodeManager.DbConnection.Query.FindWord(gameMode, preference).Execute(details);
			LibLogger.Info<PathFinder>(I18n.PathFinder_FoundPath, totalWordList.Count, details.HasFlag(PathFlags.UseAttackWord), details.HasFlag(PathFlags.UseEndWord));
			stopWatch.Stop();

			StateChanged?.Invoke(this, new PathFinderStateEventArgs(PathFinderState.Finding));

			var availableWordList = pathFilter.FilterPathList(totalWordList, details.ReuseAlreadyUsed);

			LibLogger.Info<PathFinder>(I18n.PathFinder_FoundPath_Ready, totalWordList.Count, stopWatch.ElapsedMilliseconds);
			return PathFindResult.Finished(details, totalWordList, availableWordList, stopWatch.ElapsedMilliseconds);
		}
		catch (Exception ex)
		{
			stopWatch.Stop();
			LibLogger.Error<PathFinder>(ex, I18n.PathFinderFailed_Exception);
			StateChanged?.Invoke(this, new PathFinderStateEventArgs(ex));
			return PathFindResult.Error(details);
		}
	}

	private bool IsEndNode()
	{
		WordCondition condition = details;
		if (condition.SubAvailable && !nodeManager.GetEndNodeForMode(gameMode).Contains(condition.SubChar!))
			return false;

		return nodeManager.GetEndNodeForMode(gameMode).Contains(condition.Char);
	}

	private PathFindResult GenerateRandomPath()
	{
		var firstChar = gameMode == GameMode.LastAndFirstFree ? details.Condition.Char : "";

		var stopwatch = new Stopwatch();
		stopwatch.Start();
		var generatedWordList = new List<PathObject>();
		var random = new Random();
		var len = random.Next(64, 256);

		// 맨 윗줄에 미션 글자 들어간 단어 추가
		if (!string.IsNullOrWhiteSpace(details.Condition.MissionChar))
			generatedWordList.Add(new PathObject(firstChar + random.NextString(random.Next(16, 64), false) + new string(details.Condition.MissionChar[0], len) + random.NextString(random.Next(16, 64), false), WordCategories.None, len));

		// 무작위 단어 10개 추가 (TODO: 개수 조정할 수 있도록 하기)
		for (var i = 0; i < 10; i++)
			generatedWordList.Add(new PathObject(firstChar + random.NextString(len, false), WordCategories.None, 0));
		stopwatch.Stop();

		var list = generatedWordList.ToImmutableList();
		return PathFindResult.Finished(details, list, list, stopwatch.ElapsedMilliseconds);
	}
}
