using System.ComponentModel.DataAnnotations;

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
