using System.Text;

namespace AutoKkutuLib;
public class NameMapping
{
	private readonly IDictionary<string, string> mapping = new Dictionary<string, string>();

	public void Add(string key, object value) => mapping.Add(key, value?.ToString() ?? "~NULL~");

	public virtual string ApplyTo(string target)
	{
		foreach ((var from, var to) in mapping)
			target = target.Replace(from, to);
		return target;
	}

	public override string ToString() => new StringBuilder().Append('{').AppendJoin(", ", mapping.Select(kv => $"\"{kv.Key}\": \"{kv.Value}\"")).Append('}').ToString(); // Json type :)
}
