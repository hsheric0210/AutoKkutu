﻿namespace AutoKkutuLib.Game.Events;

public class RoundChangeEventArgs : EventArgs
{
	public int RoundIndex
	{
		get;
	}

	public string RoundWord
	{
		get;
	}

	public RoundChangeEventArgs(int roundIndex, string roundWord)
	{
		RoundIndex = roundIndex;
		RoundWord = roundWord;
	}
}