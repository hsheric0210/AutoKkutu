namespace AutoKkutuLib.Database.Sql.Migrations;
internal interface IMigration
{
	string Name { get; }
	DateTime Date { get; }
	bool ConditionMet();
	void Execute();
}
