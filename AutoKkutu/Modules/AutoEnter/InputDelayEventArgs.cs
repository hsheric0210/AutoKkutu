using System;

namespace AutoKkutu.Modules.AutoEnter
{

	public class InputDelayEventArgs : EventArgs
	{
		public int Delay
		{
			get;
		}

		public string? PathAttributes
		{
			get;
		}

		public InputDelayEventArgs(int delay, string? pathAttributes = null)
		{
			Delay = delay;
			PathAttributes = pathAttributes;
		}
	}
}
