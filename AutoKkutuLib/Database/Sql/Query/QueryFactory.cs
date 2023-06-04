namespace AutoKkutuLib.Database.Sql.Query;
public abstract class QueryFactory
{
	protected AbstractDatabaseConnection Connection { get; }

	protected QueryFactory(AbstractDatabaseConnection connection) => Connection = connection;

	#region Abstract methods
	public abstract AbstractAddWordListSequenceColumnQuery AddWordListSequenceColumn();
	public abstract AbstractDropWordListColumnQuery DropWordListColumn(string columnName);
	public abstract AbstractGetColumnTypeQuery GetColumnType(string tableName, string columnName);
	public abstract AbstractIsColumnExistsQuery IsColumnExists(string tableName, string columnName);
	public abstract AbstractIsTableExistsQuery IsTableExists(string tableName);
	public abstract AbstractChangeWordListColumnTypeQuery ChangeWordListColumnType(string tableName, string columnName, string newType);
	#endregion

	#region Abstract method redirects
	public AbstractIsTableExistsQuery IsTableExists(NodeTypes nodeType) => IsTableExists(nodeType.ToNodeTableName());
	#endregion

	#region Virtual(overridable) methods
	public virtual DeduplicationQuery Deduplicate() => new(Connection);
	public virtual FindWordQuery FindWord(GameMode gameMode, WordPreference wordPreference) => new(Connection, gameMode, wordPreference);
	public virtual IndexCreationQuery CreateIndex(string tableName, string columnName) => new(Connection, tableName, columnName);
	public virtual NodeAdditionQuery AddNode(string tableName) => new(Connection, tableName);
	public virtual NodeAdditionQuery AddNode(NodeTypes nodeType) => new(Connection, nodeType.ToNodeTableName());
	public virtual NodeDeletionQuery DeleteNode(string tableName) => new(Connection, tableName);
	public virtual NodeDeletionQuery DeleteNode(NodeTypes nodeType) => new(Connection, nodeType.ToNodeTableName());
	public virtual NodeListQuery ListNode() => new(Connection);
	public virtual VacuumQuery Vacuum() => new(Connection);
	public virtual WordAdditionQuery AddWord() => new(Connection);
	public virtual WordDeletionQuery DeleteWord() => new(Connection);
	#endregion
}
