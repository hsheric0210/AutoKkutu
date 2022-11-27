using System.Data.Common;
using MySql.Data.MySqlClient;

namespace AutoKkutu.Databases.MySQL
{
	public class MySQLDatabaseCommand : CommonDatabaseCommand
	{
		public MySQLDatabaseCommand(MySqlConnection connection, string command, bool noPrepare = false) : base(noPrepare) => Command = new MySqlCommand(command, connection);

		protected override DbParameter[] TranslateParameters(params CommonDatabaseParameter[] parameters) => TranslateParameters<MySqlParameter>(parameters);
	}
}
