using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;

namespace Visibility.Structs.Client.Game.Object
{
	[StructLayout(LayoutKind.Explicit, Size = 0x1A0)]
	public unsafe struct GameObject
	{
		[FieldOffset(ActorOffsets.Name)] public fixed byte Name[30];
		[FieldOffset(ActorOffsets.ActorId)] public uint ObjectID;
		[FieldOffset(ActorOffsets.OwnerId)] public uint OwnerID;
		[FieldOffset(ActorOffsets.ObjectKind)] public byte ObjectKind;
		[FieldOffset(ActorOffsets.SubKind)] public byte SubKind;
		[FieldOffset(0xF0)] public void* DrawObject;
		[FieldOffset(0x104)] public int RenderFlags;
	}
}