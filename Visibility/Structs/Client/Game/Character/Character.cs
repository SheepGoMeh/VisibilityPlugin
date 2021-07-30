using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;
using Visibility.Structs.Client.Game.Object;

namespace Visibility.Structs.Client.Game.Character
{
	[StructLayout(LayoutKind.Explicit, Size = 0x19C8)]
	public unsafe struct Character
	{
		[FieldOffset(0x0)] public GameObject GameObject;
		[FieldOffset(ActorOffsets.PlayerCharacterTargetActorId)] public uint PlayerCharacterTargetActorId;
		[FieldOffset(ActorOffsets.BattleNpcTargetActorId)] public uint BattleNpcTargetActorId;
		[FieldOffset(ActorOffsets.CompanyTag)] public fixed byte CompanyTag[7];
		[FieldOffset(ActorOffsets.NameId)] public int NameID;
		[FieldOffset(0x1950)] public uint CompanionOwnerID;
		[FieldOffset(ActorOffsets.CurrentWorld)] public ushort CurrentWorld;
		[FieldOffset(ActorOffsets.HomeWorld)] public ushort HomeWorld;
		[FieldOffset(ActorOffsets.CurrentHp)] public int CurrentHp;
		[FieldOffset(0x19A0)] public byte StatusFlags;
	}
}