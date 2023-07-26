namespace AutoKkutuLib.Database.Sql.Query;
public abstract class QueryFactory
{
	protected DbConnectionBase Db { get; }

	protected QueryFactory(DbConnectionBase db) => Db = db;

	#region Abstract methods
	public abstract AddWordListSequenceColumnQueryBase AddWordListSequenceColumn();
	public abstract DropWordListColumnQueryBase DropWordListColumn(string columnName);
	public abstract GetColumnTypeQueryBase GetColumnType(string tableName, string columnName);
	public abstract IsColumnExistQueryBase IsColumnExists(string tableName, string columnName);
	public abstract IsTableExistQueryBase IsTableExists(string tableName);
	public abstract ChangeWordListColumnTypeQueryBase ChangeWordListColumnType(string tableName, string columnName, string newType);
	#endregion

	#region Abstract method redirects
	public IsTableExistQueryBase IsTableExists(NodeTypes nodeType) => IsTableExists(nodeType.ToNodeTableName());
	#endregion

	#region Virtual(overridable) methods
	public virtual DeduplicationQuery Deduplicate() => new(Db);
	public virtual FindWordQuery FindWord(GameMode gameMode, WordPreference wordPreference) => new(Db, gameMode, wordPreference);
	public virtual IndexCreationQuery CreateIndex(string tableName, string columnName) => new(Db, tableName, columnName);
	public virtual NodeAdditionQuery AddNode(string tableName) => new(Db, tableName);
	public virtual NodeAdditionQuery AddNode(NodeTypes nodeType) => new(Db, nodeType.ToNodeTableName());
	public virtual NodeDeletionQuery DeleteNode(string tableName) => new(Db, tableName);
	public virtual NodeDeletionQuery DeleteNode(NodeTypes nodeType) => new(Db, nodeType.ToNodeTableName());
	public virtual NodeListQuery ListNode() => new(Db);
	public virtual VacuumQuery Vacuum() => new(Db);
	public virtual WordAdditionQuery AddWord() => new(Db);
	public virtual WordDeletionQuery DeleteWord() => new(Db);
	public virtual AddColumnQuery AddColumn(string tableName, string columnName, string columnType) => new(Db, tableName, columnName, columnType);
	public virtual CreateTableQuery CreateTable(string tableName, string columns) => new(Db, tableName, columns);
	#endregion
}
