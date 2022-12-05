using AutoKkutuLib.Constants;

namespace AutoKkutuLib.Modules.HandlerManagement;

public class GameModeChangeEventArgs : EventArgs
{
	public GameMode GameMode
	{
		get;
	}

	public GameModeChangeEventArgs(GameMode gameMode) => GameMode = gameMode;
}
