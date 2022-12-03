using System;

namespace AutoKkutu.Modules.HandlerManager
{
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
}
