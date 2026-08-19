using SFS.World;
using SFS.World.PlanetModules;
using UnityEngine;

namespace SFS;

public class PostProcessing : MonoBehaviour
{
	private static readonly int Hue = Shader.PropertyToID("_Hue");

	private static readonly int Saturation = Shader.PropertyToID("_Saturation");

	private static readonly int Contrast = Shader.PropertyToID("_Contrast");

	private static readonly int Multiplier = Shader.PropertyToID("_Multiplier");

	private static readonly int Intensity = Shader.PropertyToID("_Intensity");

	private static readonly int AmbientLight = Shader.PropertyToID("_AmbientLight");

	public Material postProcessingMaterial;

	public Material starsMaterial;

	public Stars stars;

	public Material[] reentryMaterials = new Material[0];

	public void SetAmbient(PostProcessingModule.Key ambient, float maxStarIntensity = 1f)
	{
		postProcessingMaterial.SetFloat(Hue, ambient.hueShift);
		postProcessingMaterial.SetFloat(Saturation, ambient.saturation);
		postProcessingMaterial.SetFloat(Contrast, ambient.contrast);
		float num = 0.3f * ambient.red + 0.59f * ambient.green + 0.11f * ambient.blue;
		Vector4 value = new Vector4(ambient.red / num, ambient.green / num, ambient.blue / num, 1f);
		postProcessingMaterial.SetVector(Multiplier, value);
		if (GameManager.main != null)
		{
			float num2 = Mathf.Min(ambient.starIntensity, maxStarIntensity);
			starsMaterial.SetColor("_Color", new Color(1f, 1f, 1f, num2));
			stars.enabled = num2 > 0f;
		}
		Material[] array = reentryMaterials;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetFloat(Hue, (0f - ambient.hueShift) * 0.7f);
		}
		if (!(RenderSortingManager.main != null))
		{
			return;
		}
		float value2 = ambient.shadowIntensity * 1.1f;
		float value3 = 0.25f;
		foreach (Material value4 in RenderSortingManager.main.partMaterials.Values)
		{
			value4.SetFloat(Intensity, value2);
			value4.SetFloat(AmbientLight, value3);
		}
		Base.partsLoader.partMaterial.SetFloat(Intensity, value2);
		Base.partsLoader.partModelMaterialTexture.SetFloat(AmbientLight, value3);
		Base.partsLoader.partModelMaterialNormals.SetFloat(AmbientLight, value3);
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(source, destination, postProcessingMaterial);
	}
}
