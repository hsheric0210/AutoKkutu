using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql.Query;

namespace AutoKkutuLib.Postgres.Database.PostgreSql.Query;
internal class PostgreSqlQueryFactory : QueryFactory
{
	public PostgreSqlQueryFactory(DbConnectionBase connection) : base(connection) { }

	public override AbstractAddWordListSequenceColumnQuery AddWordListSequenceColumn() => new PostgreSqlAddWordListSequenceColumnQuery(Connection);
	public override AbstractChangeWordListColumnTypeQuery ChangeWordListColumnType(string tableName, string columnName, string newType) => new PostgreSqlChangeWordListColumnTypeQuery(Connection, tableName, columnName, newType);
	public override AbstractDropWordListColumnQuery DropWordListColumn(string columnName) => new PostgreSqlDropWordListColumnQuery(Connection, columnName);
	public override AbstractGetColumnTypeQuery GetColumnType(string tableName, string columnName) => new PostgreSqlGetColumnTypeQuery(Connection, tableName, columnName);
	public override AbstractIsColumnExistsQuery IsColumnExists(string tableName, string columnName) => new PostgreSqlIsColumnExistsQuery(Connection, tableName, columnName);
	public override AbstractIsTableExistsQuery IsTableExists(string tableName) => new PostgreSqlIsTableExistsQuery(Connection, tableName);

	public override VacuumQuery Vacuum() => new PostgreSqlVacuumFullQuery(Connection);
}