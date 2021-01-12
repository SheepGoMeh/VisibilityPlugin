using System.Runtime.InteropServices;

namespace Visibility.Structs.Client.Game.Character
{
	[StructLayout(LayoutKind.Explicit, Size = 0x19F0)]
	public unsafe struct Companion
	{
		[FieldOffset(0x0)] public Character Character;
	}
}