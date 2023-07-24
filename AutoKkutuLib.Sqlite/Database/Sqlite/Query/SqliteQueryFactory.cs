using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql.Query;

namespace AutoKkutuLib.Postgres.Database.PostgreSql.Query;
internal class SqliteQueryFactory : QueryFactory
{
	public SqliteQueryFactory(DbConnectionBase connection) : base(connection) { }

	public override AddWordListSequenceColumnQueryBase AddWordListSequenceColumn() => new SqliteAddWordListSequenceColumnQuery(Db);
	public override ChangeWordListColumnTypeQueryBase ChangeWordListColumnType(string tableName, string columnName, string newType) => new SqliteChangeWordListColumnTypeQuery(Db, tableName, columnName, newType);
	public override DropWordListColumnQueryBase DropWordListColumn(string columnName) => new SqliteDropWordListColumnQuery(Db, columnName);
	public override GetColumnTypeQueryBase GetColumnType(string tableName, string columnName) => new SqliteGetColumnTypeQuery(Db, tableName, columnName);
	public override IsColumnExistQueryBase IsColumnExists(string tableName, string columnName) => new SqliteIsColumnExistsQuery(Db, tableName, columnName);
	public override IsTableExistQueryBase IsTableExists(string tableName) => new SqliteIsTableExistsQuery(Db, tableName);
}