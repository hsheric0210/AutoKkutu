using Npgsql;
using System.Data.Common;

namespace AutoKkutu.Databases.PostgreSQL
{
	public class PostgreSQLDatabaseCommand : CommonDatabaseCommand
	{
		public PostgreSQLDatabaseCommand(NpgsqlConnection connection, string command, bool noPrepare = false) : base(noPrepare) => Command = new NpgsqlCommand(command, connection);

		protected override DbParameter[] TranslateParameters(params CommonDatabaseParameter[] parameters) => TranslateParameters<NpgsqlParameter>(parameters);
	}
}
