using Dapper;
using System.Data;

namespace AutoKkutuLib.Database.Sql.Query;
public class VacuumQuery : SqlQuery<int>
{
	public VacuumQuery(DbConnectionBase connection) : base(connection)
	{
	}

	public override int Execute()
	{
		LibLogger.Debug<VacuumQuery>("Running vacuum cleaner on database.");
		return Connection.Execute("VACUUM;", commandType: CommandType.Text);
	}
}
