using log4net;
using System;

namespace AutoKkutu.Databases.Extension
{
	public static class BackwardCompatibilityExtension
	{
		private static readonly ILog Logger = LogManager.GetLogger("Database Backward-Compatibility");

		private static void AddInexistentColumns(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (!connection.IsColumnExists(DatabaseConstants.WordListTableName, DatabaseConstants.ReverseWordIndexColumnName))
			{
				using (CommonDatabaseCommand command = connection.CreateCommand($"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.ReverseWordIndexColumnName} CHAR(1) NOT NULL DEFAULT ' '"))
					command.TryExecuteNonQuery($"add {DatabaseConstants.ReverseWordIndexColumnName}");
				Logger.Warn($"Added {DatabaseConstants.ReverseWordIndexColumnName} column");
			}

			if (!connection.IsColumnExists(DatabaseConstants.WordListTableName, DatabaseConstants.KkutuWordIndexColumnName))
			{
				using (CommonDatabaseCommand command = connection.CreateCommand($"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.KkutuWordIndexColumnName} CHAR(2) NOT NULL DEFAULT ' '"))
					command.TryExecuteNonQuery($"add {DatabaseConstants.KkutuWordIndexColumnName}");
				Logger.Warn($"Added {DatabaseConstants.KkutuWordIndexColumnName} column");
			}
		}

		private static bool AddSequenceColumn(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (!connection.IsColumnExists(DatabaseConstants.WordListTableName, DatabaseConstants.SequenceColumnName))
			{
				try
				{
					connection.AddSequenceColumnToWordList();
					Logger.Warn("Added sequence column");
					return true;
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to add sequence column", ex);
				}
			}

			return false;
		}

		public static void CheckBackwardCompatibility(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			bool needToCleanUp = false;

			connection.AddInexistentColumns();
			needToCleanUp |= connection.DropIsEndWordColumn();
			needToCleanUp |= connection.AddSequenceColumn();
			needToCleanUp |= connection.UpdateKkutuIndexDataType();

			if (needToCleanUp)
			{
				Logger.Warn("Executing vacuum...");
				connection.PerformVacuum();
			}
		}

		private static bool DropIsEndWordColumn(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (connection.IsColumnExists(DatabaseConstants.WordListTableName, DatabaseConstants.IsEndwordColumnName))
			{
				try
				{
					if (!connection.IsColumnExists(DatabaseConstants.WordListTableName, DatabaseConstants.FlagsColumnName))
					{
						using (CommonDatabaseCommand command = connection.CreateCommand($"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.FlagsColumnName} SMALLINT NOT NULL DEFAULT 0"))
							command.ExecuteNonQuery();
						using (CommonDatabaseCommand command = connection.CreateCommand($"UPDATE {DatabaseConstants.WordListTableName} SET {DatabaseConstants.FlagsColumnName} = CAST({DatabaseConstants.IsEndwordColumnName} AS SMALLINT)"))
							command.ExecuteNonQuery();
						Logger.Warn($"Converted '{DatabaseConstants.IsEndwordColumnName}' into {DatabaseConstants.FlagsColumnName} column.");
					}

					connection.DropWordListColumn(DatabaseConstants.IsEndwordColumnName);
					Logger.Warn($"Dropped {DatabaseConstants.IsEndwordColumnName} column as it is no longer used.");
					return true;
				}
				catch (Exception ex)
				{
					Logger.Error($"Failed to add {DatabaseConstants.FlagsColumnName} column", ex);
				}
			}

			return false;
		}

		private static bool UpdateKkutuIndexDataType(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			string? kkutuindextype = connection.GetColumnType(DatabaseConstants.WordListTableName, DatabaseConstants.KkutuWordIndexColumnName);
			if (kkutuindextype != null && (kkutuindextype.Equals("CHAR(2)", StringComparison.OrdinalIgnoreCase) || kkutuindextype.Equals("character", StringComparison.OrdinalIgnoreCase)))
			{
				connection.ChangeWordListColumnType(DatabaseConstants.WordListTableName, DatabaseConstants.KkutuWordIndexColumnName, newType: "VARCHAR(2)");
				Logger.WarnFormat("Changed type of '{0}' from CHAR(2) to VARCHAR(2)", DatabaseConstants.KkutuWordIndexColumnName);
				return true;
			}

			return false;
		}
	}
}
