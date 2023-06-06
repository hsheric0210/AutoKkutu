using System.Text;

namespace AutoKkutuLib.Extension;

public static class RandomExtension
{
	public static string NextString(this Random random, int length, bool english)
	{
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

	/// <summary>
	/// Random JavaScript Type Name Generator
	/// </summary>
	public static string NextTypeName(this Random random, int length)
	{
		var builder = new StringBuilder(length);
		for (var i = 0; i < length; i++)
		{
			var choice = random.Next(i == 0 ? 2 : 3); // Prevent numeric char on the first
			if (choice == 0)
				builder.Append((char)random.Next('A', 'Z' + 1));
			else if (choice == 1)
				builder.Append((char)random.Next('a', 'z' + 1));
			else
				builder.Append(random.Next(10));
		}
		return builder.ToString();
	}
}
