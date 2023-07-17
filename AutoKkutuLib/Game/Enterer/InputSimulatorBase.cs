using AutoKkutuLib.Hangul;
using Serilog;
using System.Collections.Immutable;

namespace AutoKkutuLib.Game.Enterer;
public abstract class InputSimulatorBase : EntererBase
{
	public InputSimulatorBase(string name, IGame game) : base(name, game)
	{
	}

	protected abstract ValueTask AppendAsync(EnterOptions options, InputCommand input);
	protected virtual async ValueTask SimulationStarted() { }
	protected virtual async ValueTask SimulationFinished() { }

	protected override async ValueTask SendAsync(EnterInfo info)
	{
		isPreinputSimInProg = info.HasFlag(PathFlags.PreSearch);
		IsPreinputFinished = false;

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
			if (!CanPerformAutoEnterNow(info.PathInfo, !isPreinputSimInProg))
			{
				valid = false; // Abort
				break;
			}

			var delay = info.Options.GetDelayPerChar();
			await AppendAsync(info.Options, input);
			await Task.Delay(delay);
		}

		await SimulationFinished();

		if (isPreinputSimInProg) // As this function runs asynchronously, this value could have been changed.
		{
			isPreinputSimInProg = false;
			IsPreinputFinished = true;
			return; // Don't submit yet
		}

		TrySubmitInput(valid);
	}
}
