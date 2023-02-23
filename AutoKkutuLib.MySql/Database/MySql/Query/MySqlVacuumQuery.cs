namespace AutoKkutuLib.Database.Sql.Query;
public class MySqlVacuumQuery : VacuumQuery
{
	internal MySqlVacuumQuery(AbstractDatabaseConnection connection) : base(connection) { }

	public override int Execute() => 0;
}
