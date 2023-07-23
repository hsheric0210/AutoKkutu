using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class PostgreSqlVacuumFullQuery : VacuumQuery
{
	internal PostgreSqlVacuumFullQuery(DbConnectionBase connection) : base(connection) { }

	public override int Execute() => Connection.Execute("VACUUM FULL;");
}
