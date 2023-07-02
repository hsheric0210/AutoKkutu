namespace AutoKkutuLib.Game;

public struct AutoEnterInfo
{
	public string? Content { get; set; }
	public AutoEnterOptions Options { get; }
	public PathDetails PathInfo { get; }
	public int WordIndex { get; }
	public int RealDelay => Options.CalcRealDelay(Content);

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
	private readonly int delayInMillis;
	private readonly bool delayPerCharEnabled;
	private readonly int delayBeforeKeyUp;
	private readonly int delayBeforeShiftKeyUp;
	private readonly bool simulateInput;

	public bool DelayEnabled { get; }
	public bool DelayStartAfterCharEnterEnabled => DelayEnabled && delayAfterInput;
	public int DelayInMillis => DelayEnabled ? delayInMillis : 0;
	public bool DelayPerCharEnabled => DelayEnabled && delayPerCharEnabled;
	public int DelayBeforeKeyUp => SimulateInput ? delayBeforeKeyUp : 0;
	public int DelayBeforeShiftKeyUp => SimulateInput ? delayBeforeShiftKeyUp : 0;
	public bool SimulateInput => DelayEnabled && DelayPerCharEnabled && simulateInput;

	public AutoEnterOptions(bool delay, bool delayAfterInput, int delayMs, bool delayPerChar, bool simulateInput)
	{
		DelayEnabled = delay;
		this.delayAfterInput = delayAfterInput;
		delayInMillis = delayMs;
		delayPerCharEnabled = delayPerChar;
		this.simulateInput = simulateInput;

		//TODO: Make them configurable
		delayBeforeKeyUp = (int)(delayMs * 1.5);
		delayBeforeShiftKeyUp = (int)(delayMs * 1.2);
	}

	public int CalcRealDelay(string? content) => DelayInMillis * (DelayPerCharEnabled && !string.IsNullOrWhiteSpace(content) ? content.Length : 1);
}