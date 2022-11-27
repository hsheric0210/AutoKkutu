using MySql.Data.MySqlClient;
using System;
using System.Data.Common;

namespace AutoKkutu.Databases.MySQL
{
	public class MySQLDatabaseParameter : CommonDatabaseParameter
	{
		public MySQLDatabaseParameter(string name, object? value) : base(name, value)
		{
		}

		public MySQLDatabaseParameter(CommonDatabaseType dataType, string name, object? value) : base(dataType, name, value)
		{
		}

		public MySQLDatabaseParameter(CommonDatabaseType dataType, byte precision, string name, object? value) : base(dataType, precision, name, value)
		{
		}

		public override DbParameter Translate()
		{
			var parameter = new MySqlParameter
			{
				ParameterName = Name,
				Value = Value,
				Precision = Precision
			};

			MySqlDbType? datatype = TranslateDataType();
			if (datatype != null)
				parameter.MySqlDbType = (MySqlDbType)datatype;

			return parameter;
		}

		public MySqlDbType? TranslateDataType()
		{
			if (DataType == null)
				return null;

			return DataType switch
			{
				CommonDatabaseType.SmallInt => MySqlDbType.Int16,
				CommonDatabaseType.MiddleInt => MySqlDbType.Int32,
				CommonDatabaseType.Character or CommonDatabaseType.CharacterVarying => MySqlDbType.VarChar,
				_ => throw new NotSupportedException(DataType.ToString()),
			};
		}
	}
}
