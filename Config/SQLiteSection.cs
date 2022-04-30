using System.Configuration;

namespace AutoKkutu
{
	public class SQLiteSection : ConfigurationSection
	{
		[ConfigurationProperty("file")]
		public string File
		{
			get
			{
				return (string)base["file"];
			}
			set
			{
				base["file"] = value;
			}
		}
	}
}
