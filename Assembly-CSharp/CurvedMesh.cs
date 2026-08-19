using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CurvedMesh : BasicMeshData
{
	public float tail_InitialSlope;

	public float tail_InitialVelocity;

	public float tail_Acceleration;

	public float tail_Scale = 1f;

	public List<Vector2> GetCurveData(int surfaceCount)
	{
		Vector2 tail_InitialVelocityVector = new Vector2(1f, tail_InitialSlope).normalized * tail_InitialVelocity;
		int resolution = tail_Resolution - (int)Mathf.Log(surfaceCount, 1.8f);
		if (resolution < 5)
		{
			resolution = 5;
		}
		return new Vector2[resolution].Select(delegate(Vector2 _, int i)
		{
			float num = (float)i / (float)(resolution - 1);
			return new Vector2(tail_InitialVelocityVector.x * num, tail_InitialVelocityVector.y * num + tail_Acceleration * num * num) * tail_Scale;
		}).ToList();
	}

	public Func<float, float> SampleCurve()
	{
		Vector2 tail_InitialVelocityVector = new Vector2(1f, tail_InitialSlope).normalized * tail_InitialVelocity;
		return delegate(float x)
		{
			float num = x / (tail_InitialVelocityVector.x * tail_Scale);
			return (tail_InitialVelocityVector.y * num + tail_Acceleration * num * num) * tail_Scale;
		};
	}
}
