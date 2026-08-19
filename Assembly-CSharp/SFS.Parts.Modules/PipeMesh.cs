using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.Career;
using SFS.Variables;
using SFS.World;
using UnityEngine;
using UV;

namespace SFS.Parts.Modules;

public class PipeMesh : BaseMesh, I_InitializePartModule
{
	public PipeData pipeData;

	public Textures textures;

	[Space]
	public Colors colors;

	public bool leftCover;

	public bool centerCover;

	public bool rightCover;

	public bool separatorRing;

	public bool smoothShading;

	private bool initialized;

	public Event_Local onSetColorTexture;

	int I_InitializePartModule.Priority => 0;

	void I_InitializePartModule.Initialize()
	{
		if (textures.widthMode == Textures.WidthMode.Composed)
		{
			textures.width.OnChange += new Action(GenerateMesh);
		}
		pipeData.onChange += new Action(GenerateMesh);
		pipeData.SubscribeToComposedDepth(GenerateMesh);
		initialized = true;
		GenerateMesh();
	}

	public override void GenerateMesh()
	{
		bool flag = GameManager.main == null && BuildManager.main == null && HubManager.main == null;
		if (initialized || !Application.isPlaying || flag)
		{
			if (!Application.isPlaying | flag)
			{
				pipeData.Output();
			}
			Points_Splittable points_Splittable = new Points_Splittable(pipeData.pipe.points);
			UV_Splittable[] array = (from x in Get_UV_Channels(pipeData.pipe)
				select new UV_Splittable(x)).ToArray();
			Color_Splittable color_Splittable = new Color_Splittable(colors.GetOutput());
			List<Splittable> list = new List<Splittable>();
			list.Add(points_Splittable);
			list.AddRange(array);
			list.Add(color_Splittable);
			Splittable.Split(points_Splittable.GetSegment(), list.ToArray());
			MeshData meshData = new MeshData();
			GetMeshQuads(points_Splittable.elements, array.Select((UV_Splittable x) => x.element).ToArray(), color_Splittable.element, GetSlopeShading(points_Splittable.elements), meshData);
			ApplyMeshData(meshData.vertices, GetQuadIndices(points_Splittable.elements), meshData.UVs, meshData.colors.ToArray(), meshData.shading, meshData.depths, pipeData.BaseDepth, pipeData.depthMultiplier, meshData.textures, MeshTopology.Quads);
		}
	}

