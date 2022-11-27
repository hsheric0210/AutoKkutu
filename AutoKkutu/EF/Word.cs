using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.EF
{
	public static class WordConstant
	{
		public const int MaxLength = 256;

		public const int MaxWordPriority = 131072; // 256(Max db word length) * 256(Max mission char count per word) * 2(For correct result)
	}

	public class Word
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id
		{
			get; set;
		}

		[MaxLength(WordConstant.MaxLength)]
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
