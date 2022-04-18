using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public class PostgreSQLSection : ConfigurationSection
	{
		[ConfigurationProperty("pgsql_host")]
		public string PostgreSQLHost
		{
			get
			{
				return (string)base["pgsql_host"];
			}
			set
			{
				base["mailServerIP"] = value;
			}
		}

		[ConfigurationProperty("pgsql_port")]
		public string PostgreSQLPort
		{
			get
			{
				return (string)base["pgsql_port"];
			}
			set
			{
				base["pgsql_port"] = value;
			}
		}

		[ConfigurationProperty("pgsql_id")]
		public string PostgreSQLID
		{
			get
			{
				return (string)base["pgsql_id"];
			}
			set
			{
				base["pgsql_id"] = value;
			}
		}

		[ConfigurationProperty("pgsql_password")]
		public string PostgreSQLPassword
		{
			get
			{
				return (string)base["pgsql_password"];
			}
			set
			{
				base["pgsql_password"] = value;
			}
		}

		[ConfigurationProperty("pgsql_db")]
		public string PostgreSQLDatabase
		{
			get
			{
				return (string)base["pgsql_db"];
			}
			set
			{
				base["pgsql_db"] = value;
			}
		}
	}
}
