using Serilog;

namespace AutoKkutuLib.Game;
public partial class Game
{
	/// <summary>
	/// DomPoller들이 사용할 Task의 기본 기틀
	/// </summary>
	/// <param name="mainJob"><paramref name="activateCondition"/>이 만족되었을 시 매 반복마다 실행할 함수</param>
	/// <param name="idleJob"><paramref name="activateCondition"/>이 만족되지 않았을 시 매 반복마다 실행할 함수</param>
	/// <param name="onException">만약 실행 도중 예외가 발생했을 때 호출할 함수</param>
	/// <param name="activateCondition">DomPoller가 본격적으로 활성화될 조건</param>
	/// <param name="intenseInterval"><paramref name="activateCondition"/>가 만족되었을 시 반복 실행 시간 간격 (ms 단위)</param>
	/// <param name="idleInterval"><paramref name="activateCondition"/>가 만족되지 않았을 시 반복 실행 시간 간격 (ms 단위)</param>
	/// <param name="cancelToken">실행 중단 및 반복 종료를 위한 위한 취소 토큰</param>
	/// <returns></returns>
	private static async Task BaseDomPoller(
		Func<Task> mainJob,
		Func<Task>? idleJob,
		Action<Exception> onException,
		Func<bool> activateCondition,
		int intenseInterval,
		int idleInterval,
		CancellationToken cancelToken)
	{
		try
		{
			cancelToken.ThrowIfCancellationRequested();

			while (true)
			{
				if (cancelToken.IsCancellationRequested)
					cancelToken.ThrowIfCancellationRequested();

				if (activateCondition())
				{
					await mainJob();
					await Task.Delay(intenseInterval, cancelToken);
				}
				else
				{
					if (idleJob != null)
						await idleJob();
					await Task.Delay(idleInterval, cancelToken);
				}
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException and not TaskCanceledException)
		{
			onException(ex);
		}
	}

	/// <summary>
	/// 게임이 진행 중일때만 특정 함수를 반복하여 호출하는 DomPoller를 구성합니다.
	/// </summary>
	/// <param name="action">게임이 실행 중일 때 매 반복마다 실행할 함수</param>
	/// <param name="cancelToken">실행 중단 및 반복 종료를 위한 위한 취소 토큰</param>
	/// <param name="watchdogName"></param>
	/// <returns></returns>
	private async Task GameDomPoller(Func<Task> action, CancellationToken cancelToken, string? watchdogName = null, int intenseInterval = intenseInterval)
	{
		await BaseDomPoller(
			action,
			null,
			ex => Log.Error(ex, "Game DomPoller '{0}' exception", watchdogName),
			() => IsGameInProgress,
			intenseInterval,
			idleInterval,
			cancelToken);
	}

	private async Task ConditionlessDomPoller(Func<Task> action, CancellationToken cancelToken, string? watchdogName = null, int intenseInterval = intenseInterval)
	{
		await BaseDomPoller(
			action,
			action,
			ex => Log.Error(ex, "Condition-less DomPoller '{0}' exception.", watchdogName),
			() => IsGameInProgress,
			intenseInterval,
			idleInterval,
			cancelToken);
	}

	private async Task SlowDomPoller(Func<Task> action, CancellationToken cancelToken, string? watchdogName = null)
	{
		await BaseDomPoller(
			action,
			null,
			ex => Log.Error(ex, "Slow DomPoller '{0}' exception.", watchdogName),
			() => true,
			idleInterval,
			idleInterval,
			cancelToken);
	}
}
