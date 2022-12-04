using System;

namespace AutoKkutu.Modules.AutoEntering;

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
