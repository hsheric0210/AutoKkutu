using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql.Query;

namespace AutoKkutuLib.MySql.Database.MySql.Query;
public class MySqlQueryFactory : QueryFactory
{
	private readonly string dbName;

	public MySqlQueryFactory(DbConnectionBase connection, string dbName) : base(connection) => this.dbName = dbName;

	public override AddWordListSequenceColumnQueryBase AddWordListSequenceColumn() => new MySqlAddWordListSequenceColumnQuery(Db);
	public override ChangeWordListColumnTypeQueryBase ChangeWordListColumnType(string tableName, string columnName, string newType) => new MySqlChangeWordListColumnTypeQuery(Db, tableName, columnName, newType);
	public override DropWordListColumnQueryBase DropWordListColumn(string columnName) => new MySqlDropWordListColumnQuery(Db, columnName);
	public override GetColumnTypeQueryBase GetColumnType(string tableName, string columnName) => new MySqlGetColumnTypeQuery(Db, dbName, tableName, columnName);
	public override IsColumnExistQueryBase IsColumnExists(string tableName, string columnName) => new MySqlIsColumnExistsQuery(Db, dbName, tableName, columnName);
	public override IsTableExistQueryBase IsTableExists(string tableName) => new MySqlIsTableExistsQuery(Db, dbName, tableName);

	public override VacuumQuery Vacuum() => new MySqlVacuumQuery(Db);
}