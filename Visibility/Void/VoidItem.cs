using System;
using System.Linq;
using System.Text;

using Dalamud.Game.ClientState.Objects.SubKinds;

using Newtonsoft.Json;

namespace Visibility.Void;

public class VoidItem
{
	[JsonIgnore]
	public string Name
	{
		get => $"{this.Firstname} {this.Lastname}";
		set
		{
			string[] name = value.Split(' ');
			this.Firstname = name[0].ToUppercase();
			this.Lastname = name[1].ToUppercase();

			byte[] nameBytes = Encoding.UTF8.GetBytes(this.Name + '\0');
			this.NameBytes = nameBytes.Length < 64 ? nameBytes : nameBytes.Take(64).ToArray();
		}
	}

	[JsonIgnore] public byte[] NameBytes { get; set; }

	[JsonIgnore]
	public uint ObjectId { get; set; } // Do not serialize because object id can be inconsistent between server restarts

	public string Firstname { get; set; }
	public string Lastname { get; set; }
	public string HomeworldName { get; set; }
	public DateTime Time { get; set; }
	public uint HomeworldId { get; set; }
	public string Reason { get; set; }
	public bool Manual { get; set; }

	public VoidItem()
	{
		this.NameBytes = Array.Empty<byte>();
		this.Firstname = string.Empty;
		this.Lastname = string.Empty;
		this.HomeworldName = string.Empty;
		this.Reason = string.Empty;
		this.ObjectId = 0;
	}

	public VoidItem(IPlayerCharacter actor, string reason, bool manual): this()
	{
		this.Name = actor.Name.TextValue;
		this.Time = DateTime.Now;
		this.HomeworldId = actor.HomeWorld.Id;
		this.HomeworldName = actor.HomeWorld.GameData!.Name;
		this.Reason = reason;
		this.Manual = manual;
	}

	public VoidItem(string name, string homeworldName, uint homeworldId, string reason, bool manual): this()
	{
		this.Name = name;
		this.Time = DateTime.Now;
		this.HomeworldId = homeworldId;
		this.HomeworldName = homeworldName;
		this.Reason = reason;
		this.Manual = manual;
	}

	[JsonConstructor]
	public VoidItem(
		string firstname,
		string lastname,
		string homeworldName,
		DateTime time,
		uint homeworldId,
		string reason,
		bool manual): this()
	{
		this.Name = $"{firstname} {lastname}";
		this.HomeworldName = homeworldName;
		this.Time = time;
		this.HomeworldId = homeworldId;
		this.Reason = reason;
		this.Manual = manual;
	}
}
