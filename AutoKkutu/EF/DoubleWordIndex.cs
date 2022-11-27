using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.EF
{
	public class DoubleWordIndex
	{
		[Key, MinLength(2), MaxLength(2)]
		public string Index
		{
			get; set;
		}
	}
}
