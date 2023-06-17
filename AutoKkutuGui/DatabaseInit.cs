using AutoKkutuGui.ConfigFile;
using AutoKkutuLib.Database;
using AutoKkutuLib.Database.MySql;
using AutoKkutuLib.Database.PostgreSql;
using AutoKkutuLib.Database.Sqlite;
using Serilog;
using System;

namespace AutoKkutuGui;

public static class DatabaseInit
{
	public static AbstractDatabaseConnection? Connect(System.Configuration.Configuration config)
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));

		switch (((DatabaseTypeSection)config.GetSection("dbtype")).Type.ToUpperInvariant())
		{
			case "MARIADB":
			case "MYSQL":
				var mysqlConnectionString = ((MySqlSection)config.GetSection("mysql")).ConnectionString;
				return MySqlDatabaseConnection.Create(mysqlConnectionString);

			case "POSTGRESQL":
			case "POSTGRES":
			case "POSTGRE":
			case "PGSQL":
				var pgsqlConnectionString = ((PostgreSqlSection)config.GetSection("postgresql")).ConnectionString;
				return PostgreSqlDatabaseConnection.Create(pgsqlConnectionString);
		}

		var file = ((SqliteSection)config.GetSection("sqlite")).File;
		return SqliteDatabaseConnection.Create(file);
	}
}
