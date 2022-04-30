using System;
using System.Linq;
using System.Text;
using static AutoKkutu.Constants;

namespace AutoKkutu
{
	public static class RandomUtils
	{

		public static Random GLOBAL_RANDOM = new Random();

		public static string GenerateRandomString(int length, bool english, Random random = null)
		{
			if (random == null)
				random = GLOBAL_RANDOM;
			var builder = new StringBuilder(length);
			char start = '가';
			char end = '힣';
			if (english)
			{
				start = 'a';
				end = 'z';
			}

			for (int i = 0; i < length; i++)
				if (random.NextDouble() > 0.7)
					builder.Append(random.Next(10));
				else
					builder.Append((char)random.Next(start, end + 1));
			return builder.ToString();
		}
	}
}
