namespace AutoKkutuLib.Database.Sql.Query;
public class MySqlVacuumQuery : VacuumQuery
{
	internal MySqlVacuumQuery(DbConnectionBase connection) : base(connection) { }

	public override int Execute() => 0;
}
