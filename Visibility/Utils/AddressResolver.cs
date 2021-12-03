using System;
using Dalamud.Game;
using Dalamud.Game.Internal;

namespace Visibility.Utils
{
	internal class AddressResolver : BaseAddressResolver
	{
		public IntPtr LocalPlayerAddress { get; private set; } 
		public IntPtr CharacterDtorAddress { get; private set; }

		public IntPtr CompanionEnableDrawAddress { get; private set; }

		public IntPtr CharacterDisableDrawAddress { get; private set; }

		public IntPtr CharacterEnableDrawAddress { get; private set; }

		protected override void Setup64Bit(SigScanner sig)
		{
			LocalPlayerAddress = sig.GetStaticAddressFromSig("48 8B F8 48 8D 2D ? ? ? ? ");
			CharacterEnableDrawAddress = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9 74 30 33 D2");
			CharacterDisableDrawAddress = sig.ScanText("48 89 7C 24 20 41 56 48  83 EC 20 48 8B F9 48 8B 0D ?? ?? ?? ??");
			CompanionEnableDrawAddress = sig.ScanText("40 53 48 83 EC 20 80 B9 ?? ?? ?? ?? ?? 48 8B D9 72 0C F7 81 ?? ?? ?? ?? ?? ?? ?? ?? 74 41");
			CharacterDtorAddress = sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ??"); // Done
		}
	}
}
