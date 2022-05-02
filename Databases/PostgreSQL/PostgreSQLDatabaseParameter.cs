using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;

namespace AutoKkutu.Databases.PostgreSQL
{
	public class PostgreSQLDatabaseParameter : CommonDatabaseParameter
	{
		public PostgreSQLDatabaseParameter(string name, object value) : base(name, value)
		{
		}

		public PostgreSQLDatabaseParameter(CommonDatabaseType dataType, string name, object value) : base(dataType, name, value)
		{
		}

		public PostgreSQLDatabaseParameter(CommonDatabaseType dataType, byte precision, string name, object value) : base(dataType, precision, name, value)
		{
		}

		public PostgreSQLDatabaseParameter(ParameterDirection direction, CommonDatabaseType dataType, byte precision, string name, object value) : base(direction, dataType, precision, name, value)
		{
		}

		public NpgsqlParameter Translate()
		{
			var parameter = new NpgsqlParameter();
			parameter.ParameterName = Name;
			parameter.Value = Value;
			parameter.Precision = Precision;

			var datatype = TranslateDataType();
			if (datatype != null)
				parameter.NpgsqlDbType = (NpgsqlDbType)datatype;

			return parameter;
		}

		public NpgsqlDbType? TranslateDataType()
		{
			if (DataType == null)
				return null;

			switch (DataType)
			{
				case CommonDatabaseType.SmallInt:
					return NpgsqlDbType.Smallint;

				case CommonDatabaseType.MiddleInt:
					return NpgsqlDbType.Integer;

				case CommonDatabaseType.Character:
					return NpgsqlDbType.Char;

				case CommonDatabaseType.CharacterVarying:
					return NpgsqlDbType.Varchar;
			}

			throw new NotSupportedException(DataType.ToString());
		}
	}
}
