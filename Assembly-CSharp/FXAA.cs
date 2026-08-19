using SFS;
using UnityEngine;

public class FXAA : MonoBehaviour
{
	public Shader shader;

	private static readonly int RPCFrame = Shader.PropertyToID("_rcpFrame");

	private Material mat;

	private void Reset()
	{
		shader = Shader.Find("Hidden/FXAA3");
	}

	private void OnEnable()
	{
		if ((object)shader == null || !shader.isSupported)
		{
			base.enabled = false;
		}
		else if ((object)mat == null)
		{
			mat = new Material(shader);
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!VideoSettingsPC.main.settings.FXAA)
		{
			Graphics.Blit(source, destination);
			return;
		}
		mat.SetVector(RPCFrame, new Vector2(1f / (float)Screen.width, 1f / (float)Screen.height));
		Graphics.Blit(source, destination, mat);
	}
}
