using System;
using System.Text;
using System.Windows.Media;

namespace AutoKkutu
{
	public static class Utils
	{
		public static class ColorConstants
		{
			public static Color NormalColor = Color.FromRgb(64, 80, 141);

			public static Color WarningColor = Color.FromRgb(243, 108, 26);

			public static Color ErrorColor = Color.FromRgb(137, 45, 45);

			public static Color WaitColor = Color.FromRgb(121, 121, 121);
		}

		public static string GenerateRandomString(Random random, int length, bool english)
		{
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
