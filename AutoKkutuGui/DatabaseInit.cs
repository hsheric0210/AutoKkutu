using AutoKkutuGui.ConfigFile;
using AutoKkutuLib.Database;
using AutoKkutuLib.Database.MySql;
using AutoKkutuLib.Database.PostgreSql;
using AutoKkutuLib.Database.Sqlite;
using Serilog;
using System;

namespace AutoKkutuGui;

public static class DatabaseUtils
{
	public static AbstractDatabase CreateDatabase(System.Configuration.Configuration config)
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));

		switch (((DatabaseTypeSection)config.GetSection("dbtype")).Type.ToUpperInvariant())
		{
			case "MARIADB":
			case "MYSQL":
				var mysqlConnectionString = ((MySqlSection)config.GetSection("mysql")).ConnectionString;
				Log.Information("MySQL selected: {connString}", mysqlConnectionString);
				return new MySqlDatabase(mysqlConnectionString);

			case "POSTGRESQL":
			case "POSTGRES":
			case "POSTGRE":
			case "PGSQL":
				var pgsqlConnectionString = ((PostgreSqlSection)config.GetSection("postgresql")).ConnectionString;
				Log.Information("PostgreSQL selected: {connString}", pgsqlConnectionString);
				return new PostgreSqlDatabase(pgsqlConnectionString);
		}

		var file = ((SqliteSection)config.GetSection("sqlite")).File;
		Log.Information("SQLite selected: File={file}", file);
		return new SqliteDatabase(file);
	}
}
