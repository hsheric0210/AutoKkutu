﻿using AutoKkutuLib.Database.Sql.Migrations;
using Dapper;

namespace AutoKkutuLib.Database.Sql;

public static class TableExtension
{
	public static void CheckTable(this DbConnectionBase connection)
	{
		if (connection == null)
			throw new ArgumentNullException(nameof(connection));

		// Create node tables
		foreach (var tableName in new string[] { DatabaseConstants.EndNodeIndexTableName, DatabaseConstants.AttackNodeIndexTableName, DatabaseConstants.ReverseEndNodeIndexTableName, DatabaseConstants.ReverseAttackNodeIndexTableName, DatabaseConstants.KkutuEndNodeIndexTableName, DatabaseConstants.KkutuAttackNodeIndexTableName })
			connection.MakeTableIfNotExists(tableName);

		connection.MakeTableIfNotExists(DatabaseConstants.KKTEndNodeIndexTableName, () => connection.Execute($"INSERT INTO {DatabaseConstants.KKTEndNodeIndexTableName} SELECT * FROM {DatabaseConstants.EndNodeIndexTableName};"));
		connection.MakeTableIfNotExists(DatabaseConstants.KKTAttackNodeIndexTableName, () => connection.Execute($"INSERT INTO {DatabaseConstants.KKTAttackNodeIndexTableName} SELECT * FROM {DatabaseConstants.AttackNodeIndexTableName};"));

		// Create word list table
		if (!connection.Query.IsTableExists(DatabaseConstants.WordTableName).Execute())
			connection.MakeTable(DatabaseConstants.WordTableName);

		if (MigrationRegistry.RunMigrations(connection))
			connection.Query.Vacuum().Execute();

		// Create indexes
		foreach (var columnName in new string[] { DatabaseConstants.WordIndexColumnName, DatabaseConstants.ReverseWordIndexColumnName, DatabaseConstants.KkutuWordIndexColumnName })
			connection.Query.CreateIndex(DatabaseConstants.WordTableName, columnName).Execute();
	}

	public static void MakeTable(this DbConnectionBase connection, string tablename)
	{
		if (connection == null)
			throw new ArgumentNullException(nameof(connection));

		var columnOptions = tablename switch
		{
			DatabaseConstants.WordTableName => connection.GetWordListColumnOptions(),
			DatabaseConstants.KkutuEndNodeIndexTableName => $"{DatabaseConstants.WordIndexColumnName} VARCHAR(2) NOT NULL",
			_ => $"{DatabaseConstants.WordIndexColumnName} CHAR(1) NOT NULL",
		};
		connection.Execute($"CREATE TABLE {tablename} ({columnOptions});");
	}

	public static void MakeTableIfNotExists(this DbConnectionBase connection, string tableName, Action? callback = null)
	{
		if (connection == null)
			throw new ArgumentNullException(nameof(connection));

		if (!connection.Query.IsTableExists(tableName).Execute())
		{
			connection.MakeTable(tableName);
			callback?.Invoke();
		}
	}
}
