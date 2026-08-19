using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.Career;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class PolygonMesh : BaseMesh, I_InitializePartModule
{
	public enum UVOptions
	{
		Auto,
		Two_Points,
		Four_Points
	}

	public enum ColorType
	{
		Module,
		Local
	}

	public PolygonData polygonModule;

	public UVOptions UV_Mode;

	public float grid;

	[HideInInspector]
	public Vector2[] bounds;

	public BasicTexture texture;

	public ColorType type;

	public Color colorBasic = Color.white;

	public ColorModule colorModule;

	private bool initialized;

	int I_InitializePartModule.Priority => 0;

	private void Reset()
	{
		type = ColorType.Local;
	}

	void I_InitializePartModule.Initialize()
	{
		polygonModule.onChange += new Action(GenerateMesh);
		polygonModule.SubscribeToComposedDepth(GenerateMesh);
		initialized = true;
		GenerateMesh();
	}

	public override void GenerateMesh()
	{
		bool flag = GameManager.main == null && BuildManager.main == null && HubManager.main == null;
		if (!initialized && Application.isPlaying && !flag)
		{
			return;
		}
		if (!Application.isPlaying | flag)
		{
			polygonModule.Output();
		}
		List<Vector2> list = new List<Vector2>(polygonModule.polygon.vertices);
		Line2[] uV_Channels = Get_UV_Channels();
		Vector3[] array = new Vector3[list.Count];
		List<int> list2 = new List<int>();
		Vector2[] array2 = new Vector2[list.Count];
		foreach (ConvexPolygon item2 in PolygonPartioner.Partion(list.ToArray()))
		{
			if (item2.points.Length != 0)
			{
				int item = list.IndexOf(item2.points[0]);
				for (int i = 1; i < item2.points.Length - 1; i++)
				{
					list2.Add(item);
					list2.Add(list.IndexOf(item2.points[i]));
					list2.Add(list.IndexOf(item2.points[i + 1]));
				}
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			Vector2 vector = list[j];
			array[j] = vector;
			array2[j] = UV_Utility.UV(vector, bounds);
		}
		List<Vector3>[] array3 = new List<Vector3>[uV_Channels.Length];
		for (int k = 0; k < uV_Channels.Length; k++)
		{
			array3[k] = new List<Vector3>();
			for (int l = 0; l < array.Length; l++)
			{
				Vector2 vector2 = uV_Channels[k].LerpUnclamped(array2[l].x, array2[l].y);
				array3[k].Add(new Vector3(vector2.x, vector2.y, 1f));
			}
		}
		List<PartTex> textures = new List<PartTex>
		{
			new PartTex
			{
				color = texture.colorTex.texture,
				shape = texture.shapeTex.texture,
				shadow = texture.shadowTex.texture
			}
		};
		ApplyMeshData(array.ToList(), list2.ToArray(), array3, GetColors(array.Length), GetShading(array.Length), GetDepths(array.Length), polygonModule.BaseDepth, 0f, textures, MeshTopology.Triangles);
	}

	private Line2[] Get_UV_Channels()
	{
		return new Line2[3]
		{
			texture.colorTex.Get_Rect(base.transform),
			texture.shapeTex.Get_Rect(base.transform),
			texture.shadowTex.Get_Rect(base.transform)
		};
	}

	private Color[] GetColors(int verticeCount)
	{
		Color color = ((type == ColorType.Module && colorModule != null) ? colorModule.GetColor() : colorBasic);
		Color[] array = new Color[verticeCount];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = color;
		}
		return array;
	}

	private static List<Vector3> GetShading(int vertexCount)
	{
		List<Vector3> list = new List<Vector3>(vertexCount);
		for (int i = 0; i < vertexCount; i++)
		{
			list.Add(new Vector3(0f, 0f, 1f));
		}
		return list;
	}

	private static List<Vector3> GetDepths(int vertexCount)
	{
		List<Vector3> list = new List<Vector3>(vertexCount);
		for (int i = 0; i < vertexCount; i++)
		{
			list.Add(Vector3.one);
		}
		return list;
	}
}
