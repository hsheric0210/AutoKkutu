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
		var sendKeyEvents = true;
		if (options.CustomParameters is Parameter param)
			sendKeyEvents = param.SendKeyEvents;

		if (shiftState && input.ShiftState == ShiftState.Release)
			shiftState = false;
		else if (!shiftState && input.ShiftState == ShiftState.Press)
			shiftState = true;

		game.AppendChat(input.TextUpdate, sendKeyEvents, input.Key, shiftState, input.ImeState == ImeState.Korean, options.DelayBeforeKeyUp);
	}

	public readonly struct Parameter : IEquatable<Parameter>
	{
		public readonly bool SendKeyEvents { get; }
		public Parameter(bool sendKeyEvents) => SendKeyEvents = sendKeyEvents;

		public override string ToString() => $"{{{nameof(SendKeyEvents)}: {SendKeyEvents}}}";

		public override bool Equals(object? obj) => obj is Parameter parameter && Equals(parameter);
		public bool Equals(Parameter other) => SendKeyEvents == other.SendKeyEvents;
		public override int GetHashCode() => HashCode.Combine(SendKeyEvents);

		public static bool operator ==(Parameter left, Parameter right) => left.Equals(right);
		public static bool operator !=(Parameter left, Parameter right) => !(left == right);
	}
}
