using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

public class Atmosphere : MonoBehaviour
{
	public Planet planet;

	private Mesh mesh;

	private void OnDestroy()
	{
		Object.Destroy(mesh);
	}

	private void Update()
	{
		if (planet.HasAtmosphereVisuals && WorldTime.main != null)
		{
			planet.atmosphereMaterial.SetFloat("_PosX", (float)(WorldTime.main.worldTime * 5.0 / planet.Radius * (double)planet.data.atmosphereVisuals.CLOUDS.velocity % 1.0));
		}
	}

	public static Atmosphere Create(Planet planet, Transform parent, Transform atmospherePrefab)
	{
		Transform obj = Object.Instantiate(atmospherePrefab.transform);
		obj.name = planet.codeName + " Atmosphere";
		obj.parent = parent;
		obj.localPosition = Vector3.forward * planet.data.atmosphereVisuals.GRADIENT.positionZ;
		obj.transform.localScale = Vector3.one * (float)(planet.data.atmosphereVisuals.GRADIENT.height + planet.Radius);
		obj.GetComponent<MeshRenderer>().material = planet.atmosphereMaterial;
		Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
		mesh.uv = GetUV(mesh.uv.Length);
		Atmosphere atmosphere = obj.gameObject.AddComponent<Atmosphere>();
		atmosphere.planet = planet;
		atmosphere.mesh = mesh;
		return atmosphere;
	}

	public void SetLayer(string layer)
	{
		base.gameObject.layer = LayerMask.NameToLayer(layer);
	}

	private static Vector2[] GetUV(int verticeCount)
	{
		Vector2[] array = new Vector2[verticeCount];
		float num = 1f / (float)(verticeCount - 2);
		for (int i = 1; i < array.Length; i++)
		{
			array[i] = new Vector2((float)(i - 1) * num, 1f);
		}
		array[0] = Vector2.zero;
		return array;
	}
}
