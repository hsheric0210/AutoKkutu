namespace AutoKkutuLib.Game;

public struct AutoEnterParameter
{
	public string? Content { get; set; }
	public AutoEnterDelayParameter DelayParameter { get; }
	public bool SimulateInput { get; }
	public PathFinderParameter PathInfo { get; }
	public int WordIndex { get; }
	public bool CanSimulateInput => DelayParameter.DelayEnabled && DelayParameter.DelayPerCharEnabled && SimulateInput;
	public int RealDelay => DelayParameter.CalcRealDelay(Content);

	public AutoEnterParameter(AutoEnterDelayParameter delayParam, bool simulateInput, PathFinderParameter param, string? content = null, int wordIndex = 0)
	{
		DelayParameter = delayParam;
		SimulateInput = simulateInput;
		PathInfo = param;
		Content = content;
		WordIndex = wordIndex;
	}

	public static implicit operator PathFinderParameter(AutoEnterParameter param) => param.PathInfo;
	public static implicit operator AutoEnterDelayParameter(AutoEnterParameter param) => param.DelayParameter;

	public bool HasFlag(PathFinderFlags flag) => PathInfo.HasFlag(flag);
}

public readonly struct AutoEnterDelayParameter
{
	private readonly bool delayAfterInput;
	private readonly int delayInMillis;
	private readonly bool delayPerCharEnabled;

	public bool DelayEnabled { get; }
	public bool DelayStartAfterCharEnterEnabled => DelayEnabled && delayAfterInput;
	public int DelayInMillis => DelayEnabled ? delayInMillis : 0;
	public bool DelayPerCharEnabled => DelayEnabled && delayPerCharEnabled;

	public AutoEnterDelayParameter(bool delay, bool delayAfterInput, int delayMs, bool delayPerChar)
	{
		this.delayAfterInput = delayAfterInput;
		DelayEnabled = delay;
		delayInMillis = delayMs;
		delayPerCharEnabled = delayPerChar;
	}

	public int CalcRealDelay(string content) => DelayInMillis * (DelayPerCharEnabled ? (content ?? throw new ArgumentNullException(nameof(content))).Length : 1);
}