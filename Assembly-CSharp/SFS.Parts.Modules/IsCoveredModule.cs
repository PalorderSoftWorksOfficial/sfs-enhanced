using SFS.Translations;
using SFS.UI;
using UnityEngine;

namespace SFS.Parts.Modules;

public class IsCoveredModule : MonoBehaviour
{
	public SurfaceData surface;

	public TranslationVariable onCoveredMsg;

	public UsePartUnityEvent onPartUsed;

	public void Activate(UsePartData a)
	{
		if (SurfaceData.IsSurfaceCovered(surface))
		{
			string text = onCoveredMsg.Field;
			if (!string.IsNullOrEmpty(text))
			{
				MsgDrawer.main.Log(text);
			}
		}
		else
		{
			onPartUsed.Invoke(a);
		}
	}
}
