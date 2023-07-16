namespace AutoKkutuLib.Game.Enterer;

public interface IEnterer
{
	string EntererName { get; }

	event EventHandler<InputDelayEventArgs>? InputDelayApply;
	event EventHandler<EnterFinishedEventArgs>? EnterFinished;

	void RequestSend(EnterInfo param);
}