using AutoKkutuLib.Hangul;

namespace AutoKkutuLib.Game.Enterer;
public class JavaScriptInputSimulator : InputSimulatorBase
{
	public const string Name = "JavaScriptInputSimulate";

	private bool shiftState;

	public JavaScriptInputSimulator(IGame game) : base(Name, game)
	{
	}

	protected async override ValueTask AppendAsync(EnterOptions options, InputCommand input)
	{
		if (shiftState && input.ShiftState == ShiftState.Release)
			shiftState = false;
		else if (!shiftState && input.ShiftState == ShiftState.Press)
			shiftState = true;

		game.AppendChat(input.TextUpdate, options.IsJavaScriptInputSimulatorSendKeyEvents, input.Key, shiftState, input.ImeState == ImeState.Korean, options.DelayBeforeKeyUp);
	}
}
