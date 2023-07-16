using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql.Query;

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
}