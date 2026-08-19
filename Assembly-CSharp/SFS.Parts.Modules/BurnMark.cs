using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SFS.Parts.Modules;

public class BurnMark : MonoBehaviour
{
	[Serializable]
	public class Burn
	{
		public float angle;

		public float intensity;

		public float x;

		public Line2[] topSurfaces;

		public Line2[] bottomSurfaces;

		public Burn GetCopy()
		{
			return new Burn
			{
				angle = angle,
				intensity = intensity,
				x = x,
				topSurfaces = topSurfaces,
				bottomSurfaces = bottomSurfaces
			};
		}
	}

	[Serializable]
	public class BurnSave
	{
		public float angle;

		public float intensity;

		public float x;

		public string top;

		public string bottom;

		public BurnSave()
		{
		}

		public BurnSave(Burn burn)
		{
			angle = burn.angle;
			intensity = burn.intensity;
			x = burn.x;
			top = ToSurfaceSave(burn.topSurfaces);
			bottom = ToSurfaceSave(burn.bottomSurfaces);
		}

		private string ToSurfaceSave(Line2[] surfaces)
		{
			StringBuilder stringBuilder = new StringBuilder(surfaces.Length * 4 * 5);
			for (int i = 0; i < surfaces.Length; i++)
			{
				Line2 line = surfaces[i];
				stringBuilder.Append(Mathf.RoundToInt(line.start.x * 100f) + ",");
				stringBuilder.Append(Mathf.RoundToInt(line.start.y * 100f) + ",");
				stringBuilder.Append(Mathf.RoundToInt(line.end.x * 100f) + ",");
				stringBuilder.Append(Mathf.RoundToInt(line.end.y * 100f) + ",");
			}
			return stringBuilder.ToString();
		}

		public Burn FromSave()
		{
			return new Burn
			{
				angle = angle,
				intensity = intensity,
				x = x,
				topSurfaces = FromSurfaceSave(top),
				bottomSurfaces = FromSurfaceSave(bottom)
			};
		}

		private Line2[] FromSurfaceSave(string text)
		{
			string[] array = text.Split(',', StringSplitOptions.None);
			int num = array.Length - 1;
			int[] array2 = new int[num];
			for (int i = 0; i < num; i++)
			{
				array2[i] = int.Parse(array[i]);
			}
			Line2[] array3 = new Line2[array.Length / 4];
			for (int j = 0; j < array3.Length; j++)
			{
				array3[j] = new Line2(new Vector2((float)array2[j * 4] / 100f, (float)array2[j * 4 + 1] / 100f), new Vector2((float)array2[j * 4 + 2] / 100f, (float)array2[j * 4 + 3] / 100f));
			}
			return array3;
		}
	}

	private class MeshReference
	{
		public MeshRenderer renderer;

		public MeshFilter filter;

		public MeshReference(MeshRenderer renderer, MeshFilter filter)
		{
			this.renderer = renderer;
			this.filter = filter;
		}
	}

	private struct Bounds
	{
		public float xMin;

		public float xMax;

		public float yMin;

		public float yMax;
	}

	private const float margin = 0.3f;

	private const float textureWidth = 7f;

	private static readonly int Opacity = Shader.PropertyToID("_Opacity");

	private static readonly int OffsetTexture = Shader.PropertyToID("_Offset");

	public Burn burn;

	private MeshReference[] meshReferences;

	private Texture2D offsetTexture;

	private float opacity = 1f;

	public void Initialize()
	{
		meshReferences = (from a in GetComponentsInChildren<MeshRenderer>(includeInactive: true)
			select new MeshReference(a, a.GetComponent<MeshFilter>())).ToArray();
	}

	private void DrawSurfaces()
	{
		Matrix2x2 matrix2x = Matrix2x2.Angle((0f - (0f - burn.angle + 90f)) * ((float)Math.PI / 180f));
		Line2[] topSurfaces = burn.topSurfaces;
		for (int i = 0; i < topSurfaces.Length; i++)
		{
			Line2 line = topSurfaces[i];
			Debug.DrawLine(base.transform.TransformPoint(line.start * matrix2x), base.transform.TransformPoint(line.end * matrix2x), Color.red);
		}
		topSurfaces = burn.bottomSurfaces;
		for (int i = 0; i < topSurfaces.Length; i++)
		{
			Line2 line2 = topSurfaces[i];
			Debug.DrawLine(base.transform.TransformPoint(line2.start * matrix2x), base.transform.TransformPoint(line2.end * matrix2x), Color.yellow);
		}
	}

