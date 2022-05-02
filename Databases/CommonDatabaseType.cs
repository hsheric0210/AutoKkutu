using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu.Databases
{
	public enum CommonDatabaseType
	{
		/// <summary>
		/// 16-bit integer
		/// </summary>
		SmallInt,

		/// <summary>
		/// 32-bit integer
		/// </summary>
		MiddleInt,

		/// <summary>
		/// Fixed-length string
		/// </summary>
		Character,

		/// <summary>
		/// Variable-length string
		/// </summary>
		CharacterVarying
	}
}
