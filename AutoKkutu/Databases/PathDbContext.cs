using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;

namespace AutoKkutu.Databases
{
	public sealed class PathDbContext : DbContext
	{
		public DatabaseProvider ProviderType
		{
			get;
		}

		public string ConnectionString
		{
			get;
		}

		public DbSet<WordModel> Word
		{
			get; set;
		}

		public DbSet<WordIndexModel> AttackWordIndex
		{
			get; set;
		}
		public DbSet<WordIndexModel> EndWordIndex
		{
			get; set;
		}

		public DbSet<WordIndexModel> ReverseAttackWordIndex
		{
			get; set;
		}
		public DbSet<WordIndexModel> ReverseEndWordIndex
		{
			get; set;
		}

		public DbSet<WordIndexModel> KkutuAttackWordIndex
		{
			get; set;
		}
		public DbSet<WordIndexModel> KkutuEndWordIndex
		{
			get; set;
		}

		public PathDbContext(DatabaseProvider providerType, string connectionString)
		{
			ProviderType = providerType;
			ConnectionString = connectionString;
		}

		public int WordPriority(int flags, int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal) => throw new NotSupportedException("This method will be remapped by EF Core");

		public int MissionWordPriority(string word, int flags, string missionWord, int endWordFlag, int attackWordFlag, int endMissionWordOrdinal, int endWordOrdinal, int attackMissionWordOrdinal, int attackWordOrdinal, int missionWordOrdinal, int normalWordOrdinal) => throw new NotSupportedException("This method will be remapped by EF Core");

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			switch (ProviderType)
			{
				case DatabaseProvider.Sqlite:
					optionsBuilder.UseSqlite(ConnectionString);
					break;
				case DatabaseProvider.PostgreSql:
					optionsBuilder.UseNpgsql(ConnectionString);
					break;
				case DatabaseProvider.MySql:
					optionsBuilder.UseMySQL(ConnectionString);
					break;
			}
		}

		public void ImplementRequiredFunctions()
		{
			switch (ProviderType)
			{
				case DatabaseProvider.Sqlite:
					ImplementRequiredFunctionsSqlite();
					break;
				case DatabaseProvider.PostgreSql:
					ImplementRequiredFunctionsPostgreSql();
					break;
				case DatabaseProvider.MySql:
					ImplementRequiredFunctionsMySql();
					break;
			}
		}

		private void ImplementRequiredFunctionsSqlite()
		{
			if (Database.GetDbConnection() is not SqliteConnection conn)
				return;

			conn.CreateFunction("WordPriority", (int flags, int endWordFlag, int attackWordFlag, int endWordOrdinal, int attackWordOrdinal, int normalWordOrdinal) =>
			{
				if ((flags & endWordFlag) != 0)
					return endWordOrdinal * DatabaseConstants.MaxWordLength;
				if ((flags & attackWordFlag) != 0)
					return attackWordOrdinal * DatabaseConstants.MaxWordLength;
				return normalWordOrdinal * DatabaseConstants.MaxWordLength;
			});

			conn.CreateFunction("MissionWordPriority", (string word, int flags, string missionWord, int endWordFlag, int attackWordFlag, int endMissionWordOrdinal, int endWordOrdinal, int attackMissionWordOrdinal, int attackWordOrdinal, int missionWordOrdinal, int normalWordOrdinal) =>
			{
				char missionChar = char.ToUpperInvariant(missionWord[0]);
				int missionOccurrence = (from char c in word.ToUpperInvariant() where c == missionChar select c).Count();
				bool hasMission = missionOccurrence > 0;

				if ((flags & endWordFlag) != 0)
					return (hasMission ? endMissionWordOrdinal : endWordOrdinal) * DatabaseConstants.MaxWordPriority + missionOccurrence * 256;
				if ((flags & attackWordFlag) != 0)
					return (hasMission ? attackMissionWordOrdinal : attackWordOrdinal) * DatabaseConstants.MaxWordPriority + missionOccurrence * 256;
				return (hasMission ? missionWordOrdinal : normalWordOrdinal) * DatabaseConstants.MaxWordPriority + missionOccurrence * 256;
			});
		}

