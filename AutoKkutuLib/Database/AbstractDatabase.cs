using AutoKkutuLib.Database.Extension;
using AutoKkutuLib.Utils;

namespace AutoKkutuLib.Database;

public abstract class AbstractDatabase : IDisposable
{
	private AbstractDatabaseConnection? _baseConnection;

	public AbstractDatabaseConnection Connection => _baseConnection.RequireNotNull();

	static AbstractDatabase()
	{
		typeof(WordModel).RegisterMapping();
	}

	protected AbstractDatabase()
	{
	}

	public abstract AbstractDatabaseConnection OpenSecondaryConnection();

	public abstract string GetDBType();

	protected void Initialize(AbstractDatabaseConnection defaultConnection)
	{
		if (_baseConnection != null)
			throw new InvalidOperationException($"{nameof(Connection)} is already initialized");
		_baseConnection = defaultConnection;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
			Connection.Dispose();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
