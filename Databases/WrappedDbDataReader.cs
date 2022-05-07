using System;
using System.Collections;
using System.Data.Common;

namespace AutoKkutu.Databases
{
	/// <summary>
	/// A DbDataReader that disposes the underlying command together when it get disposed.
	/// </summary>
	public class WrappedDbDataReader : DbDataReader
	{
		private readonly CommonDatabaseCommand Command;
		private readonly DbDataReader BaseReader;

		public override int Depth => BaseReader.Depth;

		public override int FieldCount => BaseReader.FieldCount;

		public override bool HasRows => BaseReader.HasRows;

		public override bool IsClosed => BaseReader.IsClosed;

		public override int RecordsAffected => BaseReader.RecordsAffected;

		public WrappedDbDataReader(CommonDatabaseCommand command, DbDataReader underlyingReader)
		{
			Command = command;
			BaseReader = underlyingReader;
		}

		public override object this[int ordinal] => BaseReader[ordinal];

		public override object this[string name] => BaseReader[name];
		public override bool GetBoolean(int ordinal) => BaseReader.GetBoolean(ordinal);

		public override byte GetByte(int ordinal) => BaseReader.GetByte(ordinal);

		public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => BaseReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

		public override char GetChar(int ordinal) => BaseReader.GetChar(ordinal);

		public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => BaseReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

		public override string GetDataTypeName(int ordinal) => BaseReader.GetDataTypeName(ordinal);

		public override DateTime GetDateTime(int ordinal) => BaseReader.GetDateTime(ordinal);

		public override decimal GetDecimal(int ordinal) => BaseReader.GetDecimal(ordinal);

		public override double GetDouble(int ordinal) => BaseReader.GetDouble(ordinal);

		public override IEnumerator GetEnumerator() => BaseReader.GetEnumerator();

		public override Type GetFieldType(int ordinal) => BaseReader.GetFieldType(ordinal);

		public override float GetFloat(int ordinal) => BaseReader.GetFloat(ordinal);

		public override Guid GetGuid(int ordinal) => BaseReader.GetGuid(ordinal);

		public override short GetInt16(int ordinal) => BaseReader.GetInt16(ordinal);

		public override int GetInt32(int ordinal) => BaseReader.GetInt32(ordinal);

		public override long GetInt64(int ordinal) => BaseReader.GetInt64(ordinal);

		public override string GetName(int ordinal) => BaseReader.GetName(ordinal);

		public override int GetOrdinal(string name) => BaseReader.GetOrdinal(name);

		public override string GetString(int ordinal) => BaseReader.GetString(ordinal);

		public override object GetValue(int ordinal) => BaseReader.GetValue(ordinal);

		public override int GetValues(object[] values) => BaseReader.GetValues(values);

		public override bool IsDBNull(int ordinal) => BaseReader.IsDBNull(ordinal);

		public override bool NextResult() => BaseReader.NextResult();

		public override bool Read() => BaseReader.Read();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Command.Dispose();
			base.Dispose(disposing);
		}
	}
}
