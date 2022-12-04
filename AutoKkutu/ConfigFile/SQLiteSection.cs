﻿using System.Configuration;

namespace AutoKkutu.ConfigFile;

public class SqliteSection : ConfigurationSection
{
	[ConfigurationProperty("file")]
	public string File
	{
		get => (string)base["file"];
		set => base["file"] = value;
	}
}
