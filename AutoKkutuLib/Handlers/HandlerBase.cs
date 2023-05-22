namespace AutoKkutuLib.Handlers;

public abstract class HandlerBase
{
	public abstract string HandlerName { get; }
	public abstract IReadOnlyCollection<Uri> UrlPattern { get; }
	public abstract BrowserBase Browser { get; }

	public abstract GameMode GameMode { get; }
	public abstract bool IsGameInProgress { get; }
	public abstract bool IsMyTurn { get; }
	public abstract string PresentedWord { get; }
	public abstract string MissionChar { get; }
	public abstract string ExampleWord { get; }
	public abstract int RoundIndex { get; }
	public abstract string RoundText { get; }
	public abstract float RoundTime { get; }
	public abstract float TurnTime { get; }
	public abstract string UnsupportedWord { get; }

	public abstract void ClickSubmit();
	public abstract string GetWordInHistory(int index);
	public virtual void RegisterRoundIndexFunction() { }
	public abstract void UpdateChat(string input);
}