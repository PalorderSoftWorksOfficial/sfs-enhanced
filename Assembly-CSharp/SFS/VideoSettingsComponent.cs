using UnityEngine;
using UnityEngine.UI;

namespace SFS;

public class VideoSettingsComponent : MonoBehaviour
{
	public CanvasScaler scaler;

	public CanvasGroup canvasRenderer;

	public float minOpacity;

	public float maxOpacity = 1f;

	private void Start()
	{
		scaler.enabled = true;
		VideoSettingsPC.main.onScaleUIChanged.AddListener(Apply);
		VideoSettingsPC.main.onOpacityChanged.AddListener(Apply);
		Apply();
	}

	private void Apply()
	{
		scaler.referenceResolution = new Vector2(1f, 1600f / VideoSettingsPC.main.settings.uiScale);
		if (canvasRenderer != null)
		{
			canvasRenderer.alpha = Mathf.Lerp(minOpacity, maxOpacity, VideoSettingsPC.main.settings.uiOpacity);
		}
	}
}
