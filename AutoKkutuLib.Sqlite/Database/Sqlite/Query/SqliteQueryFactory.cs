using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql;
using AutoKkutuLib.Database.Sql.Query;
using Dapper;

namespace AutoKkutuLib.Postgres.Database.PostgreSql.Query;
internal class SqliteQueryFactory : QueryFactory
{
	public SqliteQueryFactory(AbstractDatabaseConnection connection) : base(connection) { }

	public override AbstractAddWordListSequenceColumnQuery AddWordListSequenceColumn() => new SqliteAddWordListSequenceColumnQuery(Connection);
	public override AbstractChangeWordListColumnTypeQuery ChangeWordListColumnType(string tableName, string columnName, string newType) => new SqliteChangeWordListColumnTypeQuery(Connection, tableName, columnName, newType);
	public override AbstractDropWordListColumnQuery DropWordListColumn(string columnName) => new SqliteDropWordListColumnQuery(Connection, columnName);
	public override AbstractGetColumnTypeQuery GetColumnType(string tableName, string columnName) => new SqliteGetColumnTypeQuery(Connection, tableName, columnName);
	public override AbstractIsColumnExistsQuery IsColumnExists(string tableName, string columnName) => new SqliteIsColumnExistsQuery(Connection, tableName, columnName);
	public override AbstractIsTableExistsQuery IsTableExists(string tableName) => new SqliteIsTableExistsQuery(Connection, tableName);

	private void RebuildWordList()
	{
		Connection.Execute($"ALTER TABLE {DatabaseConstants.WordTableName} RENAME TO _{DatabaseConstants.WordTableName};");
		Connection.MakeTable(DatabaseConstants.WordTableName);
		Connection.Execute($"INSERT INTO {DatabaseConstants.WordTableName} (word, word_index, reverse_word_index, kkutu_index, flags) SELECT word, word_index, reverse_word_index, kkutu_index, flags FROM _{DatabaseConstants.WordTableName};");
		Connection.Execute($"DROP TABLE _{DatabaseConstants.WordTableName};");
	}
}