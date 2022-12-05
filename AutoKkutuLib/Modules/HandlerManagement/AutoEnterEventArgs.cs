using System;

namespace AutoKkutuLib.Modules.AutoEntering;

public class AutoEnterEventArgs : EventArgs
{
	public string Content
	{
		get;
	}

	public AutoEnterEventArgs(string content)
	{
		Content = content;
	}
}
