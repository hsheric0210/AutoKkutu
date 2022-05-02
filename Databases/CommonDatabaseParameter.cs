using System.Data;

namespace AutoKkutu.Databases
{
	public abstract class CommonDatabaseParameter
	{
		public CommonDatabaseType? DataType
		{
			get; private set;
		}

		public ParameterDirection Direction
		{
			get;
		} = ParameterDirection.Input;

		public string Name
		{
			get;
		}

		public byte Precision
		{
			get;
		}

		public object Value
		{
			get;
		}

		protected CommonDatabaseParameter(string name, object value)
		{
			Name = name;
			Value = value;
		}

		protected CommonDatabaseParameter(CommonDatabaseType dataType, string name, object value) : this(name, value)
		{
			DataType = dataType;
		}

		protected CommonDatabaseParameter(CommonDatabaseType dataType, byte precision, string name, object value) : this(dataType, name, value)
		{
			DataType = dataType;
			Precision = precision;
			Name = name;
			Value = value;
		}

		protected CommonDatabaseParameter(ParameterDirection direction, CommonDatabaseType dataType, byte precision, string name, object value) : this(dataType, precision, name, value)
		{
			Direction = direction;
		}
	}
}
