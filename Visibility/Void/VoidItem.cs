using System;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace Visibility.Void;

public class VoidItem()
{
	[JsonIgnore]
	public string Name
	{
		get => $"{this.Firstname} {this.Lastname}";
		init
		{
			string[] name = value.Split(' ');
			this.Firstname = name[0].ToUppercase();
			this.Lastname = name[1].ToUppercase();

			byte[] nameBytes = Encoding.UTF8.GetBytes(this.Name + '\0');
			this.NameBytes = nameBytes.Length < 64 ? nameBytes : nameBytes.Take(64).ToArray();
		}
	}

	[JsonIgnore]
	public byte[] NameBytes { get; set; } = [];

	[JsonIgnore]
	// Do not serialize this property, because object id can be inconsistent between server restarts
	public uint ObjectId { get; set; } = 0;

	public ulong Id { get; set; }

	public string Firstname { get; set; } = string.Empty;
	public string Lastname { get; set; } = string.Empty;
	public string HomeworldName { get; set; } = string.Empty;
	public DateTime Time { get; set; }
	public uint HomeworldId { get; set; }
	public string Reason { get; set; } = string.Empty;
	public bool Manual { get; set; }

	[JsonConstructor]
	public VoidItem(
		ulong id,
		string firstname,
		string lastname,
		string homeworldName,
		DateTime time,
		uint homeworldId,
		string reason,
		bool manual): this()
	{
		this.Id = id;
		this.Name = $"{firstname} {lastname}";
		this.HomeworldName = homeworldName;
		this.Time = time;
		this.HomeworldId = homeworldId;
		this.Reason = reason;
		this.Manual = manual;
	}
}
