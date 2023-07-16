using AutoKkutuGui.ConfigFile;
using AutoKkutuLib.Database;
using AutoKkutuLib.Database.MySql;
using AutoKkutuLib.Database.PostgreSql;
using AutoKkutuLib.Database.Sqlite;
using System;

namespace AutoKkutuGui;

public static class DatabaseInit
{
	public static AbstractDatabaseConnection? Connect(string type, string connString)
	{
		switch (type.ToUpperInvariant())
		{
			case "MARIADB":
			case "MYSQL":
				return MySqlDatabaseConnection.Create(connString);

			case "POSTGRESQL":
			case "POSTGRES":
			case "POSTGRE":
			case "PGSQL":
				return PostgreSqlDatabaseConnection.Create(connString);
		}

		return SqliteDatabaseConnection.Create(connString);
	}
}
