using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.EF
{
	public class Word
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id
		{
			get; set;
		}

		[MaxLength(256)]
		public string WordString
		{
			get; set;
		}

		public char WordIndex
		{
			get; set;
		}

		public char ReverseWordIndex
		{
			get; set;
		}

		public string KkutuIndex
		{
			get; set;
		}

		public int Flags
		{
			get; set;
		}
	}
}
