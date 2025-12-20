using System.Text;

using Dalamud.Game.ClientState.Objects.Types;

// Alias vers TON enum
using PluginVisibilityFlags = Visibility.VisibilityFlags;

namespace Visibility;

public static class Extensions
{
	public static string Format(this string format, params object[] args)
		=> string.Format(format, args);

	public static void ToggleBool(this ref bool property, bool value, bool toggle = false)
	{
		if (toggle)
			property = !property;
		else
			property = value;
	}

	public static string GetFirstname(this IGameObject actor)
		=> actor.Name.TextValue.Split(' ')[0];

	public static string GetLastname(this IGameObject actor)
		=> actor.Name.TextValue.Split(' ')[1];

	public static string ByteToString(this byte[] arr)
		=> Encoding.Default.GetString(arr).Replace("\0", string.Empty);

	public static string ToUppercase(this string str)
	{
		if (string.IsNullOrEmpty(str))
			return string.Empty;

		var arr = str.ToCharArray();
		arr[0] = char.ToUpper(arr[0]);
		return new string(arr);
	}

	// Extension sur int + TON enum (aliasé)
	public static bool TestFlag(this int value, PluginVisibilityFlags flag)
		=> (value & (int)flag) != 0;
}
