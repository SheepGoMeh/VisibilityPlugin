using System.Runtime.InteropServices;

namespace Visibility.Structs.Client.Game.Character
{
	[StructLayout(LayoutKind.Explicit, Size = 0x2BE0)]
	public unsafe struct BattleChara
	{
		[FieldOffset(0x0)] public Character Character;
	}
}