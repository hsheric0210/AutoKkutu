﻿namespace AutoKkutuLib.Modules.AutoEntering;

public class InputDelayEventArgs : EventArgs
{
	public int Delay
	{
		get;
	}

	public int WordIndex
	{
		get;
	}

	public InputDelayEventArgs(int delay, int wordIndex)
	{
		Delay = delay;
		WordIndex = wordIndex;
	}
}
