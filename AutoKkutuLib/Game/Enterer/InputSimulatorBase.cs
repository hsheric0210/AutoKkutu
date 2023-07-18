using AutoKkutuLib.Hangul;
using Serilog;
using System.Collections.Immutable;
using System.Threading;

namespace AutoKkutuLib.Game.Enterer;
public abstract class InputSimulatorBase : EntererBase
{
	private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

	public InputSimulatorBase(string name, IGame game) : base(name, game)
	{
	}

	protected abstract ValueTask AppendAsync(EnterOptions options, InputCommand input);
	protected virtual async ValueTask SimulationStarted() { }
	protected virtual async ValueTask SimulationFinished() { }

	protected override async ValueTask SendAsync(EnterInfo info)
	{
		if (semaphore.CurrentCount == 0)
		{
			// 최후의 보루: Semaphore 사용해서 여러 입력 작업 동시 진행 방지
			Log.Warning("Semaphore prevented duplicate entering.");
			return;
		}

		await semaphore.WaitAsync();
		try
		{
			if (info.HasFlag(PathFlags.PreSearch))
			{
				Interlocked.CompareExchange(ref isPreinputSimInProg, 1, 0);
				Interlocked.CompareExchange(ref isPreinputFinished, 0, 1);
			}

			await SimulationStarted();

			var content = info.Content;
			var valid = true;

			var list = new List<HangulSplit>();
			foreach (var ch in content)
				list.Add(HangulSplit.Parse(ch));

			var recomp = new HangulRecomposer(KeyboardLayout.QWERTY, list.ToImmutableList()); // TODO: Make KeyboardLayout configurable with AutoEnterOptions
			var inputList = recomp.Recompose();

			var startDelay = info.Options.GetStartDelay();
			await Task.Delay(startDelay);

			Log.Information(I18n.Main_InputSimulating, content);
			game.UpdateChat("");

			foreach (var input in inputList)
			{
				Log.Debug("Input requested: {ipt}", input);
				if (!CanPerformAutoEnterNow(info.PathInfo, !IsPreinputSimInProg))
				{
					valid = false; // Abort
					break;
				}

				var delay = info.Options.GetDelayPerChar();
				await AppendAsync(info.Options, input);
				await Task.Delay(delay);
			}

			await SimulationFinished();

			if (IsPreinputSimInProg) // As this function runs asynchronously, this value could have been changed.
			{
				Interlocked.CompareExchange(ref isPreinputSimInProg, 0, 1);
				Interlocked.CompareExchange(ref isPreinputFinished, 1, 0);
				return; // Don't submit yet
			}

			TrySubmitInput(valid);
		}
		finally
		{
			semaphore.Release();
		}
	}
}
