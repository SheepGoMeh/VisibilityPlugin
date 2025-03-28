using System.Text;

using Dalamud.Game.ClientState.Objects.Types;

namespace Visibility;

public static class Extensions
{
	public static string Format(this string format, params object[] args) => string.Format(format, args);

	/// <summary>
	/// Toggles boolean value if toggle is true, otherwise sets it to value
	/// </summary>
	/// <param name="property">Boolean property being modified</param>
	/// <param name="value">Value if not toggled</param>
	/// <param name="toggle">If true, toggle property</param>
	public static void ToggleBool(this ref bool property, bool value, bool toggle = false)
	{
		if (toggle)
		{
			property = !property;
		}
		else
		{
			property = value;
		}
	}

	public static string GetFirstname(this IGameObject actor) => actor.Name.TextValue.Split(' ')[0];

	public static string GetLastname(this IGameObject actor) => actor.Name.TextValue.Split(' ')[1];

	public static string ByteToString(this byte[] arr) => Encoding.Default.GetString(arr).Replace("\0", string.Empty);

	public static string ToUppercase(this string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return string.Empty;
		}

		char[] arr = str.ToCharArray();
		arr[0] = char.ToUpper(arr[0]);
		return new string(arr);
	}

	public static bool TestFlag(this int value, VisibilityFlags flag) => (value & (byte)flag) != 0;
}
