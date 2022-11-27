using System.Configuration;

namespace AutoKkutu.ConfigFile
{
	public class DatabaseTypeSection : ConfigurationSection
	{
		[ConfigurationProperty("type", DefaultValue = "Local")]
		public string Type
		{
			get
			{
				return (string)base["type"];
			}
			set
			{
				base["type"] = value;
			}
		}
	}
}
