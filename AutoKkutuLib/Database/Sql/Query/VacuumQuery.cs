using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class VacuumQuery : SqlQuery<int>
{
	public VacuumQuery(AbstractDatabaseConnection connection) : base(connection)
	{
	}

	// https://wiki.postgresql.org/wiki/Deleting_duplicates
	public override int Execute() => Connection.Execute("VACUUM;");
}
