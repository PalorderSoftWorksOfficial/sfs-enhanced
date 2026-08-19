using UnityEngine;

namespace SFS.Parts.Modules;

public class PipePoint
{
	public Vector2 position;

	public Vector2 width;

	public float height;

	public float cutLeft;

	public float cutRight;

	public Vector2 Left => position - width / 2f;

	public Vector2 Right => position + width / 2f;

	public PipePoint(Vector2 position, Vector2 width, float height, float cutLeft, float cutRight)
	{
		this.position = position;
		this.width = width;
		this.height = height;
		this.cutLeft = cutLeft;
		this.cutRight = cutRight;
	}

	public Vector2 GetPosition(float t)
	{
		return position + width * (t * 0.5f);
	}

	public static PipePoint LerpByHeight(PipePoint a, PipePoint b, float height)
	{
		return Lerp(a, b, Mathf.InverseLerp(a.height, b.height, height));
	}

	private static PipePoint Lerp(PipePoint a, PipePoint b, float t)
	{
		return new PipePoint(Vector2.Lerp(a.position, b.position, t), Vector2.Lerp(a.width, b.width, t), Mathf.Lerp(a.height, b.height, t), Mathf.Lerp(a.cutLeft, b.cutLeft, t), Mathf.Lerp(a.cutRight, b.cutRight, t));
	}
}
