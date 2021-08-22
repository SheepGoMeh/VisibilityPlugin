using System;
using System.Linq;
using System.Text;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Newtonsoft.Json;

namespace Visibility.Void
{
	public class VoidItem
	{
		[JsonIgnore]
		public string Name
		{
			get => $"{Firstname} {Lastname}";
			set
			{
				var name = value.Split(' ');
				Firstname = name[0].ToUppercase();
				Lastname = name[1].ToUppercase();

				var nameBytes = Encoding.UTF8.GetBytes(Name + '\0');
				NameBytes = nameBytes.Length < 30 ? nameBytes : nameBytes.Take(30).ToArray();
			}
		}

		[JsonIgnore] public byte[] NameBytes { get; set; }

		public string Firstname { get; set; }
		public string Lastname { get; set; }
		public string HomeworldName { get; set; }
		public DateTime Time { get; set; }
		public uint HomeworldId { get; set; }
		public string Reason { get; set; }
		public bool Manual { get; set; }

		public VoidItem(PlayerCharacter actor, string reason, bool manual)
		{
			Name = actor.Name.TextValue;
			Time = DateTime.Now;
			HomeworldId = actor.HomeWorld.Id;
			HomeworldName = actor.HomeWorld.GameData.Name;
			Reason = reason;
			Manual = manual;
		}

		public VoidItem(string name, string homeworldName, uint homeworldId, string reason, bool manual)
		{
			Name = name;
			Time = DateTime.Now;
			HomeworldId = homeworldId;
			HomeworldName = homeworldName;
			Reason = reason;
			Manual = manual;
		}
	}
}