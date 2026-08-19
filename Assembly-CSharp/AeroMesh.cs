using System;
using System.Collections.Generic;
using System.Linq;
using SFS;
using SFS.World.Drag;
using UnityEngine;

public class AeroMesh : MonoBehaviour
{
	public struct Data
	{
		public float velocityAngle_Rad;

		public Matrix2x2 localToWorld;

		public BasicMeshData data;

		public List<Vector2> curveData;

		public Func<float, float> sampleCurve;
	}

	private static readonly int Temperature = Shader.PropertyToID("_Temperature");

	private static readonly int Opacity = Shader.PropertyToID("_Opacity");

	private const float SlopeReduction = 0.8f;

	public MeshFilter meshFilter;

	public MeshRenderer meshRenderer;

	public bool _drawDebug;

	private void Start()
	{
		meshFilter.mesh = new Mesh();
		meshRenderer.sortingLayerName = "Default";
		meshRenderer.sortingOrder = 10;
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(meshFilter.mesh);
	}

	public void SetShockOpacity(float shockOpacity)
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(Opacity, shockOpacity);
		meshRenderer.SetPropertyBlock(materialPropertyBlock);
	}

	public void SetTemperature(float strength)
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(Temperature, strength);
		meshRenderer.SetPropertyBlock(materialPropertyBlock);
	}

	public void GenerateMesh(Data A, List<Surface> exposedSurfaces)
	{
		if (!meshRenderer.enabled)
		{
			return;
		}
		List<Vector2> curveData = A.curveData;
		BasicMeshData data = A.data;
		for (int i = 0; i < curveData.Count - 1; i++)
		{
			if (curveData[i + 1].y > data.top_Fade)
			{
				curveData.Insert(i + 1, new Line2(curveData[i], curveData[i + 1]).GetPositionAtY(data.top_Fade));
				break;
			}
		}
		List<List<Vector2>> points = GetPoints(exposedSurfaces);
		List<float>[] m = GetM(points, A.sampleCurve, Line2.GetSlope_Abs(curveData[0], curveData[1]), A.data);
		(float, float)[] fadeXGroups = SideFade_Insert(points, m, data);
		Vector2[][][] vertices = GetVertices(curveData, points, m);
		GenerateMesh(A, vertices, fadeXGroups);
	}

	private void GenerateMesh(Data A, Vector2[][][] verticeGroups, (float, float)[] fadeXGroups)
	{
		int count = A.curveData.Count;
		float uv_RandomOffset = (A.data.randomizeHorizontalUV ? UnityEngine.Random.value : 0f);
		float height = A.curveData.Last().y - A.data.top_Fade;
		float[] array = new float[count].Select(Get_UV_Y).ToArray();
		int num = verticeGroups.Sum((Vector2[][] points) => points[0].Length - 1) * (count - 1) * 4;
		Vector3[] array2 = new Vector3[num];
		Color[] array3 = new Color[num];
		Vector3[] array4 = new Vector3[num];
		int[] array5 = new int[num];
		int num2 = 0;
		for (int num3 = 0; num3 < verticeGroups.Length; num3++)
		{
			Vector2[][] array6 = verticeGroups[num3];
			float[] array7 = array6[count / 2].Select((Vector2 p) => p.x / A.data.textureWidth + uv_RandomOffset).ToArray();
			Vector2[] pointGroup = array6[0];
			(float, float) tuple = fadeXGroups[num3];
			float fadeX_Left = tuple.Item1;
			float fadeX_Right = tuple.Item2;
			float[] array8 = array6.First().Select(delegate(Vector2 p)
			{
				float a3 = Mathf.InverseLerp(pointGroup.First().x, fadeX_Left, p.x);
				float b = Mathf.InverseLerp(pointGroup.Last().x, fadeX_Right, p.x);
				return Mathf.Min(a3, b);
			}).ToArray();
			for (int num4 = 0; num4 < array6.Length - 1; num4++)
			{
				Vector2[] array9 = array6[num4];
				Vector2[] array10 = array6[num4 + 1];
				float y = array[num4];
				float y2 = array[num4 + 1];
				float a = Mathf.Clamp01(A.curveData[num4].y / A.data.top_Fade);
				float a2 = Mathf.Clamp01(A.curveData[num4 + 1].y / A.data.top_Fade);
				for (int num5 = 0; num5 < array9.Length - 1; num5++)
				{
					array2[num2] = array9[num5];
					array2[num2 + 1] = array9[num5 + 1];
					array2[num2 + 2] = array10[num5 + 1];
					array2[num2 + 3] = array10[num5];
					float[] quadM = UV_Utility.GetQuadM(array2[num2], array2[num2 + 1], array2[num2 + 2], array2[num2 + 3]);
					array4[num2] = new Vector3(array7[num5], y, 1f) * quadM[0];
					array4[num2 + 1] = new Vector3(array7[num5 + 1], y, 1f) * quadM[1];
					array4[num2 + 2] = new Vector3(array7[num5 + 1], y2, 1f) * quadM[2];
					array4[num2 + 3] = new Vector3(array7[num5], y2, 1f) * quadM[3];
					array3[num2] = new Color(0f, 0f, array8[num5], a);
					array3[num2 + 1] = new Color(0f, 0f, array8[num5 + 1], a);
					array3[num2 + 2] = new Color(0f, 0f, array8[num5 + 1], a2);
					array3[num2 + 3] = new Color(0f, 0f, array8[num5], a2);
					num2 += 4;
				}
			}
		}
		for (int num6 = 0; num6 < num; num6++)
		{
			array5[num6] = num6;
		}
		Mesh mesh = meshFilter.mesh;
		mesh.Clear();
		mesh.SetVertices(array2);
		mesh.SetIndices(array5, MeshTopology.Quads, 0);
		mesh.SetColors(array3);
		mesh.SetUVs(0, array4);
		mesh.RecalculateBounds();
		base.transform.position = new Vector2(0f, A.data.top_Move) * A.localToWorld;
		base.transform.eulerAngles = new Vector3(0f, 0f, A.velocityAngle_Rad * 57.29578f);
		float Get_UV_Y(float none, int i)
		{
			return (A.curveData[i].y - (A.data.startTexAfterTopFade ? A.data.top_Fade : 0f)) / height;
		}
	}

	private static List<List<Vector2>> GetPoints(List<Surface> exposedSurfaces)
	{
		if (exposedSurfaces.Count == 0)
		{
			return new List<List<Vector2>>();
		}
		List<List<Vector2>> list = new List<List<Vector2>>
		{
			new List<Vector2>
			{
				exposedSurfaces[0].line.start,
				exposedSurfaces[0].line.end
			}
		};
		int num = 0;
		for (int i = 1; i < exposedSurfaces.Count; i++)
		{
			Line2 line = exposedSurfaces[i].line;
			List<Vector2> list2 = list[num];
			if ((list2.Last() - line.start).sqrMagnitude < 0.0009f)
			{
				list2.Add(line.end);
				continue;
			}
			list.Add(new List<Vector2> { line.start, line.end });
			num++;
		}
		return list;
	}

	private List<float>[] GetM(List<List<Vector2>> pointGroups, Func<float, float> sampleCurve, float curveSlope, BasicMeshData data)
	{
		List<float>[] array = new List<float>[pointGroups.Count];
		for (int i = 0; i < pointGroups.Count; i++)
		{
			List<Vector2> list = pointGroups[i];
			int num = list.Count - 1;
			List<(float, float)> list2 = new List<(float, float)>(new(float, float)[num]);
			for (int j = 0; j < num; j++)
			{
				float num2 = 0f - Line2.GetSlope(list[j], list[j + 1]);
				float num3 = curveSlope / num2 * 0.8f;
				list2[j] = ((num3 < 0f) ? num3 : (-1f), (num3 > 0f) ? num3 : 1f);
			}
			if (data.skipSurfaces)
			{
				if (_drawDebug)
				{
					for (int k = 0; k < list.Count - 1; k++)
					{
						Debug.DrawLine(list[k], list[k + 1], new Color(0f, 0f, 0f, 0.5f));
					}
				}
				for (int num4 = list.Count - 2; num4 >= 1; num4--)
				{
					Vector2 vector = list[num4];
					float num5 = Mathf.Clamp01(0f - list2[num4].Item1 - 0.2f);
					bool flag = false;
					for (int num6 = num4 - 1; num6 >= 0; num6--)
					{
						Vector2 vector2 = list[num6];
						if (vector2.y > vector.y - sampleCurve((vector.x - vector2.x) / num5))
						{
							flag = true;
							break;
						}
					}
					if (_drawDebug)
					{
						for (int l = 0; l < 50; l++)
						{
							Debug.DrawLine(vector + new Vector2((float)(-l) / 10f * num5, 0f - sampleCurve((float)l / 10f)), vector + new Vector2((float)(-(l + 1)) / 10f * num5, 0f - sampleCurve((float)(l + 1) / 10f)), flag ? Color.red : Color.yellow);
						}
					}
					if (!flag)
					{
						list.RemoveRange(0, num4);
						list2.RemoveRange(0, num4);
						break;
					}
				}
				int count = list.Count;
				for (int m = 1; m < count - 1; m++)
				{
					Vector2 vector3 = list[m];
					float num7 = Mathf.Clamp01(list2[m - 1].Item2 - 0.2f);
					bool flag2 = false;
					for (int n = m + 1; n < count; n++)
					{
						Vector2 vector4 = list[n];
						if (vector4.y > vector3.y - sampleCurve((vector4.x - vector3.x) / num7))
						{
							flag2 = true;
							break;
						}
					}
					if (_drawDebug)
					{
						for (int num8 = 0; num8 < 50; num8++)
						{
							Debug.DrawLine(vector3 + new Vector2((float)num8 / 10f * num7, 0f - sampleCurve((float)num8 / 10f)), vector3 + new Vector2((float)(num8 + 1) / 10f * num7, 0f - sampleCurve((float)(num8 + 1) / 10f)), flag2 ? Color.red : Color.yellow);
						}
					}
					if (!flag2)
					{
						int count2 = count - m - 1;
						list.RemoveRange(m + 1, count2);
						list2.RemoveRange(m, count2);
						break;
					}
				}
				if (_drawDebug)
				{
					for (int num9 = 0; num9 < list.Count - 1; num9++)
					{
						Debug.DrawLine(list[num9], list[num9 + 1]);
					}
				}
			}
			int count3 = list2.Count;
			for (int num10 = 1; num10 < count3; num10++)
			{
				list2[num10] = (Mathf.Max(list2[num10].Item1, list2[num10 - 1].Item1), list2[num10].Item2);
			}
			for (int num11 = list2.Count - 2; num11 >= 0; num11--)
			{
				list2[num11] = (list2[num11].Item1, Mathf.Min(list2[num11].Item2, list2[num11 + 1].Item2));
			}
			float start = list.First().x;
			float end = list.Last().x;
			float m_Left = Mathf.Max((!data.reduceIfBelow || i == 0 || pointGroups[i - 1].Last().y < list.First().y) ? (-1f) : (-0.3f), list2.First().Item1);
			float m_Right = Mathf.Min((!data.reduceIfBelow || i == pointGroups.Count - 1 || pointGroups[i + 1].Last().y < list.First().y) ? 1f : 0.3f, list2.Last().Item2);
			int count4 = list.Count;
			List<float> list3 = new List<float>(new float[count4]);
			for (int num12 = 0; num12 < count4; num12++)
			{
				float value = GetM(list[num12].x);
				list3[num12] = Mathf.Clamp(value, (num12 < count4 - 1) ? list2[num12].Item1 : (-1f), (num12 > 0) ? list2[num12 - 1].Item2 : 1f);
			}
			array[i] = list3;
			float GetM(float x)
			{
				return Mathf.Lerp(m_Left, m_Right, Mathf.InverseLerp(start, end, x));
			}
		}
		return array;
	}

	private static (float, float)[] SideFade_Insert(List<List<Vector2>> pointGroups, List<float>[] mGroups, BasicMeshData data)
	{
		(float, float)[] array = new(float, float)[pointGroups.Count];
		for (int i = 0; i < pointGroups.Count; i++)
		{
			List<Vector2> list = pointGroups[i];
			List<float> list2 = mGroups[i];
			if (data.extend)
			{
				array[i] = (list[0].x, list[list.Count - 1].x);
				Vector2 vector = list[1] - list[0];
				list.Insert(0, list[0] - new Vector2(vector.x, Mathf.Max(vector.y, 0f)) / vector.magnitude * data.side_FadeX);
				list2.Insert(0, Mathf.LerpUnclamped(list2[0], list2[1], 0f - Mathf.Min(data.side_FadeX / vector.magnitude, 0.5f)));
				Vector2 vector2 = list[list.Count - 1] - list[list.Count - 2];
				list.Add(list[list.Count - 1] + new Vector2(vector2.x, Mathf.Min(vector2.y, 0f)) / vector2.magnitude * data.side_FadeX);
				list2.Add(Mathf.LerpUnclamped(list2[list2.Count - 2], list2[list2.Count - 1], 1f + Mathf.Min(data.side_FadeX / vector2.magnitude, 0.5f)));
				continue;
			}
			float num = list2.First() + data.side_FadeM;
			float num2 = list.First().x + data.side_FadeX;
			for (int j = 0; j < list.Count - 1; j++)
			{
				if (list2[j + 1] > num || list[j + 1].x > num2)
				{
					float a = Mathf.InverseLerp(list2[j], list2[j + 1], num);
					float b = Mathf.InverseLerp(list[j].x, list[j + 1].x, num2);
					float t = Mathf.Min(a, b);
					Vector2 item = Vector2.Lerp(list[j], list[j + 1], t);
					list.Insert(j + 1, item);
					list2.Insert(j + 1, Mathf.Lerp(list2[j], list2[j + 1], t));
					array[i].Item1 = item.x;
					break;
				}
			}
			float num3 = list2.Last() - data.side_FadeM;
			float num4 = list.Last().x - data.side_FadeX;
			for (int num5 = list.Count - 1; num5 >= 1; num5--)
			{
				if (list2[num5 - 1] < num3 || list[num5 - 1].x < num4)
				{
					float a2 = Mathf.InverseLerp(list2[num5 - 1], list2[num5], num3);
					float b2 = Mathf.InverseLerp(list[num5 - 1].x, list[num5].x, num4);
					float t2 = Mathf.Max(a2, b2);
					Vector2 item2 = Vector2.Lerp(list[num5 - 1], list[num5], t2);
					list.Insert(num5, item2);
					list2.Insert(num5, Mathf.Lerp(list2[num5 - 1], list2[num5], t2));
					array[i].Item2 = item2.x;
					break;
				}
			}
		}
		return array;
	}

	private static Vector2[][][] GetVertices(List<Vector2> curveData, List<List<Vector2>> pointGroups, List<float>[] M)
	{
		Vector2[][][] array = new Vector2[pointGroups.Count][][];
		for (int i = 0; i < pointGroups.Count; i++)
		{
			List<Vector2> list = pointGroups[i];
			List<float> list2 = M[i];
			int count = list.Count;
			int count2 = curveData.Count;
			Vector2[][] array2 = new Vector2[count2][];
			for (int j = 0; j < count2; j++)
			{
				Vector2 vector = curveData[j];
				Vector2[] array3 = new Vector2[count];
				for (int k = 0; k < count; k++)
				{
					array3[k] = list[k] + new Vector2(vector.x * list2[k], 0f - vector.y);
				}
				array2[j] = array3;
			}
			array[i] = array2;
		}
		return array;
	}
}
