using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.EF
{
	public class SingleWordIndex
	{
		[Key]
		public char Index
		{
			get; set;
		}
	}
}
