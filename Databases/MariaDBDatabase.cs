using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu.Databases
{
	public partial class MariaDBDatabase : MySQLDatabase
	{
		public MariaDBDatabase(string connectionString) : base(connectionString)
		{
		}

		public override string GetDBType() => "MariaDB";
	}
}
