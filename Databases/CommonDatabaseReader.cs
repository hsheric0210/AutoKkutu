using System;

namespace AutoKkutu.Databases
{
	public abstract class CommonDatabaseReader : IDisposable
	{
		public object this[string name] => GetObject(name);

		public string GetString(string name) => GetString(GetOrdinal(name));

		public int GetInt32(string name) => GetInt32(GetOrdinal(name));

		public abstract bool Read();

		protected abstract object GetObject(string name);

		public abstract string GetString(int index);

		public abstract int GetInt32(int index);

		public abstract int GetOrdinal(string name);

		protected virtual void Dispose(bool disposing)
		{
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
