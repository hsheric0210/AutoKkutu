using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class VacuumQuery : SqlQuery<int>
{
	public VacuumQuery(AbstractDatabaseConnection connection) : base(connection)
	{
	}

	public override int Execute() => Connection.Execute("VACUUM;");
}