	public void SetBurn(Vector2 velocityDirection, Transform positionContext, float intensity, Line2[] topSurfaces_World, Line2[] bottomSurfaces_World, float opacity)
	{
		if (meshReferences == null)
		{
			Initialize();
		}
		for (int i = 0; i < bottomSurfaces_World.Length; i++)
		{
			Line2 line = bottomSurfaces_World[i];
			ref Vector2 start = ref line.start;
			ref Vector2 end = ref line.end;
			Vector2 end2 = line.end;
			Vector2 start2 = line.start;
			start = end2;
			end = start2;
			bottomSurfaces_World[i] = line;
		}
		bottomSurfaces_World = bottomSurfaces_World.Reverse().ToArray();
		Matrix2x2 toVelocityDirectionLocal = Matrix2x2.Angle(0f - (Mathf.Atan2(velocityDirection.y, velocityDirection.x) - (float)Math.PI / 2f));
		(float, float) cutBounds = GetCutBounds(toVelocityDirectionLocal);
		float item = cutBounds.Item1;
		float item2 = cutBounds.Item2;
		Vector2 vector = base.transform.InverseTransformVector(velocityDirection);
		float num = Mathf.Atan2(vector.y, vector.x) * 57.29578f;
		Matrix2x2 rotate = Matrix2x2.Angle((0f - num + 90f) * ((float)Math.PI / 180f));
		Line2[] topSurfaces = CutSurfaces(item, item2, toVelocityDirectionLocal, rotate, topSurfaces_World);
		Line2[] bottomSurfaces = CutSurfaces(item, item2, toVelocityDirectionLocal, rotate, bottomSurfaces_World);
		Vector2 vector2 = positionContext.InverseTransformVector(velocityDirection);
		float num2 = Mathf.Atan2(vector2.y, vector2.x) * 57.29578f;
		float x = (positionContext.InverseTransformPoint(base.transform.position) * Matrix2x2.Angle((0f - num2 + 90f) * ((float)Math.PI / 180f))).x;
		burn = new Burn
		{
			angle = num,
			intensity = intensity,
			x = x,
			topSurfaces = topSurfaces,
			bottomSurfaces = bottomSurfaces
		};
		this.opacity = opacity;
	}

	private (float, float) GetCutBounds(Matrix2x2 toVelocityDirectionLocal)
	{
		float num = float.MaxValue;
		float num2 = float.MinValue;
		MeshReference[] array = meshReferences;
		for (int i = 0; i < array.Length; i++)
		{
			MeshFilter filter = array[i].filter;
			Vector3[] vertices = filter.sharedMesh.vertices;
			foreach (Vector3 position in vertices)
			{
				float x = toVelocityDirectionLocal.GetX(filter.transform.TransformPoint(position));
				num = Mathf.Min(num, x);
				num2 = Mathf.Max(num2, x);
			}
		}
		return (num, num2);
	}

	private Line2[] CutSurfaces(float xMin, float xMax, Matrix2x2 toVelocityDirectionLocal, Matrix2x2 rotate, Line2[] surfaces_WorldCoords)
	{
		int i;
		for (i = 0; i < surfaces_WorldCoords.Length && toVelocityDirectionLocal.GetX(surfaces_WorldCoords[i].end) < xMin - 0.3f; i++)
		{
		}
		int num = surfaces_WorldCoords.Length - 1;
		while (num > 0 && toVelocityDirectionLocal.GetX(surfaces_WorldCoords[num].start) > xMax + 0.3f)
		{
			num--;
		}
		Line2[] array = new Line2[num - i + 1];
		for (int j = 0; j < array.Length; j++)
		{
			Line2 line = surfaces_WorldCoords[i + j];
			array[j] = new Line2(base.transform.InverseTransformPoint(line.start) * rotate, base.transform.InverseTransformPoint(line.end) * rotate);
		}
		return array;
	}

	public void ApplyEverything()
	{
		Matrix2x2 rotate = Matrix2x2.Angle((0f - burn.angle + 90f) * ((float)Math.PI / 180f));
		Vector2[][] rotatedVertices = GetRotatedVertices(rotate);
		Bounds bounds = GetBounds(rotatedVertices);
		ApplyUV(rotatedVertices, bounds);
		CreateTexture(bounds);
		ApplyTexture();
		ApplyIntensity();
	}

	private void ApplyUV(Vector2[][] rotatedVertices, Bounds bounds)
	{
		for (int i = 0; i < meshReferences.Length; i++)
		{
			MeshReference meshReference = meshReferences[i];
			Vector2[] array = rotatedVertices[i];
			Vector3[] array2 = new Vector3[array.Length];
			for (int j = 0; j < array.Length; j++)
			{
				array2[j] = new Vector3((burn.x + array[j].x) / 7f, array[j].y - bounds.yMin, Mathf.InverseLerp(bounds.xMin, bounds.xMax, array[j].x));
			}
			meshReference.filter.sharedMesh.SetUVs(4, array2);
		}
	}

