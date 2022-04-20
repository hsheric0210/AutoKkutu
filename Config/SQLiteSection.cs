using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
