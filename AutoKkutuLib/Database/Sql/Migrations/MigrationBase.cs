namespace AutoKkutuLib.Database.Sql.Migrations;
internal abstract class MigrationBase : IMigration
{
	public abstract string Name { get; }
	public abstract DateTime Date { get; }
	protected DbConnectionBase Db { get; }

	protected MigrationBase(DbConnectionBase db) => Db = db;

	public abstract bool ConditionMet();
	public abstract void Execute();
}
