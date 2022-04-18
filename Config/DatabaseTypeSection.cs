using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public class DatabaseTypeSection : ConfigurationSection
	{
		[ConfigurationProperty("type", DefaultValue = "SQLite")]
		public string Title
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
