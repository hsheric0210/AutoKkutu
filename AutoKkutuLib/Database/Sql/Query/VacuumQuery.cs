using Dapper;
using Serilog;

namespace AutoKkutuLib.Database.Sql.Query;
public class VacuumQuery : SqlQuery<int>
{
	public VacuumQuery(AbstractDatabaseConnection connection) : base(connection)
	{
	}

	public override int Execute()
	{
		Log.Debug(nameof(VacuumQuery) + ": Running vacuum cleaner on database.");
		return Connection.Execute("VACUUM;");
	}
}
