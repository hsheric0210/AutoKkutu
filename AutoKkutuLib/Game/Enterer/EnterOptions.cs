using System.Text;

namespace AutoKkutuLib.Game.Enterer;

public readonly struct EnterOptions : IEquatable<EnterOptions>
{
	private readonly bool delayAfterInput;
	private readonly int startDelay;
	private readonly int startDelayRandom;
	private readonly int delayPerChar;
	private readonly int delayPerCharRandom;

	public bool DelayEnabled { get; }

	public bool IsDelayPerCharRandomized => delayPerCharRandom > 0;
	public bool IsJavaScriptInputSimulatorSendKeyEvents { get; }
	/// <summary>
	/// Only used in 'Immediately' mode
	/// </summary>
	public bool DelayStartAfterCharEnterEnabled => DelayEnabled && delayAfterInput;
	public int DelayBeforeKeyUp { get; }

	public EnterOptions(bool delay, bool delayAfterInput, bool jsinputSendKeys, int startDelay, int startDelayRandom, int delayPerChar, int delayPerCharRandom)
	{
		DelayEnabled = delay;
		IsJavaScriptInputSimulatorSendKeyEvents = jsinputSendKeys;
		this.delayAfterInput = delayAfterInput;
		this.startDelay = startDelay;
		this.startDelayRandom = startDelayRandom;
		this.delayPerChar = delayPerChar;
		this.delayPerCharRandom = delayPerCharRandom;

		//TODO: Make them configurable
		DelayBeforeKeyUp = (int)(delayPerChar * 1.5);
	}

	private int GetDelay(int delay, int random)
	{
		if (!DelayEnabled)
			return 0;
		if (delay <= 0)
			return delay;
		return Random.Shared.Next(delay - delay * random / 100, delay + delay * random / 100 + 1); // +1 to bypass exclusive-maxValue restriction
	}

	/// <summary>
	/// 입력 시작 지연 시간에 랜덤값을 적용하여 반환합니다.
	/// </summary>
	public int GetStartDelay() => GetDelay(startDelay, startDelayRandom);

	/// <summary>
	/// 단어 당 입력 지연 시간에 랜덤값을 적용하여 반환합니다.
	/// </summary>
	public int GetDelayPerChar() => GetDelay(delayPerChar, delayPerCharRandom);

	public int GetMaxDelayPerChar() => delayPerChar + delayPerChar * delayPerCharRandom / 100;

	/// <summary>
	/// 주어진 단어에 대해 최악의 경우(입력 시작 시간과 글자 입력 간 지연 시간이 최대)를 가정하고 최대 입력 지연 시간을 계산하여 반환합니다.
	/// </summary>
	/// <param name="input">입려하려는 내용</param>
	public int GetMaxDelay(string? input) => DelayEnabled ? startDelay + startDelay * startDelayRandom / 100 + (string.IsNullOrEmpty(input) ? 0 : input.Length * GetMaxDelayPerChar()) : 0;

	/// <summary>
	/// 주어진 단어에 대해 랜덤값 적용된 입력 시작 지연과 랜덤값 적용된 글자 입력 간 지연을 고려하여 적용될 총 딜레이.
	/// 랜덤값이 적용되었기에 호출 때마다 값이 달라진다는 점에 유의.
	/// </summary>
	/// <param name="input">입력하려는 단어</param>
	public int GetDelayFor(string? input) => GetStartDelay() + (string.IsNullOrEmpty(input) ? 0 : input.Length * GetDelayPerChar());

	/// <summary>
	/// 주어진 단어에 대해 최적의 경우(입력 시작 시간과 글자 입력 간 지연 시간이 최소)를 가정하고 최소 입력 지연 시간을 계산하여 반환합니다.
	/// </summary>
	/// <param name="input">입력하려는 단어</param>
	public int GetMinDelay(string? input) => DelayEnabled ? startDelay - startDelay * startDelayRandom / 100 + (string.IsNullOrEmpty(input) ? 0 : input.Length * (delayPerChar - delayPerChar * delayPerCharRandom / 100)) : 0;
	public override bool Equals(object? obj) => obj is EnterOptions options && Equals(options);
	public bool Equals(EnterOptions other) => delayAfterInput == other.delayAfterInput && startDelay == other.startDelay && startDelayRandom == other.startDelayRandom && delayPerChar == other.delayPerChar && delayPerCharRandom == other.delayPerCharRandom && DelayBeforeKeyUp == other.DelayBeforeKeyUp && DelayEnabled == other.DelayEnabled;

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(delayAfterInput);
		hash.Add(startDelay);
		hash.Add(startDelayRandom);
		hash.Add(delayPerChar);
		hash.Add(delayPerCharRandom);
		hash.Add(DelayBeforeKeyUp);
		hash.Add(DelayEnabled);
		return hash.ToHashCode();
	}

	public static bool operator ==(EnterOptions left, EnterOptions right) => left.Equals(right);

	public static bool operator !=(EnterOptions left, EnterOptions right) => !(left == right);

	public override string ToString()
	{
		var builder = new StringBuilder().Append(nameof(EnterOptions)).Append('{');
		builder.Append(nameof(DelayEnabled)).Append(": ").Append(DelayEnabled).Append(", ");
		builder.Append(nameof(startDelay)).Append(": ").Append(startDelay).Append(" (random: ").Append(startDelayRandom).Append("%), ");
		builder.Append(nameof(delayPerChar)).Append(": ").Append(delayPerChar).Append(" (random: ").Append(delayPerCharRandom).Append("%), ");
		builder.Append(nameof(DelayBeforeKeyUp)).Append(": ").Append(DelayBeforeKeyUp);
		return builder.Append('}').ToString();
	}
}