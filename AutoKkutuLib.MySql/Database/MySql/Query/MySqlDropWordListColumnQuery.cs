﻿using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class MySqlDropWordListColumnQuery : AbstractDropWordListColumnQuery
{
	internal MySqlDropWordListColumnQuery(AbstractDatabaseConnection connection, string columnName) : base(connection, columnName) { }

	// Inevitable dynamically-formatted SQL: The column name could't be parameterized
	public override int Execute() => Connection.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} DROP {ColumnName}");
}
