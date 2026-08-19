using UnityEngine;

namespace SFS.Parts.Modules;

public class OwnModule : MonoBehaviour
{
	public enum PartPack
	{
		BigParts = 0,
		Redstone_Atlas = 1,
		Full_Version = 3
	}

	public PartPack pack;

	public bool IsOwned
	{
		get
		{
			if (pack == PartPack.Full_Version)
			{
				return DevSettings.FullVersion;
			}
			return true;
		}
	}

	public virtual bool IsPremium => true;
}
