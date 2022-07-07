using NLog;
using System;

namespace AutoKkutu.Databases.Extension
{
	public static class BackwardCompatibilityExtension
	{
		private static readonly Logger Logger = LogManager.GetLogger("Database Backward-Compatibility");

		private static void AddInexistentColumns(this CommonDatabaseConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (!connection.IsColumnExists(DatabaseConstants.WordListTableName, DatabaseConstants.ReverseWordIndexColumnName))
			{
				connection.TryExecuteNonQuery($"add {DatabaseConstants.ReverseWordIndexColumnName}", $"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.ReverseWordIndexColumnName} CHAR(1) NOT NULL DEFAULT ' '");
				Logger.Warn($"Added {DatabaseConstants.ReverseWordIndexColumnName} column.");
			}

			if (!connection.IsColumnExists(DatabaseConstants.WordListTableName, DatabaseConstants.KkutuWordIndexColumnName))
			{
				connection.TryExecuteNonQuery($"add {DatabaseConstants.KkutuWordIndexColumnName}", $"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.KkutuWordIndexColumnName} CHAR(2) NOT NULL DEFAULT ' '");
				Logger.Warn($"Added {DatabaseConstants.KkutuWordIndexColumnName} column.");
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
					Logger.Warn("Added sequence column.");
					return true;
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Failed to add sequence column.");
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
						connection.ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN {DatabaseConstants.FlagsColumnName} SMALLINT NOT NULL DEFAULT 0");
						connection.ExecuteNonQuery($"UPDATE {DatabaseConstants.WordListTableName} SET {DatabaseConstants.FlagsColumnName} = CAST({DatabaseConstants.IsEndwordColumnName} AS SMALLINT)");
						Logger.Warn($"Converted '{DatabaseConstants.IsEndwordColumnName}' into {DatabaseConstants.FlagsColumnName} column.");
					}

					connection.DropWordListColumn(DatabaseConstants.IsEndwordColumnName);
					Logger.Warn($"Dropped {DatabaseConstants.IsEndwordColumnName} column as it is no longer used.");
					return true;
				}
				catch (Exception ex)
				{
					Logger.Error(ex, $"Failed to add {DatabaseConstants.FlagsColumnName} column.");
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
				Logger.Warn($"Changed type of '{DatabaseConstants.KkutuWordIndexColumnName}' from CHAR(2) to VARCHAR(2).");
				return true;
			}

			return false;
		}
	}
}
