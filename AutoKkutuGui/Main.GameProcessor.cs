//#define SELENIUM
using AutoKkutuLib;
using AutoKkutuLib.Database.Jobs;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Game;
using AutoKkutuLib.Game.Enterer;
using AutoKkutuLib.Path;
using Serilog;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace AutoKkutuGui;

public partial class Main
{
	/// <summary>
	/// 마지막 Pre-search 결과를 나타냅니다. 마지막 Pre-search가 실패하였거나, 실행 조건이 만족되지 않은 경우 <c>null</c>로 설정됩니다.
	/// </summary>
	/// <remarks>
	/// 턴이 시작될 때, 만약 이 검색 결과가 턴 조건과 일치한다면, 새로운 검색을 시작하는 대신 이 검색 결과를 사용함으로서 지연을 줄일 수 있습니다.
	/// </remarks>
	private PathList? preSearch;

	/// <summary>
	/// 마지막으로 자동 검색된 단어 목록을 나타냅니다.
	/// </summary>
	/// <remarks>
	/// 오답 자동 수정을 위해서 사용될 목적으로 설계되었습니다.
	/// </remarks>
	private PathList? autoPathFindCache;

	// TODO: 내가 이번 턴에 패배했고, 라운드가 끝났을 경우
	// 다음 라운드 시작 단어에 대해 미리 Pre-search 및 입력 수행.
	private void OnRoundChanged(object? sender, EventArgs e) => preSearch = null; // Invalidate pre-search result on round changed

	public void OnPathUpdated(PathFindResult args)
	{
		// TryAutoEnter에서는 Enter요청 넣은 직후에 해당 단어를 사용 가능한 목록에서 삭제해 버리기 때문에
		// Pre-input하다가 턴 시작되서 입력 완료하려 할 때, 단어를 찾을 수 없다는 메시지를 띄움.
		// 이는 'preSearch' 단어 목록과 지금 입력하려는 단어 목록을 서로 격리함으로서 해결 가능.
		// 한편, 참고로 랜덤 단어 선택 켜져있을 때 Pre-input하려는 단어와 턴 시작되서 입력하려는 단어가 다를 수 있음에도 어떻게 Pre-input한 결과를 정상적으로 입력 완료하냐면
		// 단어 전체를 확인하는 것이 아닌, 단순히 단어 조건이 현재 조건과 맞는지만 검사하기 때문임. (일종의 부속 효과)
		var pathListCopy = new PathList(args.FilteredWordList, args.Details);

		Log.Verbose(I18n.Main_PathUpdateReceived);
		if (!args.HasFlag(PathFlags.DoNotAutoEnter))
			autoPathFindCache = pathListCopy;

		if (args.HasFlag(PathFlags.PreSearch))
		{
			// 내 턴이 이미 시작한 상태에서 Pre-search 결과 입력을 시작할 경우, 매우 높은 확률로 레이스 컨디션이 발생해 2개의 자동입력이 동시에 진행됨 -> (특히 입력 시뮬레이션 시) 꼬임
			if (AutoKkutu.Game.Session.IsMyTurn() && AutoKkutu.Game.Session.IsTurnInProgress)
			{
				preSearch = null;
				Log.Debug("Pre-search result received but my turn is started. Dropping pre-search result.");
				return;
			}

			preSearch = args.FilteredWordList;
		}

		var autoEnter = Preference.AutoEnterEnabled && !args.HasFlag(PathFlags.DoNotAutoEnter) /*&& !args.HasFlag(PathFinderFlags.PreSearch)*/;

		if (args.Result == PathFindResultType.NotFound && !args.HasFlag(PathFlags.DoNotAutoEnter))
			UpdateStatusMessage(StatusMessage.NotFound); // Not found
		else if (args.Result == PathFindResultType.Error)
			UpdateStatusMessage(StatusMessage.Error); // Error occurred
		else if (!autoEnter)
			UpdateStatusMessage(StatusMessage.Normal);

		if (AutoKkutu.Game.RequestRescanIfPathExpired(args.Details))
		{
			Log.Warning("Expired word condition {path} rejected. Rescanning...", args.Details.Condition);
			return;
		}

		UpdateSearchState(args);
		PathListUpdated?.Invoke(this, new PathListUpdateEventArgs(args.FoundWordList.Select(po => new GuiPathObject(po)).ToImmutableList()));

		if (autoEnter)
		{
			Log.Verbose("Auto-entering on path update...");
			TryAutoEnter(pathListCopy);
		}
	}

