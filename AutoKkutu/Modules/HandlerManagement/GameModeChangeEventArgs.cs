using AutoKkutu.Constants;
using System;

namespace AutoKkutu.Modules.HandlerManagement;

public class GameModeChangeEventArgs : EventArgs
{
	public GameMode GameMode
	{
		get;
	}

	public GameModeChangeEventArgs(GameMode gameMode) => GameMode = gameMode;
}
