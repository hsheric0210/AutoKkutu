using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public class MySQLSection : ConfigurationSection
	{
		[ConfigurationProperty("mysql_host")]
		public string MySQLHost
		{
			get
			{
				return (string)base["mysql_host"];
			}
			set
			{
				base["mailServerIP"] = value;
			}
		}

		[ConfigurationProperty("mysql_port")]
		public string MySQLPort
		{
			get
			{
				return (string)base["mysql_port"];
			}
			set
			{
				base["mysql_port"] = value;
			}
		}

		[ConfigurationProperty("mysql_id")]
		public string MySQLID
		{
			get
			{
				return (string)base["mysql_id"];
			}
			set
			{
				base["mysql_id"] = value;
			}
		}

		[ConfigurationProperty("mysql_password")]
		public string MySQLPassword
		{
			get
			{
				return (string)base["mysql_password"];
			}
			set
			{
				base["mysql_password"] = value;
			}
		}

		[ConfigurationProperty("mysql_db")]
		public string MySQLDatabase
		{
			get
			{
				return (string)base["mysql_db"];
			}
			set
			{
				base["mysql_db"] = value;
			}
		}
	}
}
