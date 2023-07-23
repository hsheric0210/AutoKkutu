using System;

namespace AutoKkutuGui;
public readonly struct EnterDelayConfig : IEquatable<EnterDelayConfig>
{
	public readonly bool IsEnabled { get; }
	public readonly int StartDelay { get; }
	public readonly int StartDelayRandom { get; }
	public readonly int DelayPerChar { get; }
	public readonly int DelayPerCharRandom { get; }

	public EnterDelayConfig(bool isEnabled, int startDelay, int startDelayRandom, int delayPerChar, int delayPerCharRandom)
	{
		IsEnabled = isEnabled;
		StartDelay = startDelay;
		StartDelayRandom = startDelayRandom;
		DelayPerChar = delayPerChar;
		DelayPerCharRandom = delayPerCharRandom;
	}

	public override bool Equals(object? obj) => obj is EnterDelayConfig config && Equals(config);
	public bool Equals(EnterDelayConfig other) => IsEnabled == other.IsEnabled && StartDelay == other.StartDelay && StartDelayRandom == other.StartDelayRandom && DelayPerChar == other.DelayPerChar && DelayPerCharRandom == other.DelayPerCharRandom;
	public override int GetHashCode() => HashCode.Combine(IsEnabled, StartDelay, StartDelayRandom, DelayPerChar, DelayPerCharRandom);

	public static bool operator ==(EnterDelayConfig left, EnterDelayConfig right) => left.Equals(right);
	public static bool operator !=(EnterDelayConfig left, EnterDelayConfig right) => !(left == right);
}
