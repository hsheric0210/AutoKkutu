using AutoKkutu.ConfigFile;
using AutoKkutu.Constants;
using AutoKkutu.Database;
using AutoKkutu.Database.MySQL;
using AutoKkutu.Database.PostgreSQL;
using AutoKkutu.Database.SQLite;
using AutoKkutu.Modules.PathManager;
using AutoKkutu.Utils.Extension;
using Serilog;
using System;
using System.Configuration;

namespace AutoKkutu.Utils
{
	public static class DatabaseUtils
	{
		public static AbstractDatabase CreateDatabase(Configuration config)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));

			switch (((DatabaseTypeSection)config.GetSection("dbtype")).Type.ToUpperInvariant())
			{
				case "MARIADB":
				case "MYSQL":
					string mysqlConnectionString = ((MySQLSection)config.GetSection("mysql")).ConnectionString;
					Log.Information("MySQL selected: {connString}", mysqlConnectionString);
					return new MySqlDatabase(mysqlConnectionString);

				case "POSTGRESQL":
				case "POSTGRES":
				case "POSTGRE":
				case "PGSQL":
					string pgsqlConnectionString = ((PostgreSQLSection)config.GetSection("postgresql")).ConnectionString;
					Log.Information("PostgreSQL selected: {connString}", pgsqlConnectionString);
					return new PostgreSqlDatabase(pgsqlConnectionString);
			}

			string file = ((SQLiteSection)config.GetSection("sqlite")).File;
			Log.Information("SQLite selected: File={file}", file);
			return new SqliteDatabase(file);
		}
	}
}