	private void TryAutoEnter(PathList list, bool flushPreinputPath = false)
	{
		if (!EntererManager.TryGetEnterer(AutoKkutu.Game, Preference.AutoEnterMode, out var enterer))
		{
			Log.Error("AutoEnter interrupted because the enterer {name} is not available.", Preference.AutoEnterMode);
			return;
		}

		var opt = new EnterOptions(Preference.AutoEnterDelayEnabled, Preference.AutoEnterStartDelay, Preference.AutoEnterStartDelayRandom, Preference.AutoEnterDelayPerChar, Preference.AutoEnterDelayPerCharRandom, 1, 0, GetEnterCustomParameter());
		var time = AutoKkutu.Game.GetTurnTimeMillis();
		var bestPath = list.ChooseBestWord(opt, time, Preference.RandomWordSelection ? Preference.RandomWordSelectionCount : 1);
		if (!bestPath.Available)
		{
			if (bestPath.AllTimeFilteredOut)
			{
				Log.Warning(I18n.Auto_TimeOver);
				UpdateStatusMessage(StatusMessage.AllWordTimeOver, time);
			}
			else
			{
				Log.Warning(I18n.Auto_NoMorePathAvailable);
				UpdateStatusMessage(StatusMessage.NotFound);
			}
		}
		else
		{
			var param = list.Details;
			if (flushPreinputPath)
				param = param.WithoutFlags(PathFlags.PreSearch); // Fixme: 이런 번거로운 방법 대신 더 나은 방법 생각해보기

			enterer.RequestSend(new EnterInfo(opt, param, bestPath.Object.Content));
			list.PushUsed(bestPath);
		}
	}

	/* EVENTS: Handler */

	private void OnGameEnded(object? sender, EventArgs e)
	{
		// TODO: move this to Lib: AutoKkutu.Mediator.cs
		//UpdateSearchState(PathFindResult.Empty(PathDetails.Empty));
		if (Preference.AutoDBUpdateEnabled)
		{
			UpdateStatusMessage(StatusMessage.DatabaseIntegrityCheck, I18n.Status_AutoUpdate);
			var updateTask = new DbUpdateJob(AutoKkutu.NodeManager, AutoKkutu.PathFilter);
			var opts = DbUpdateJob.DbUpdateCategories.None;
			if (Preference.AutoDBWordAddEnabled)
				opts |= DbUpdateJob.DbUpdateCategories.Add;
			if (Preference.AutoDBWordRemoveEnabled)
				opts |= DbUpdateJob.DbUpdateCategories.Remove;
			if (Preference.AutoDBAddEndEnabled)
				opts |= DbUpdateJob.DbUpdateCategories.AddEnd;
			var result = updateTask.Execute(opts);
			UpdateStatusMessage(StatusMessage.DatabaseIntegrityCheckDone, I18n.Status_AutoUpdate, result);
		}
		else
		{
			UpdateStatusMessage(StatusMessage.Wait);
		}
	}

	private void OnGameModeChange(object? sender, GameModeChangeEventArgs args) => Log.Information(I18n.Main_GameModeUpdated, ConfigEnums.GetGameModeName(args.GameMode));

	private void OnMyPathIsUnsupported(object? sender, UnsupportedWordEventArgs args)
	{
		if (!args.Session.IsMyTurn())
			return;

		if (autoPathFindCache is not PathList prevAutoEnter)
		{
			Log.Warning("이전에 수행한 단어 검색 결과를 찾을 수 없습니다!");
			return;
		}

		var word = args.Word;
		Log.Warning(I18n.Main_MyPathIsUnsupported, word);

		if (Preference.AutoEnterEnabled && Preference.AutoFixEnabled)
		{

			if (!EntererManager.TryGetEnterer(AutoKkutu.Game, Preference.AutoEnterMode, out var enterer))
			{
				Log.Error("AutoFix interrupted because the enterer {name} is not available.", Preference.AutoEnterMode);
				return;
			}

			var parameter = new EnterInfo(
							new EnterOptions(Preference.AutoEnterDelayEnabled, Preference.AutoEnterStartDelay, Preference.AutoEnterStartDelayRandom, Preference.AutoEnterDelayPerChar, Preference.AutoEnterDelayPerCharRandom, 1, 0, GetEnterCustomParameter()),
							prevAutoEnter.Details.WithoutFlags(PathFlags.PreSearch));

			var bestPath = prevAutoEnter.ChooseBestWord(parameter.Options, AutoKkutu.Game.GetTurnTimeMillis(), Preference.RandomWordSelection ? Preference.RandomWordSelectionCount : 1);
			if (!bestPath.Available)
			{
				Log.Warning(I18n.Main_NoMorePathAvailable);
				//TODO: NoPathAvailable?.Invoke(this, new NoPathAvailableEventArgs(timeover, AutoKkutu.Game.GetTurnTimeMillis()));
				return;
			}

			enterer.RequestSend(parameter with { Content = bestPath.Object.Content });
			prevAutoEnter.PushUsed(bestPath);
		}
	}

