using System.Text;

namespace AutoKkutuLib.Game.Enterer;

public struct EnterInfo : IEquatable<EnterInfo>
{
	public string? Content { get; set; }
	public EnterOptions Options { get; }
	public PathDetails PathInfo { get; }

	public int GetTotalDelay() => Options.GetDelayFor(Content);

	public EnterInfo(EnterOptions delayParam, PathDetails param, string? content = null)
	{
		Options = delayParam;
		PathInfo = param;
		Content = content;
	}

	public static implicit operator PathDetails(EnterInfo param) => param.PathInfo;
	public static implicit operator EnterOptions(EnterInfo param) => param.Options;

	public static bool operator ==(EnterInfo left, EnterInfo right) => left.Equals(right);
	public static bool operator !=(EnterInfo left, EnterInfo right) => !(left == right);

	public bool HasFlag(PathFlags flag) => PathInfo.HasFlag(flag);

	public override string ToString()
	{
		var builder = new StringBuilder();
		builder.Append(nameof(EnterInfo)).Append('{');
		builder.Append(nameof(Content)).Append(": ").Append(Content).Append(", ");
		builder.Append(nameof(Options)).Append(": ").Append(Options).Append(", ");
		builder.Append(nameof(PathInfo)).Append(": ").Append(PathInfo);
		return builder.Append('}').ToString();
	}
	public override bool Equals(object? obj) => obj is EnterInfo info && Equals(info);
	public bool Equals(EnterInfo other) => Content == other.Content && Options.Equals(other.Options) && PathInfo.Equals(other.PathInfo);
	public override int GetHashCode() => HashCode.Combine(Content, Options, PathInfo);
}
