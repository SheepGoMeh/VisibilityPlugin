using System;
using Dalamud.Game;

namespace Visibility.Utils
{
	internal class AddressResolver : BaseAddressResolver
	{
		public IntPtr CharacterDtorAddress { get; private set; }

		public IntPtr CompanionEnableDrawAddress { get; private set; }

		public IntPtr CharacterDisableDrawAddress { get; private set; }

		public IntPtr CharacterEnableDrawAddress { get; private set; }

		protected override void Setup64Bit(SigScanner sig)
		{
			this.CharacterEnableDrawAddress = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9 74 33 45 33 C0");
			this.CharacterDisableDrawAddress = sig.ScanText("48 89 5C 24 ?? 41 56 48 83 EC 20 48 8B D9 48 8B 0D ?? ?? ?? ??");
			this.CompanionEnableDrawAddress = sig.ScanText("40 53 48 83 EC 20 80 B9 ?? ?? ?? ?? ?? 48 8B D9 72 0C F7 81 ?? ?? ?? ?? ?? ?? ?? ?? 74 44");
			this.CharacterDtorAddress = sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ??"); // Done
		}
	}
}