	private void OnTurnStarted(object? sender, TurnStartEventArgs args)
	{
		var isMyTurn = args.Session.IsMyTurn();
		if (isMyTurn && Preference.AutoEnterEnabled)
		{
			if (preSearch is PathList list)
			{
				if (list.Details.Condition.IsSimilar(args.Condition)) // pre-search 해 놓은 검색 결과가 여전히 유효함! 미리 입력해 놓은 단어 그대로 써 먹을 수 있음.
				{
					Log.Debug("Using the pre-search result for: {condition}", list.Details.Condition);
					TryAutoEnter(list, flushPreinputPath: true);
					return;
				}

				Log.Warning("Pre-search path is expired! Presearch: {pre}, Search: {now}", list.Details.Condition, args.Condition);
			}
			else
			{
				Log.Debug("Pre-search data not available. Starting the search.");
			}

		}

		if (!Preference.OnlySearchOnMyTurn || isMyTurn)
		{
			StartPathScan(
				args.Session.GameMode,
				args.Condition,
				isMyTurn ? PathFlags.None : PathFlags.DoNotAutoEnter);  // 다른 사람 턴에 검색된 단어는 자동입력하면 안됨
		}
	}

	private void OnPathRescanRequested(object? sender, WordConditionPresentEventArgs args) => StartPathScan(AutoKkutu.Game.Session.GameMode, args.Condition);

	private void OnTurnEnded(object? sender, TurnEndEventArgs args)
	{
		var turn = args.Session.GetRelativeTurn();

		if (args.Session.IsMyTurn())
		{
			return;
		}

		// Pre-search
		if (turn < 0 || turn != args.Session.GetMyPreviousUserTurn() || string.IsNullOrEmpty(args.Value))
			return;

		var missionChar = args.Session.PreviousTurnMission;
		if (!string.IsNullOrEmpty(missionChar) && args.Value.Contains(missionChar, StringComparison.OrdinalIgnoreCase))
		{
			Log.Information("Unable to pre-search because previous turn value contains mission char '{char}'.", missionChar);
			goto presearchFail;
		}

		var condition = args.Session.GameMode.ConvertWordToCondition(args.Value, args.Session.PreviousTurnMission);
		if (condition == null)
		{
			Log.Information("Unable to pre-search due to the failure of condition extraction from previous turn value.");
			goto presearchFail;
		}

		PerformPreSearchAndPreInput(args.Session.GameMode, (WordCondition)condition);
		return;

presearchFail:
		Log.Verbose("Pre-search result flushed.");
		preSearch = null;
	}

	public void PerformPreSearchAndPreInput(GameMode gameMode, WordCondition condition)
	{
		Log.Verbose("Performing pre-search (+ pre-input) on: {condition}", condition);
		AutoKkutu.CreatePathFinder()
			.SetGameMode(gameMode)
			.SetPathDetails(new PathDetails(condition, SetupPathFinderFlags() | PathFlags.PreSearch, Preference.ReturnModeEnabled, Preference.MaxDisplayedWordCount))
			.SetWordPreference(Preference.ActiveWordPreference)
			.BeginFind(OnPathUpdated);
	}

	private void OnTypingWordPresented(object? sender, WordPresentEventArgs args)
	{
		var word = args.Word;

		if (!Preference.AutoEnterEnabled)
			return;

		if (!EntererManager.TryGetEnterer(AutoKkutu.Game, Preference.AutoEnterMode, out var enterer))
		{
			Log.Error("TypingBattle Auto-Enter interrupted because the enterer {name} is not available.", Preference.AutoEnterMode);
			return;
		}

		enterer.RequestSend(new EnterInfo(
			new EnterOptions(Preference.AutoEnterDelayEnabled, Preference.AutoEnterStartDelay, Preference.AutoEnterStartDelayRandom, Preference.AutoEnterDelayPerChar, Preference.AutoEnterDelayPerCharRandom, 1, 0, GetEnterCustomParameter()),
			PathDetails.Empty.WithFlags(PathFlags.DoNotCheckExpired),
			word));
	}

	private void OnChatUpdated(object? sender, EventArgs args) => ChatUpdated?.Invoke(this, args);
}
