namespace AutoKkutuLib.Game;

public record AutoEnterParameter(
	bool DelayEnabled,
	bool DelayStartAfterCharEnterEnabled,
	int DelayInMillis,
	bool DelayPerCharEnabled,
	bool SimulateInput,
	PathFinderParameter PathFinderParams,
	string Content = "",
	int WordIndex = 0)
{
	public bool CanSimulateInput => DelayEnabled && DelayPerCharEnabled && SimulateInput;
	public int RealDelay => CalcRealDelay(Content);

	public int CalcRealDelay(string content) => DelayInMillis * (DelayPerCharEnabled ? (content ?? throw new ArgumentNullException(nameof(content))).Length : 1);
}
