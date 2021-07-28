using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.BaseTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;

namespace Visibility
{
	public static class Extensions
	{
		public static bool IsStatus(this GameObject obj, StatusFlags flag)
		{
			return (Marshal.ReadByte(obj.Address + 0x1906) & (byte) flag) > 0;
		}

		public static string GetFirstname(this GameObject obj)
		{
			return obj.Name.TextValue.Split(' ')[0];
		}

		public static string GetLastname(this GameObject obj)
		{
			return obj.Name.TextValue.Split(' ')[1];
		}

		public static string ByteToString(this byte[] arr)
		{
			return Encoding.Default.GetString(arr).Replace("\0", string.Empty);
		}

		public static string ToUppercase(this string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return string.Empty;
			}
			
			var arr = str.ToCharArray();
			arr[0] = char.ToUpper(arr[0]);
			return new string(arr);
		}

		public static async void Rerender(this GameObject o)
		{
			await Task.Run(async () =>
			{
				try
				{
					var addrRenderToggle = o.Address + 0x104;
					var renderToggle = Marshal.ReadInt32(addrRenderToggle);

					if (o is PlayerCharacter)
					{
						renderToggle |= (int)VisibilityFlags.Invisible;
						Marshal.WriteInt32(addrRenderToggle, renderToggle);
						await Task.Delay(100);
						renderToggle &= ~(int)VisibilityFlags.Invisible;
						Marshal.WriteInt32(addrRenderToggle, renderToggle);
						await Task.Delay(100);
					}
					else
					{
						renderToggle |= (int)VisibilityFlags.Invisible;
						Marshal.WriteInt32(addrRenderToggle, renderToggle);
						await Task.Delay(10);
						renderToggle &= ~(int)VisibilityFlags.Invisible;
						Marshal.WriteInt32(addrRenderToggle, renderToggle);
					}
				}
				catch (Exception ex)
				{
#if DEBUG
					PluginLog.LogError(ex.ToString());
#endif
				}
			});
		}

		public static async void Render(this GameObject o)
		{
			await Task.Run(() =>
			{
				try
				{
					var addrRenderToggle = o.Address + 0x104;
					var renderToggle = Marshal.ReadInt32(addrRenderToggle);
					if ((renderToggle & (int) VisibilityFlags.Invisible) != (int) VisibilityFlags.Invisible &&
					    (o.ObjectKind != ObjectKind.MountType ||
					     (renderToggle & (int) VisibilityFlags.Unknown15) != (int) VisibilityFlags.Unknown15)) return;
					renderToggle &= ~(int)VisibilityFlags.Invisible;
					Marshal.WriteInt32(addrRenderToggle, renderToggle);
				}
				catch (Exception ex)
				{
#if DEBUG
					PluginLog.LogError(ex.ToString());
#endif
				}
			});
		}

		public static async void Hide(this GameObject o)
		{
			await Task.Run(() =>
			{
				try
				{
					var addrRenderToggle = o.Address + 0x104;
					var renderToggle = Marshal.ReadInt32(addrRenderToggle);

					renderToggle |= (int)VisibilityFlags.Invisible;
					Marshal.WriteInt32(addrRenderToggle, renderToggle);
				}
				catch (Exception ex)
				{
#if DEBUG
					PluginLog.LogError(ex.ToString());
#endif
				}
			});
		}
	}
}
