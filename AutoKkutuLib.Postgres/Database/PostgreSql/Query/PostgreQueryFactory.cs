using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql.Query;

namespace AutoKkutuLib.Postgres.Database.PostgreSql.Query;
internal class PostgreQueryFactory : QueryFactory
{
	public PostgreQueryFactory(AbstractDatabaseConnection connection) : base(connection) { }

	public override AbstractAddWordListSequenceColumnQuery AddWordListSequenceColumn() => new PostgreAddWordListSequenceColumnQuery(Connection);
	public override AbstractChangeWordListColumnTypeQuery ChangeWordListColumnType(string tableName, string columnName, string newType) => new PostgreChangeWordListColumnTypeQuery(Connection, tableName, columnName, newType);
	public override AbstractDropWordListColumnQuery DropWordListColumn(string columnName) => new PostgreDropWordListColumnQuery(Connection, columnName);
	public override AbstractGetColumnTypeQuery GetColumnType(string tableName, string columnName) => new PostgreGetColumnTypeQuery(Connection, tableName, columnName);
	public override AbstractIsColumnExistsQuery IsColumnExists(string tableName, string columnName) => new PostgreIsColumnExistsQuery(Connection, tableName, columnName);
	public override AbstractIsTableExistsQuery IsTableExists(string tableName) => new PostgreIsTableExistsQuery(Connection, tableName);
}