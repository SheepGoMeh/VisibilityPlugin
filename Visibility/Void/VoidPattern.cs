using System.Text.RegularExpressions;

using Newtonsoft.Json;

namespace Visibility.Void;

[method: JsonConstructor]
public class VoidPattern(
	string id,
	int version,
	string pattern,
	string description,
	bool offworld = false,
	bool enabled = true)
{
	public string Id { get; } = id;
	public int Version { get; set; } = version;
	public string Pattern { get; } = pattern;
	public string Description { get; } = description;
	public bool Offworld { get; set; } = offworld;
	public bool Enabled { get; set; } = enabled;

	[JsonIgnore] public readonly Regex Regex = new(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
}
