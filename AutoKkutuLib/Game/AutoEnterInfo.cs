namespace AutoKkutuLib.Game;

public struct AutoEnterInfo
{
	public string? Content { get; set; }
	public AutoEnterOptions Options { get; }
	public PathDetails PathInfo { get; }
	public int WordIndex { get; }

	public int GetTotalDelay() => Options.GetDelayFor(Content);

	public AutoEnterInfo(AutoEnterOptions delayParam, PathDetails param, string? content = null, int wordIndex = 0)
	{
		Options = delayParam;
		PathInfo = param;
		Content = content;
		WordIndex = wordIndex;
	}

	public static implicit operator PathDetails(AutoEnterInfo param) => param.PathInfo;
	public static implicit operator AutoEnterOptions(AutoEnterInfo param) => param.Options;

	public bool HasFlag(PathFlags flag) => PathInfo.HasFlag(flag);
}

public readonly struct AutoEnterOptions
{
	private readonly bool delayAfterInput;
	private readonly int startDelay;
	private readonly int startDelayRandom;
	private readonly int delayPerChar;
	private readonly int delayPerCharRandom;
	private readonly int delayBeforeKeyUp;
	private readonly int delayBeforeShiftKeyUp;
	private readonly bool simulateInput;

	public bool DelayEnabled { get; }

	public bool IsDelayPerCharRandomized => delayPerCharRandom > 0;
	public bool DelayStartAfterCharEnterEnabled => DelayEnabled && delayAfterInput;
	public int DelayBeforeKeyUp => SimulateInput ? delayBeforeKeyUp : 0;
	public int DelayBeforeShiftKeyUp => SimulateInput ? delayBeforeShiftKeyUp : 0;
	public bool SimulateInput => DelayEnabled && simulateInput;

	public AutoEnterOptions(bool delay, bool delayAfterInput, int startDelay, int startDelayRandom, int delayPerChar, int delayPerCharRandom, bool simulateInput)
	{
		DelayEnabled = delay;
		this.delayAfterInput = delayAfterInput;
		this.startDelay = startDelay;
		this.simulateInput = simulateInput;
		this.startDelayRandom = startDelayRandom;
		this.delayPerChar = delayPerChar;
		this.delayPerCharRandom = delayPerCharRandom;

		//TODO: Make them configurable
		delayBeforeKeyUp = (int)(delayPerChar * 1.5);
		delayBeforeShiftKeyUp = (int)(delayPerChar * 1.2);
	}

	private static int GetDelay(int delay, int random)
	{
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
	public int GetMaxDelay(string? input) => startDelay + startDelay * startDelayRandom / 100 + (string.IsNullOrEmpty(input) ? 0 : (input.Length * (delayPerChar + delayPerChar * delayPerCharRandom / 100)));

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
	public int GetMinDelay(string? input) => startDelay - startDelay * startDelayRandom / 100 + (string.IsNullOrEmpty(input) ? 0 : (input.Length * (delayPerChar - delayPerChar * delayPerCharRandom / 100)));
}