	private void GetMeshQuads(List<PipePoint> points, List<StartEnd_UV>[] vertical_UVs, Color_Channel color_Channel, List<Line> slopeShading, MeshData data)
	{
		for (int i = 0; i < points.Count - 1; i++)
		{
			PipePoint pipePoint = points[i];
			PipePoint pipePoint2 = points[i + 1];
			float magnitude = pipePoint.width.magnitude;
			float magnitude2 = pipePoint2.width.magnitude;
			Vector3[] collection = new Vector3[4]
			{
				pipePoint.GetPosition(pipePoint.cutLeft * 2f - 1f),
				pipePoint2.GetPosition(pipePoint2.cutLeft * 2f - 1f),
				pipePoint2.GetPosition(pipePoint2.cutRight * 2f - 1f),
				pipePoint.GetPosition(pipePoint.cutRight * 2f - 1f)
			};
			float[] quadM = UV_Utility.GetQuadM(pipePoint.GetPosition(-1f), pipePoint2.GetPosition(-1f), pipePoint2.GetPosition(1f), pipePoint.GetPosition(1f));
			Vector3[][] array = new Vector3[vertical_UVs.Length][];
			for (int j = 0; j < vertical_UVs.Length; j++)
			{
				StartEnd_UV startEnd_UV = vertical_UVs[j][i];
				if (!startEnd_UV.data.metalTexture)
				{
					if (leftCover && j == 0)
					{
						startEnd_UV.texture_UV.end.x = Mathf.Lerp(startEnd_UV.texture_UV.start.x, startEnd_UV.texture_UV.end.x, 0.05f);
					}
					if (centerCover && j == 0)
					{
						float x = Mathf.Lerp(startEnd_UV.texture_UV.start.x, startEnd_UV.texture_UV.end.x, 0.4f);
						float x2 = Mathf.Lerp(startEnd_UV.texture_UV.start.x, startEnd_UV.texture_UV.end.x, 0.6f);
						startEnd_UV.texture_UV.start.x = x;
						startEnd_UV.texture_UV.end.x = x2;
					}
					if (rightCover && j == 0)
					{
						startEnd_UV.texture_UV.start.x = Mathf.Lerp(startEnd_UV.texture_UV.start.x, startEnd_UV.texture_UV.end.x, 0.95f);
					}
				}
				if (separatorRing && j == 0)
				{
					startEnd_UV.texture_UV.end.y = startEnd_UV.texture_UV.start.y;
				}
				float t_X = (startEnd_UV.data.fixedWidth ? ((pipePoint.cutLeft - 0.5f) * pipePoint.width.magnitude / startEnd_UV.fixedWidthValue + 0.5f) : pipePoint.cutLeft);
				float t_X2 = (startEnd_UV.data.fixedWidth ? ((pipePoint2.cutLeft - 0.5f) * pipePoint2.width.magnitude / startEnd_UV.fixedWidthValue + 0.5f) : pipePoint2.cutLeft);
				float t_X3 = (startEnd_UV.data.fixedWidth ? ((pipePoint2.cutRight - 0.5f) * pipePoint2.width.magnitude / startEnd_UV.fixedWidthValue + 0.5f) : pipePoint2.cutRight);
				float t_X4 = (startEnd_UV.data.fixedWidth ? ((pipePoint.cutRight - 0.5f) * pipePoint.width.magnitude / startEnd_UV.fixedWidthValue + 0.5f) : pipePoint.cutRight);
				Line2 texture_UV = startEnd_UV.texture_UV;
				Line vertical_UV = startEnd_UV.vertical_UV;
				float[] array2 = (startEnd_UV.data.fixedWidth ? new float[4] { 1f, 1f, 1f, 1f } : quadM);
				array[j] = new Vector3[4]
				{
					texture_UV.LerpUnclamped(t_X, vertical_UV.start).ToVector3(1f) * array2[0],
					texture_UV.LerpUnclamped(t_X2, vertical_UV.end).ToVector3(1f) * array2[1],
					texture_UV.LerpUnclamped(t_X3, vertical_UV.end).ToVector3(1f) * array2[2],
					texture_UV.LerpUnclamped(t_X4, vertical_UV.start).ToVector3(1f) * array2[3]
				};
			}
			StartEnd_Color startEnd_Color = color_Channel.elements[i];
			Color[] collection2 = new Color[4]
			{
				startEnd_Color.color_Edge.start,
				startEnd_Color.color_Edge.end,
				startEnd_Color.color_Edge.end,
				startEnd_Color.color_Edge.start
			};
			Line line = slopeShading[i];
			Vector3[] collection3 = new Vector3[4]
			{
				new Vector3(line.start, 0f, 1f) * quadM[0],
				new Vector3(line.end, 0f, 1f) * quadM[1],
				new Vector3(line.end, 0f, 1f) * quadM[2],
				new Vector3(line.start, 0f, 1f) * quadM[3]
			};
			Vector3[] collection4 = new Vector3[4]
			{
				new Vector3(pipePoint.cutLeft, magnitude, 1f) * quadM[0],
				new Vector3(pipePoint2.cutLeft, magnitude2, 1f) * quadM[1],
				new Vector3(pipePoint2.cutRight, magnitude2, 1f) * quadM[2],
				new Vector3(pipePoint.cutRight, magnitude, 1f) * quadM[3]
			};
			data.vertices.AddRange(collection);
			data.UVs[0].AddRange(array[0]);
			data.UVs[1].AddRange(array[1]);
			data.UVs[2].AddRange(array[2]);
			data.colors.AddRange(collection2);
			data.shading.AddRange(collection3);
			data.depths.AddRange(collection4);
			data.textures.Add(new PartTex
			{
				color = vertical_UVs[0][i].texture,
				shape = vertical_UVs[1][i].texture,
				shadow = vertical_UVs[2][i].texture
			});
		}
	}

	private int[] GetQuadIndices(List<PipePoint> points)
	{
		if (points.Count == 0)
		{
			return new int[0];
		}
		int[] array = new int[(points.Count - 1) * 4];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i;
		}
		return array;
	}

	public void SetColorTexture(ColorTexture colorTexture)
	{
		if (!(textures.texture.colorTexture == colorTexture))
		{
			textures.texture.colorTexture = colorTexture;
			GenerateMesh();
			onSetColorTexture?.Invoke();
		}
	}

	public void SetShapeTexture(ShapeTexture shapeTexture)
	{
		if (!(textures.texture.shapeTexture == shapeTexture))
		{
			textures.texture.shapeTexture = shapeTexture;
			GenerateMesh();
		}
	}

	private List<StartEnd_UV>[] Get_UV_Channels(Pipe shape)
	{
		return textures.GetOutput(shape, base.transform, GetLightDirection());
	}

	private Vector2 GetLightDirection()
	{
		Vector2 vector = new Vector2(-1f * pipeData.depthMultiplier, 1f);
		if (GameManager.main != null && base.transform.root.childCount > 0 && base.transform.root.GetChild(0).name == "Parts Holder")
		{
			return base.transform.root.GetChild(0).TransformDirection(vector);
		}
		return vector;
	}

	private List<Line> GetSlopeShading(List<PipePoint> points)
	{
		float[] array = new float[points.Count - 1];
		for (int i = 0; i < points.Count - 1; i++)
		{
			array[i] = GetSlopeShade(points[i], points[i + 1]);
		}
		List<Line> list = new List<Line>();
		for (int j = 0; j < points.Count - 1; j++)
		{
			float start = array[(smoothShading && j > 0) ? (j - 1) : j];
			float end = array[j];
			list.Add(new Line(start, end));
		}
		return list;
	}

	private float GetSlopeShade(PipePoint point_A, PipePoint point_B)
	{
		float num = Mathf.Clamp((point_B.width.magnitude - point_A.width.magnitude) / (point_B.height - point_A.height), -0.8f, 0.8f);
		if (Vector2.Angle(base.transform.TransformVector(Vector2.up), new Vector2(-1f, 1f)) < 90f)
		{
			num = 0f - num;
		}
		return num * 0.2f * pipeData.depthMultiplier;
	}
}
