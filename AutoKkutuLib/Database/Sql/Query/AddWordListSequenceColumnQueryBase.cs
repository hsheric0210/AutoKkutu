namespace AutoKkutuLib.Database.Sql.Query;
public abstract class AddWordListSequenceColumnQueryBase : SqlQuery<bool>
{
	protected AddWordListSequenceColumnQueryBase(DbConnectionBase connection) : base(connection) { }
}
