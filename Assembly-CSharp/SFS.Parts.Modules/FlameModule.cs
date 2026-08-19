using System;
using System.Linq;
using UnityEngine;

namespace SFS.Parts.Modules;

[ExecuteInEditMode]
public class FlameModule : MonoBehaviour
{
	public MeshFilter meshFilter;

	[Range(0f, 1f)]
	public float throttle;

	[Range(0f, 1f)]
	public float vacuum;

	public float startWidth;

	public AnimationCurve ground;

	public AnimationCurve vac;

	public Vector3 scaleGround = Vector3.one;

	public Vector3 scaleVac = Vector3.one;

	public float edgeFixed;

	public float edgePercent;

	public float textureAmount = 1f;

	public float opacityPower = 3f;

	public int count;

	private void Start()
	{
		meshFilter = base.gameObject.GetComponent<MeshFilter>();
	}

	private void Update()
	{
		Vector2[] points = GetPoints(ground, scaleGround);
		Vector2[] points2 = GetPoints(vac, scaleVac);
		Vector2[] array = new Vector2[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = Vector2.Lerp(points[i], points2[i], vacuum);
		}
		CreateMesh(array);
	}

	private Vector2[] GetPoints(AnimationCurve curve, Vector3 scale)
	{
		float time = curve.keys.Last().time;
		Vector2[] array = new Vector2[count];
		for (int i = 0; i < count; i++)
		{
			float num = (time - (float)i / (float)(count - 1) * time) * throttle;
			array[i] = new Vector2(startWidth, 0f) + new Vector2(curve.Evaluate(num) * scale.x, (0f - num) * scale.y) * scale.z;
		}
		return array;
	}

	private void CreateMesh(Vector2[] points)
	{
		int num = 4;
		int num2 = points.Length - 1;
		int num3 = num * num2 * 4;
		float num4 = UnityEngine.Random.Range(0f, 1f);
		Vector3[] array = new Vector3[num3];
		Color[] array2 = new Color[num3];
		Vector3[] array3 = new Vector3[num3];
		int[] array4 = new int[num3];
		Color[] array5 = new Color[5]
		{
			new Color(1f, 1f, 1f, 0f),
			new Color(1f, 1f, 1f, 0.7f),
			new Color(1f, 1f, 1f, 1f),
			new Color(1f, 1f, 1f, 0.7f),
			new Color(1f, 1f, 1f, 0f)
		};
		float[] array6 = new float[5]
		{
			0f * textureAmount + num4,
			0.25f * textureAmount + num4,
			0.5f * textureAmount + num4,
			0.75f * textureAmount + num4,
			1f * textureAmount + num4
		};
		Func<float, float>[] array7 = new Func<float, float>[5]
		{
			(float x) => (0f - x) * (1f + edgePercent) - edgeFixed,
			(float x) => 0f - x,
			(float x) => 0f,
			(float x) => x,
			(float x) => x * (1f + edgePercent) + edgeFixed
		};
		Color[] array8 = new Color[points.Length];
		for (int num5 = 0; num5 < count; num5++)
		{
			float f = (float)num5 / (float)(count - 1);
			array8[num5] = new Color(1f, 1f, 1f, Mathf.Pow(f, opacityPower));
		}
		int num6 = 0;
		for (int num7 = 0; num7 < num; num7++)
		{
			for (int num8 = 0; num8 < num2; num8++)
			{
				float y = (float)(num8 + 1) / (float)(count - 1);
				float y2 = (float)num8 / (float)(count - 1);
				Vector2 vector = points[num8 + 1];
				Vector2 vector2 = points[num8];
				array[num6] = new Vector2(array7[num7](vector.x), vector.y);
				array[num6 + 1] = new Vector2(array7[num7 + 1](vector.x), vector.y);
				array[num6 + 2] = new Vector2(array7[num7 + 1](vector2.x), vector2.y);
				array[num6 + 3] = new Vector2(array7[num7](vector2.x), vector2.y);
				float[] quadM = UV_Utility.GetQuadM(array[num6], array[num6 + 1], array[num6 + 2], array[num6 + 3]);
				array3[num6] = new Vector3(array6[num7], y, 1f) * quadM[0];
				array3[num6 + 1] = new Vector3(array6[num7 + 1], y, 1f) * quadM[1];
				array3[num6 + 2] = new Vector3(array6[num7 + 1], y2, 1f) * quadM[2];
				array3[num6 + 3] = new Vector3(array6[num7], y2, 1f) * quadM[3];
				array2[num6] = array5[num7] * array8[num8 + 1];
				array2[num6 + 1] = array5[num7 + 1] * array8[num8 + 1];
				array2[num6 + 2] = array5[num7 + 1] * array8[num8];
				array2[num6 + 3] = array5[num7] * array8[num8];
				num6 += 4;
			}
		}
		for (int num9 = 0; num9 < num3; num9++)
		{
			array4[num9] = num9;
		}
		if (meshFilter.sharedMesh == null)
		{
			meshFilter.sharedMesh = new Mesh();
		}
		Mesh sharedMesh = meshFilter.sharedMesh;
		sharedMesh.Clear();
		sharedMesh.SetVertices(array);
		sharedMesh.SetIndices(array4, MeshTopology.Quads, 0);
		sharedMesh.SetColors(array2);
		sharedMesh.SetUVs(0, array3);
		sharedMesh.RecalculateBounds();
	}
}
