using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class TemperatureTest : MonoBehaviour
{
	public ReentryData[] recordings;

	public AeroFormula formula;

	private void Update()
	{
		if (!Application.isEditor)
		{
			return;
		}
		for (int i = 0; i < recordings.Length; i++)
		{
			Vector3 pos = base.transform.position + Vector3.down * (i * 10);
			ReentryData recording = recordings[i];
			Draw(recording.points.Select((Point a) => (float)a.density * 1000f / 2f), Color.blue);
			(float, float, float, float)[] source = recording.points.Select(delegate(Point a)
			{
				formula.GetEverything(a.velocity, a.velocity_Y, a.density, (float)recording.startHeatingVelocityMultiplier, recording.shockwaveM, out var Q, out var shockOpacity, out var temperature, out var pure);
				return (Q: Q, shockOpacity: shockOpacity, temperature: temperature, pure: pure);
			}).ToArray();
			Draw(source.Select(((float Q, float shockOpacity, float temperature, float pure) a) => a.Q / 20f), Color.cyan);
			Draw(source.Select(((float Q, float shockOpacity, float temperature, float pure) a) => a.temperature / 1000f), Color.red);
			void Draw(IEnumerable<float> a, Color c)
			{
				float[] array = a.ToArray();
				for (int j = 0; j < array.Length - 1; j++)
				{
					Debug.DrawLine(pos + new Vector3((float)j / 30f, array[j]), pos + new Vector3((float)(j + 1) / 30f, array[j + 1]), c);
				}
			}
		}
	}
}
