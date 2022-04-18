using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu.Databases
{
	public interface CommonDatabaseReader : IDisposable
	{
		bool Read();
		object GetObject(string name);
	}
}
