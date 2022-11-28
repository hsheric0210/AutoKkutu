using AutoKkutu.Constants;
using System;

namespace AutoKkutu.Databases.Extension
{
	public static class WordIndexExtension
	{
		public static string GetWordIndex(this WordModel word, WordIndexType wordIndexType)
		{
			if (word is null)
				throw new ArgumentNullException(nameof(word));

			return wordIndexType switch
			{
				WordIndexType.WordIndex => word.WordIndex,
				WordIndexType.ReverseWordIndex => word.ReverseWordIndex,
				WordIndexType.KkutuWordIndex => word.KkutuWorldIndex,
				_ => throw new ArgumentException("Unsupported word index type", nameof(wordIndexType)),
			};
		}

		public static void SetWordIndex(this WordModel word, WordIndexType wordIndexType, string newValue)
		{
			if (word is null)
				throw new ArgumentNullException(nameof(word));

			switch (wordIndexType)
			{
				case WordIndexType.WordIndex:
					word.WordIndex = newValue;
					break;
				case WordIndexType.ReverseWordIndex:
					word.WordIndex = newValue;
					break;
				case WordIndexType.KkutuWordIndex:
					word.WordIndex = newValue;
					break;
			}
		}
	}
}
