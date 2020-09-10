using System;
using System.Runtime.InteropServices;
using Dalamud.Plugin;

namespace Visibility.Utils
{
    internal class PlaceholderResolver
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate ulong ResolvePlaceholderActor(long param1, string param2, byte param3, byte param4);
        private ResolvePlaceholderActor _placeholderResolver;
        private IntPtr _magicUiObject = IntPtr.Zero;
        private IntPtr _magicStructInfo = IntPtr.Zero;
        private readonly AddressResolver _address = new AddressResolver();

        public void Init(DalamudPluginInterface pluginInterface)
        {
            _magicStructInfo = pluginInterface.TargetModuleScanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 05 ?? ?? ?? ?? 48 85 C9 74 0C");
            _placeholderResolver = Marshal.GetDelegateForFunctionPointer<ResolvePlaceholderActor>(_address.ResolvePlaceholderText);
            _address.Setup(pluginInterface.TargetModuleScanner);
            SetupPlaceholderResolver();
        }
        
        private void SetupPlaceholderResolver()
        {
            while (_magicUiObject == IntPtr.Zero)
            {
                try
                {
                    var step2 = Marshal.ReadIntPtr(_magicStructInfo) + 8;
                    _magicUiObject = Marshal.ReadIntPtr(step2) + 0xe780 + 0x50;
                }
                catch (Exception)
                {
                    _magicUiObject = IntPtr.Zero;
                }
            }
        }
        
        public int GetTargetActorId(string placeholder)
        {
            var ptr = (IntPtr)_placeholderResolver((long)_magicUiObject, placeholder, 1, 0);
            if (ptr != IntPtr.Zero || (int)ptr != 0)
                return Marshal.ReadInt32(ptr + 0x74);
            return 0;
        }
    }
}