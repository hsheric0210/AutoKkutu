using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql.Query;

namespace AutoKkutuLib.Postgres.Database.PostgreSql.Query;
internal class PostgreSqlQueryFactory : QueryFactory
{
	public PostgreSqlQueryFactory(DbConnectionBase connection) : base(connection) { }

	public override AddWordListSequenceColumnQueryBase AddWordListSequenceColumn() => new PostgreSqlAddWordListSequenceColumnQuery(Db);
	public override ChangeWordListColumnTypeQueryBase ChangeWordListColumnType(string tableName, string columnName, string newType) => new PostgreSqlChangeWordListColumnTypeQuery(Db, tableName, columnName, newType);
	public override DropWordListColumnQueryBase DropWordListColumn(string columnName) => new PostgreSqlDropWordListColumnQuery(Db, columnName);
	public override GetColumnTypeQueryBase GetColumnType(string tableName, string columnName) => new PostgreSqlGetColumnTypeQuery(Db, tableName, columnName);
	public override IsColumnExistQueryBase IsColumnExists(string tableName, string columnName) => new PostgreSqlIsColumnExistsQuery(Db, tableName, columnName);
	public override IsTableExistQueryBase IsTableExists(string tableName) => new PostgreSqlIsTableExistsQuery(Db, tableName);

	public override VacuumQuery Vacuum() => new PostgreSqlVacuumFullQuery(Db);
}