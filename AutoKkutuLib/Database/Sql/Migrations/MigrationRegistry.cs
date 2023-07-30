using System.Collections.Immutable;

namespace AutoKkutuLib.Database.Sql.Migrations;
internal static class MigrationRegistry
{
	internal static IImmutableList<IMigration> GetMigrations(DbConnectionBase connection)
	{
		var builder = ImmutableList.CreateBuilder<IMigration>();
		builder.Add(new AddWordListKkutuWordIndexColumn(connection));
		builder.Add(new AddWordListReverseWordIndexColumn(connection));
		builder.Add(new AddWordListSequenceColumn(connection));
		builder.Add(new ChangeWordListKkutuWordIndexColumnType(connection));
		builder.Add(new ConvertWordListEndWordColumnToFlags(connection));
		builder.Add(new AddWordListChoseongColumn(connection));
		builder.Add(new AddWordListMeaningColumn(connection));
		builder.Add(new AddWordListTypeColumn(connection));
		builder.Add(new AddWordListThemeColumns(connection));
		builder.Add(new EnlargeWordListFlagsColumnType(connection));

		builder.Sort(new MigrationComparer());
		return builder.ToImmutable();
	}

	internal static bool RunMigrations(DbConnectionBase connection)
	{
		var dbModified = false;
		foreach (var migration in from migration in GetMigrations(connection) where CheckMigrationCondition(migration) select migration)
		{
			LibLogger.Info(nameof(MigrationRegistry), "Running migration: {migName} (Date: {migDate})", migration.Name, migration.Date);
			try
			{
				migration.Execute();
				LibLogger.Info(nameof(MigrationRegistry), "Finished migration: {migName} (Date: {migDate})", migration.Name, migration.Date);
			}
			catch (Exception ex)
			{
				LibLogger.Error(nameof(MigrationRegistry), ex, "Failed to execute migration: {migName}", migration.Name);
			}

			dbModified = true;
		}

		return dbModified;
	}

	internal static bool CheckMigrationCondition(IMigration migration)
	{
		try
		{
			return migration.ConditionMet();
		}
		catch (Exception ex)
		{
			LibLogger.Error(nameof(MigrationRegistry), ex, "Failed to check migration condition: {migName}", migration.Name);
		}
		return false;
	}

	internal class MigrationComparer : IComparer<IMigration>
	{
		public int Compare(IMigration? x, IMigration? y) => DateTime.Compare(x!.Date, y!.Date);
	}
}
