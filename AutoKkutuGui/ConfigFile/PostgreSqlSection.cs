using System.Configuration;

namespace AutoKkutuGui.ConfigFile;

public class PostgreSqlSection : ConfigurationSection
{
	[ConfigurationProperty("connectionString")]
	public string ConnectionString
	{
		get => (string)base["connectionString"];
		set => base["connectionString"] = value;
	}
}
