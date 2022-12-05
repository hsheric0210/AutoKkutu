using System.Text;

namespace AutoKkutuLib.Utils;

public static class RandomUtils
{
	private static readonly Random GLOBAL_RANDOM = new();

	public static string GenerateRandomString(int length, bool english, Random? random = null)
	{
		random ??= GLOBAL_RANDOM;

		var builder = new StringBuilder(length);
		var start = '가';
		var end = '힣';
		if (english)
		{
			start = 'a';
			end = 'z';
		}

		for (var i = 0; i < length; i++)
		{
			if (random.NextDouble() > 0.7)
				builder.Append(random.Next(10));
			else
				builder.Append((char)random.Next(start, end + 1));
		}

		return builder.ToString();
	}
}
