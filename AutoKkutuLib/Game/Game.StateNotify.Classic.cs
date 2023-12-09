using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Game;
public partial class Game
{

	/// <summary>
	/// 클래식 게임 모드(끝말잇기, 앞말잇기 등)에서 턴 시작을 알리고 관련 이벤트들을 호출합니다.
	/// <paramref name="turnIndex"/>를 캐싱하여, 연속된 동일 <paramref name="turnIndex"/>에 대하여 한 번만 반응합니다.
	/// </summary>
	/// <param name="isMyTurn">
	/// 시작된 턴이 확실히 내 턴인지의 여부를 나타냅니다. (DOM에 '채팅창에 내용을 입력하세요!' 창이 떠 있는 등...)
	/// <paramref name="isMyTurn"/>가 <c>false</c>이나, <paramref name="turnIndex"/>를 보면 내 턴인 경우도 존재할 수 있으며 해당 경우 실제로도 내 턴이 맞습니다.
	/// </param>
	/// <param name="turnIndex">
	/// 시작된 턴의 인덱스를 나타냅니다.
	/// 만약 바로 이전에 같은 <paramref name="turnIndex"/>로 이 함수를 호출한 적이 있다면, 이번 새로운 요청은 무시될 수 있습니다.
	/// <paramref name="isMyTurn"/>가 <c>true</c>일 경우, <c>-1</c>일 수도 있으나, 해당 경우 실제로 내 턴이 맞습니다.
	/// </param>
	/// <param name="condition">턴의 단어 조건을 나타냅니다.</param>
	public void NotifyClassicTurnStart(bool isMyTurn, int turnIndex, WordCondition condition)
	{
		if (condition.IsEmpty() && !Session.GameMode.IsConditionlessMode())
		{
			Log.Debug("Ignoring turn start request as condition is empty.");
			return;
		}

		lock (sessionLock)
		{
			if (isMyTurn && turnIndex == -1) // DomHandler: turnIndex가 '-1'이나, isMyTurn은 'true' --- 내 턴 맞음
				turnIndex = Session.GetMyTurnIndex();

			if (!isMyTurn && turnIndex == Session.GetMyTurnIndex()) // WebSocketHandler: turnIndex는 내 턴을 나타내나, isMyTurn은 'false' --- 내 턴 맞음
				isMyTurn = true;

			if (!Session.AmIGaming || Session.TurnIndex == turnIndex && Session.IsTurnInProgress)
				return;

			Session.TurnIndex = turnIndex;
			Session.IsTurnInProgress = true;
			if (!isMyTurn && Session.GetRelativeTurn() == Session.GetMyPreviousUserTurn())
			{
				LibLogger.Debug(gameStateNotify, "Previous user mission character is {char}.", condition.MissionChar);
				Session.PreviousTurnMission = condition.MissionChar;
			}

			LibLogger.Debug(gameStateNotify, "Turn #{turnIndex} arrived (isMyTurn: val={isMyTurn} turn={isMyTurnT}), word condition is {word}.", turnIndex, isMyTurn, Session.IsMyTurn(), condition);

			Session.WordCondition = condition;
			TurnStarted?.Invoke(this, new TurnStartEventArgs(new GameSessionState(Session), condition));
		}
	}

	/// <summary>
	/// 게임의 턴 끝을 알리고 관련 이벤트들을 호출하며, 한 턴에 국한된 캐시들을 초기화합니다.
	/// 딱히 변수를 캐싱하거나 하지 않기 때문에, 한 번의 턴 변경 당 한 번씩만 호출되어야 합니다.
	/// </summary>
	/// <param name="value">끝난 턴에서 유저가 입력한 단어; 빈 문자열일 수 있습니다</param>
	public void NotifyClassicTurnEndOk(string value)
	{
		lock (sessionLock)
		{
			if (!Session.AmIGaming || !Session.IsTurnInProgress)
				return;

			LibLogger.Debug(gameStateNotify, "Turn #{turnIndex} ended. Value is {value}.", Session.TurnIndex, value);
			Session.IsTurnInProgress = false;

			// Clear turn-specific caches
			turnErrorWordCache = null;

			TurnEnded?.Invoke(this, new TurnEndEventArgs(new GameSessionState(Session), value));
		}
	}

}
