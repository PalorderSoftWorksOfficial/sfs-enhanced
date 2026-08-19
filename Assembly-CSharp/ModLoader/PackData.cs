using UnityEngine;

namespace ModLoader;

public class PackData : ScriptableObject
{
	public string DisplayName;

	public string Version;

	public string Description;

	public string Author;

	public bool ShowIcon;

	public Texture2D Icon;

	public static PackData ExampleTexturePack()
	{
		PackData packData = ScriptableObject.CreateInstance<PackData>();
		packData.Author = "Spaceflight Simulator";
		packData.Description = "Example pack with game textures";
		packData.Icon = null;
		packData.Version = "1.0";
		packData.DisplayName = "Example Textures";
		packData.ShowIcon = false;
		return packData;
	}
}
