using System.Collections.Generic;
using UnityEngine;

public class VertexChain : List<Vector2>
{
	public VertexChain()
	{
	}

	public VertexChain(int capacity)
		: base(capacity)
	{
	}

	public VertexChain(IEnumerable<Vector2> vertices)
	{
		AddRange(vertices);
	}

	public void ForceCounterClockWise()
	{
		if (base.Count >= 3 && !IsCounterClockWise())
		{
			Reverse();
		}
	}

	public float GetSignedArea()
	{
		if (base.Count < 3)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < base.Count; i++)
		{
			int index = (i + 1) % base.Count;
			Vector2 vector = base[i];
			Vector2 vector2 = base[index];
			num += vector.x * vector2.y;
			num -= vector.y * vector2.x;
		}
		return num / 2f;
	}

	public bool IsCounterClockWise()
	{
		if (base.Count < 3)
		{
			return false;
		}
		return GetSignedArea() > 0f;
	}

	public int NextIndex(int index)
	{
		if (index + 1 <= base.Count - 1)
		{
			return index + 1;
		}
		return 0;
	}

	public Vector2 NextVertex(int index)
	{
		return base[NextIndex(index)];
	}

	public int PreviousIndex(int index)
	{
		if (index - 1 >= 0)
		{
			return index - 1;
		}
		return base.Count - 1;
	}

	public Vector2 PreviousVertex(int index)
	{
		return base[PreviousIndex(index)];
	}
}
