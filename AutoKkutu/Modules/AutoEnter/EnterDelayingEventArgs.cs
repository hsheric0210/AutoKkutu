using AutoKkutu.Constants;
using AutoKkutu.Modules.PathFinder;
using AutoKkutu.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AutoKkutu.Modules.AutoEnter
{

	public class EnterDelayingEventArgs : EventArgs
	{
		public int Delay
		{
			get;
		}

		public string? PathAttributes
		{
			get;
		}

		public EnterDelayingEventArgs(int delay, string? pathAttributes = null)
		{
			Delay = delay;
			PathAttributes = pathAttributes;
		}
	}
}
