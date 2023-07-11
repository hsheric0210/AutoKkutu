using System.Text;

namespace AutoKkutuLib.Game;

public struct AutoEnterInfo : IEquatable<AutoEnterInfo>
{
	public string? Content { get; set; }
	public AutoEnterOptions Options { get; }
	public PathDetails PathInfo { get; }

	public int GetTotalDelay() => Options.GetDelayFor(Content);

	public AutoEnterInfo(AutoEnterOptions delayParam, PathDetails param, string? content = null)
	{
		Options = delayParam;
		PathInfo = param;
		Content = content;
	}

	public static implicit operator PathDetails(AutoEnterInfo param) => param.PathInfo;
	public static implicit operator AutoEnterOptions(AutoEnterInfo param) => param.Options;

	public static bool operator ==(AutoEnterInfo left, AutoEnterInfo right) => left.Equals(right);
	public static bool operator !=(AutoEnterInfo left, AutoEnterInfo right) => !(left == right);

	public bool HasFlag(PathFlags flag) => PathInfo.HasFlag(flag);

	public override string ToString()
	{
		var builder = new StringBuilder();
		builder.Append(nameof(AutoEnterInfo)).Append('{');
		builder.Append(nameof(Content)).Append(": ").Append(Content).Append(", ");
		builder.Append(nameof(Options)).Append(": ").Append(Options).Append(", ");
		builder.Append(nameof(PathInfo)).Append(": ").Append(PathInfo);
		return builder.Append('}').ToString();
	}
	public override bool Equals(object? obj) => obj is AutoEnterInfo info && Equals(info);
	public bool Equals(AutoEnterInfo other) => Content == other.Content && Options.Equals(other.Options) && PathInfo.Equals(other.PathInfo);
}

public readonly struct AutoEnterOptions : IEquatable<AutoEnterOptions>
{
	private readonly bool delayAfterInput;
	private readonly int startDelay;
	private readonly int startDelayRandom;
	private readonly int delayPerChar;
	private readonly int delayPerCharRandom;
	private readonly int delayBeforeKeyUp;
	private readonly int delayBeforeShiftKeyUp;

	public bool DelayEnabled { get; }
	public AutoEnterMode Mode { get; }

	public bool IsDelayPerCharRandomized => delayPerCharRandom > 0;

	/// <summary>
	/// Only used in 'Immediately' mode
	/// </summary>
	public bool DelayStartAfterCharEnterEnabled => DelayEnabled && delayAfterInput;
	public int DelayBeforeKeyUp => Mode == AutoEnterMode.EnterImmediately ? 0 : delayBeforeKeyUp;
	public int DelayBeforeShiftKeyUp => Mode == AutoEnterMode.EnterImmediately ? 0 : delayBeforeShiftKeyUp;

	public AutoEnterOptions(AutoEnterMode mode, bool delay, bool delayAfterInput, int startDelay, int startDelayRandom, int delayPerChar, int delayPerCharRandom)
	{
		Mode = mode;
		DelayEnabled = delay;
		this.delayAfterInput = delayAfterInput;
		this.startDelay = startDelay;
		this.startDelayRandom = startDelayRandom;
		this.delayPerChar = delayPerChar;
		this.delayPerCharRandom = delayPerCharRandom;

		//TODO: Make them configurable
		delayBeforeKeyUp = (int)(delayPerChar * 1.5);
		delayBeforeShiftKeyUp = (int)(delayPerChar * 1.2);
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

	/// <summary>
	/// 주어진 단어에 대해 최악의 경우(입력 시작 시간과 글자 입력 간 지연 시간이 최대)를 가정하고 최대 입력 지연 시간을 계산하여 반환합니다.
	/// </summary>
	/// <param name="input">입려하려는 내용</param>
	public int GetMaxDelay(string? input) => DelayEnabled ? (startDelay + startDelay * startDelayRandom / 100 + (string.IsNullOrEmpty(input) ? 0 : (input.Length * (delayPerChar + delayPerChar * delayPerCharRandom / 100)))) : 0;

	/// <summary>
	/// 주어진 단어에 대해 랜덤값 적용된 입력 시작 지연과 랜덤값 적용된 글자 입력 간 지연을 고려하여 적용될 총 딜레이.
	/// 랜덤값이 적용되었기에 호출 때마다 값이 달라진다는 점에 유의.
	/// </summary>
	/// <param name="input">입력하려는 단어</param>
	public int GetDelayFor(string? input) => GetStartDelay() + (string.IsNullOrEmpty(input) ? 0 : (input.Length * GetDelayPerChar()));

	/// <summary>
	/// 주어진 단어에 대해 최적의 경우(입력 시작 시간과 글자 입력 간 지연 시간이 최소)를 가정하고 최소 입력 지연 시간을 계산하여 반환합니다.
	/// </summary>
	/// <param name="input">입력하려는 단어</param>
	public int GetMinDelay(string? input) => DelayEnabled ? (startDelay - startDelay * startDelayRandom / 100 + (string.IsNullOrEmpty(input) ? 0 : (input.Length * (delayPerChar - delayPerChar * delayPerCharRandom / 100)))) : 0;
	public override bool Equals(object? obj) => obj is AutoEnterOptions options && Equals(options);
	public bool Equals(AutoEnterOptions other) => delayAfterInput == other.delayAfterInput && startDelay == other.startDelay && startDelayRandom == other.startDelayRandom && delayPerChar == other.delayPerChar && delayPerCharRandom == other.delayPerCharRandom && delayBeforeKeyUp == other.delayBeforeKeyUp && delayBeforeShiftKeyUp == other.delayBeforeShiftKeyUp && DelayEnabled == other.DelayEnabled && Mode == other.Mode && DelayBeforeKeyUp == other.DelayBeforeKeyUp && DelayBeforeShiftKeyUp == other.DelayBeforeShiftKeyUp;

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(delayAfterInput);
		hash.Add(startDelay);
		hash.Add(startDelayRandom);
		hash.Add(delayPerChar);
		hash.Add(delayPerCharRandom);
		hash.Add(delayBeforeKeyUp);
		hash.Add(delayBeforeShiftKeyUp);
		hash.Add(DelayEnabled);
		hash.Add(Mode);
		hash.Add(DelayBeforeKeyUp);
		hash.Add(DelayBeforeShiftKeyUp);
		return hash.ToHashCode();
	}

	public static bool operator ==(AutoEnterOptions left, AutoEnterOptions right) => left.Equals(right);

	public static bool operator !=(AutoEnterOptions left, AutoEnterOptions right) => !(left == right);

	public override string ToString()
	{
		var builder = new StringBuilder().Append(nameof(AutoEnterOptions)).Append('{');
		builder.Append(nameof(Mode)).Append(": ").Append(Mode).Append(", ");
		builder.Append(nameof(DelayEnabled)).Append(": ").Append(DelayEnabled).Append(", ");
		builder.Append(nameof(startDelay)).Append(": ").Append(startDelay).Append(" (random: ").Append(startDelayRandom).Append("%), ");
		builder.Append(nameof(delayPerChar)).Append(": ").Append(delayPerChar).Append(" (random: ").Append(delayPerCharRandom).Append("%), ");
		builder.Append(nameof(delayBeforeKeyUp)).Append(": ").Append(delayBeforeKeyUp).Append(", ");
		builder.Append(nameof(delayBeforeShiftKeyUp)).Append(": ").Append(delayBeforeShiftKeyUp);
		return builder.Append('}').ToString();
	}
}