	private void CreateTexture(Bounds bounds)
	{
		if (offsetTexture != null)
		{
			UnityEngine.Object.Destroy(offsetTexture);
		}
		float num = bounds.xMax - bounds.xMin;
		int num2 = (int)(num * 6f);
		offsetTexture = new Texture2D(num2, 1)
		{
			wrapMode = TextureWrapMode.Clamp
		};
		float num3 = num / (float)num2;
		for (int i = 0; i < num2; i++)
		{
			float X = bounds.xMin + num3 * (0.5f + (float)i);
			float num4 = GetHeightAtX(burn.topSurfaces);
			float num5 = num4 - bounds.yMin;
			float num6 = GetHeightAtX(burn.bottomSurfaces);
			float num7 = Mathf.Max(num4 - num6, 0.7f);
			offsetTexture.SetPixel(i, 0, new Color(num5 / 10f * 0.5f + 0.5f, num7 / 10f, 0f, 0f));
			float GetHeightAtX(Line2[] surfaces)
			{
				Line2[] array = surfaces;
				for (int j = 0; j < array.Length; j++)
				{
					Line2 line = array[j];
					if (X >= line.start.x && X <= line.end.x)
					{
						return line.GetHeightAtX_Unclamped(X);
					}
				}
				if (surfaces.Length == 0)
				{
					return float.MinValue;
				}
				Line2 line2 = surfaces[0];
				array = surfaces;
				for (int j = 0; j < array.Length; j++)
				{
					Line2 line3 = array[j];
					if (Mathf.Abs(line3.LerpUnclamped(0.5f).x - X) < Mathf.Abs(line2.LerpUnclamped(0.5f).x - X))
					{
						line2 = line3;
					}
				}
				return line2.GetHeightAtX(X);
			}
		}
		offsetTexture.Apply();
	}

	private void ApplyTexture()
	{
		SetPropertyBlock(delegate(MaterialPropertyBlock a)
		{
			a.SetTexture(OffsetTexture, offsetTexture);
		});
	}

	public void SetOpacity(float opacity, bool forceApply)
	{
		if (opacity != this.opacity || forceApply)
		{
			this.opacity = opacity;
			ApplyIntensity();
		}
	}

	private void ApplyIntensity()
	{
		float newIntensity = burn.intensity * opacity;
		SetPropertyBlock(delegate(MaterialPropertyBlock a)
		{
			a.SetFloat(Opacity, newIntensity * 1.2f);
		});
	}

	private void SetPropertyBlock(Action<MaterialPropertyBlock> setData)
	{
		MeshReference[] array = meshReferences;
		foreach (MeshReference meshReference in array)
		{
			for (int j = 0; j < meshReference.renderer.materials.Length; j++)
			{
				MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
				if (meshReference.renderer.HasPropertyBlock())
				{
					meshReference.renderer.GetPropertyBlock(materialPropertyBlock, j);
				}
				setData(materialPropertyBlock);
				meshReference.renderer.SetPropertyBlock(materialPropertyBlock, j);
			}
		}
	}

	private Vector2[][] GetRotatedVertices(Matrix2x2 rotate)
	{
		Vector2[][] array = new Vector2[meshReferences.Length][];
		for (int i = 0; i < meshReferences.Length; i++)
		{
			MeshFilter filter = meshReferences[i].filter;
			Vector3[] vertices = filter.sharedMesh.vertices;
			Vector2[] array2 = new Vector2[vertices.Length];
			for (int j = 0; j < vertices.Length; j++)
			{
				array2[j] = base.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[j])) * rotate;
			}
			array[i] = array2;
		}
		return array;
	}

	private static Bounds GetBounds(Vector2[][] vertices)
	{
		float num = float.MaxValue;
		float num2 = float.MinValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		foreach (Vector2[] array in vertices)
		{
			for (int j = 0; j < array.Length; j++)
			{
				Vector2 vector = array[j];
				num = Mathf.Min(num, vector.x);
				num2 = Mathf.Max(num2, vector.x);
				num3 = Mathf.Min(num3, vector.y);
				num4 = Mathf.Max(num4, vector.y);
			}
		}
		return new Bounds
		{
			xMin = num,
			xMax = num2,
			yMin = num3,
			yMax = num4
		};
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(offsetTexture);
	}

	public float GetAngleRadWorld()
	{
		Vector2 vector = new Vector2(Mathf.Cos(burn.angle), Mathf.Sin(burn.angle));
		Vector2 vector2 = base.transform.TransformVector(vector);
		return Mathf.Atan2(vector2.y, vector2.x);
	}
}
