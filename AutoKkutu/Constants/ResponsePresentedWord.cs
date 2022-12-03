using System;

namespace AutoKkutu.Constants
{
	public class ResponsePresentedWord
	{
		public string Content
		{
			get;
		}

		public bool CanSubstitution
		{
			get;
		}

		public string? Substitution
		{
			get;
		}

		public ResponsePresentedWord(string content, bool canSubsitution, string substituation = "")
		{
			Content = content;
			CanSubstitution = canSubsitution;
			if (!CanSubstitution)
				return;
			Substitution = substituation;
		}

		public override bool Equals(object? obj) => obj is ResponsePresentedWord other
			&& string.Equals(Content, other.Content, StringComparison.OrdinalIgnoreCase)
			&& Substitution == other.Substitution
			&& (!CanSubstitution || string.Equals(Substitution, other.Substitution, StringComparison.OrdinalIgnoreCase));

		public override int GetHashCode() => HashCode.Combine(Content, CanSubstitution, Substitution);
	}
}
