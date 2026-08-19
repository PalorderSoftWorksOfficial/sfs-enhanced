using UnityEngine;

namespace SFS;

public class FOV_Sync : MonoBehaviour
{
	public Canvas canvas;

	public Camera output;

	private void Update()
	{
		float num = (float)Screen.height / canvas.scaleFactor * VideoSettingsPC.main.settings.uiScale;
		output.fieldOfView = Mathf.Atan2(num / 2500f, 1f) * 2f * 57.29578f;
	}
}
