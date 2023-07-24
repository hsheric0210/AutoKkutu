using AutoKkutuLib.Database;
using AutoKkutuLib.Database.MySql;
using AutoKkutuLib.Database.PostgreSql;
using AutoKkutuLib.Database.Sqlite;

namespace AutoKkutuGui;

public static class DatabaseInit
{
	public static DbConnectionBase? Connect(string type, string connString)
	{
		switch (type.ToUpperInvariant())
		{
			case "MARIADB":
			case "MYSQL":
				return MySqlDbConnection.Create(connString);

			case "POSTGRESQL":
			case "POSTGRES":
			case "POSTGRE":
			case "PGSQL":
				return PostgreSqlDbConnection.Create(connString);
		}

		return SqliteDbConnection.Create(connString);
	}
}
