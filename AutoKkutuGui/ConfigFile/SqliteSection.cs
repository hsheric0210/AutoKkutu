using System.Configuration;

namespace AutoKkutuGui.ConfigFile;

public class SqliteSection : ConfigurationSection
{
	[ConfigurationProperty("file")]
	public string File
	{
		get => (string)base["file"];
		set => base["file"] = value;
	}
}