		private void ImplementRequiredFunctionsPostgreSql()
		{
			if (!Database.IsNpgsql())
				return;

			Database.ExecuteSql($@"CREATE OR REPLACE FUNCTION WordPriority(flags INT, endWordFlag INT, attackWordFlag INT, endWordOrdinal INT, attackWordOrdinal INT, normalWordOrdinal INT)
RETURNS INTEGER AS $$
BEGIN
	IF ((flags & endWordFlag) != 0) THEN
		RETURN endWordOrdinal * {DatabaseConstants.MaxWordLength};
	END IF;
	IF ((flags & attackWordFlag) != 0) THEN
		RETURN attackWordOrdinal * {DatabaseConstants.MaxWordLength};
	END IF;
	RETURN normalWordOrdinal * {DatabaseConstants.MaxWordLength};
END;
$$ LANGUAGE plpgsql
");

			Database.ExecuteSql($@"CREATE OR REPLACE FUNCTION MissionWordPriority(word VARCHAR, flags INT, missionword VARCHAR, endWordFlag INT, attackWordFlag INT, endMissionWordOrdinal INT, endWordOrdinal INT, attackMissionWordOrdinal INT, attackWordOrdinal INT, missionWordOrdinal INT, normalWordOrdinal INT)
RETURNS INTEGER AS $$
DECLARE
	occurrence INTEGER;
BEGIN
	occurrence := ROUND((LENGTH(word) - LENGTH(REPLACE(LOWER(word), LOWER(missionWord), ''))) / LENGTH(missionWord));

	IF ((flags & endWordFlag) != 0) THEN
		IF (occurrence > 0) THEN
			RETURN endMissionWordOrdinal * {DatabaseConstants.MaxWordPriority} + occurrence * 256;
		ELSE
			RETURN endWordOrdinal * {DatabaseConstants.MaxWordPriority};
		END IF;
	END IF;
	IF ((flags & attackWordFlag) != 0) THEN
		IF (occurrence > 0) THEN
			RETURN attackMissionWordOrdinal * {DatabaseConstants.MaxWordPriority} + occurrence * 256;
		ELSE
			RETURN attackWordOrdinal * {DatabaseConstants.MaxWordPriority};
		END IF;
	END IF;

	IF occurrence > 0 THEN
		RETURN missionWordOrdinal * {DatabaseConstants.MaxWordPriority} + occurrence * 256;
	ELSE
		RETURN normalWordOrdinal * {DatabaseConstants.MaxWordPriority};
	END IF;
END;
$$ LANGUAGE plpgsql
");
		}

		private void ImplementRequiredFunctionsMySql()
		{
			// if (!Database.IsMySql()) // I hope you use MySQL! (No validation)
			//	return;

			Database.ExecuteSqlRaw("DROP FUNCTION IF EXISTS WordPriority;");
			Database.ExecuteSql($@"CREATE FUNCTION WordPriority(flags INT, endWordFlag INT, attackWordFlag INT, endWordOrdinal INT, attackWordOrdinal INT, normalWordOrdinal INT) RETURNS INT
DETERMINISTIC
NO SQL
BEGIN
	IF (flags & endWordFlag) != 0 THEN
		RETURN endWordOrdinal * {DatabaseConstants.MaxWordLength};
	END IF;
	IF (flags & attackWordFlag) != 0 THEN
		RETURN attackWordOrdinal * {DatabaseConstants.MaxWordLength};
	END IF;
	RETURN normalWordOrdinal * {DatabaseConstants.MaxWordLength};
END;
");

			Database.ExecuteSqlRaw("DROP FUNCTION IF EXISTS MissionWordPriority;");
			Database.ExecuteSql($@"CREATE FUNCTION MissionWordPriority(word VARCHAR(256), flags INT, missionword VARCHAR(2), endWordFlag INT, attackWordFlag INT, endMissionWordOrdinal INT, endWordOrdinal INT, attackMissionWordOrdinal INT, attackWordOrdinal INT, missionWordOrdinal INT, normalWordOrdinal INT) RETURNS INT
DETERMINISTIC
NO SQL
BEGIN
	DECLARE occurrence INT;

	SET occurrence = ROUND((LENGTH(word) - LENGTH(REPLACE(LOWER(word), LOWER(missionWord), ''))) / LENGTH(missionWord));
	IF (flags & endWordFlag) != 0 THEN
		IF occurrence > 0 THEN
			RETURN endMissionWordOrdinal * {DatabaseConstants.MaxWordPriority} + occurrence * 256;
		ELSE
			RETURN endWordOrdinal * {DatabaseConstants.MaxWordPriority};
		END IF;
	END IF;
	IF (flags & attackWordFlag) != 0 THEN
		IF occurrence > 0 THEN
			RETURN attackMissionWordOrdinal * {DatabaseConstants.MaxWordPriority} + occurrence * 256;
		ELSE
			RETURN attackWordOrdinal * {DatabaseConstants.MaxWordPriority};
		END IF;
	END IF;

	IF occurrence > 0 THEN
		RETURN missionWordOrdinal * {DatabaseConstants.MaxWordPriority} + occurrence * 256;
	ELSE
		RETURN normalWordOrdinal * {DatabaseConstants.MaxWordPriority};
	END IF;
END;
");
		}
	}
}
