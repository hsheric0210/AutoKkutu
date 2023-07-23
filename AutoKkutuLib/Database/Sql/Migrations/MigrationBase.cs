namespace AutoKkutuLib.Database.Sql.Migrations;
internal abstract class MigrationBase : IMigration
{
	public abstract string Name { get; }
	public abstract DateTime Date { get; }
	protected DbConnectionBase DbConnection { get; }

	protected MigrationBase(DbConnectionBase dbConnetion) => DbConnection = dbConnetion;

	public abstract bool ConditionMet();
	public abstract void Execute();
}
