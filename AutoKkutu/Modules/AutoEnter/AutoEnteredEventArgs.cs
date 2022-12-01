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

	public class AutoEnteredEventArgs : EventArgs
	{
		public string Content
		{
			get;
		}

		public AutoEnteredEventArgs(string content)
		{
			Content = content;
		}
	}
}
