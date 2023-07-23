using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Sql.Query;

namespace AutoKkutuLib.MySql.Database.MySql.Query;
public class MySqlQueryFactory : QueryFactory
{
	private readonly string dbName;

	public MySqlQueryFactory(DbConnectionBase connection, string dbName) : base(connection) => this.dbName = dbName;

	public override AbstractAddWordListSequenceColumnQuery AddWordListSequenceColumn() => new MySqlAddWordListSequenceColumnQuery(Connection);
	public override AbstractChangeWordListColumnTypeQuery ChangeWordListColumnType(string tableName, string columnName, string newType) => new MySqlChangeWordListColumnTypeQuery(Connection, tableName, columnName, newType);
	public override AbstractDropWordListColumnQuery DropWordListColumn(string columnName) => new MySqlDropWordListColumnQuery(Connection, columnName);
	public override AbstractGetColumnTypeQuery GetColumnType(string tableName, string columnName) => new MySqlGetColumnTypeQuery(Connection, dbName, tableName, columnName);
	public override AbstractIsColumnExistsQuery IsColumnExists(string tableName, string columnName) => new MySqlIsColumnExistsQuery(Connection, dbName, tableName, columnName);
	public override AbstractIsTableExistsQuery IsTableExists(string tableName) => new MySqlIsTableExistsQuery(Connection, dbName, tableName);

	public override VacuumQuery Vacuum() => new MySqlVacuumQuery(Connection);
}