using AutoKkutuLib.Hangul;

namespace AutoKkutuLib.Game.Enterer;
public class JavaScriptInputSimulator : InputSimulatorBase
{
	private bool shiftState;

	public JavaScriptInputSimulator(IGame game) : base(EntererMode.SimulateInputJavaScript, game)
	{
	}

	protected async override Task AppendAsync(EnterOptions options, InputCommand input)
	{
		if (shiftState && input.ShiftState == ShiftState.Release)
			shiftState = false;
		else if (!shiftState && input.ShiftState == ShiftState.Press)
			shiftState = true;

		game.AppendChat(input.TextUpdate, options.IsJavaScriptInputSimulatorSendKeyEvents, input.Key, shiftState, input.ImeState == ImeState.Korean, options.DelayBeforeKeyUp);
	}
}
