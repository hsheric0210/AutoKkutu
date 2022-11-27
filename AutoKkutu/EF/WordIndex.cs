using System.ComponentModel.DataAnnotations;

namespace AutoKkutu.EF
{
	public class WordIndex
	{
		[Key, MinLength(1), MaxLength(2)]
		public string Index
		{
			get; set;
		}
	}
}
