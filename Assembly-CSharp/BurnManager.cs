using System;
using System.Collections.Generic;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World.Drag;
using UnityEngine;

public class BurnManager : MonoBehaviour
{
	public PartHolder partHolder;

	private float currentMarkOpacity = 1f;

	public void ApplyBurnMarks(List<Surface> topSurfaces, float temperature, Matrix2x2 localToWorld, float velocityAngleRad, int frameIndex)
	{
		float num = Mathf.Round((1f - Mathf.InverseLerp(800f, 2500f, temperature)) * 20f) / 20f;
		float num2 = AeroModule.GetIntensity(temperature - 400f, 600f) * 1.5f;
		if (num2 > 0.1f)
		{
			BurnMark burn = GetBurn(frameIndex, out var newBurnMark);
			bool flag;
			if (newBurnMark || num2 - 0.05f > burn.burn.intensity)
			{
				flag = true;
			}
			else if (num2 > burn.burn.intensity * 0.7f && Mathf.Abs(Mathf.DeltaAngle(burn.GetAngleRadWorld() * 57.29578f, velocityAngleRad * 57.29578f)) > 20f)
			{
				flag = true;
				num2 = burn.burn.intensity;
			}
			else
			{
				flag = false;
			}
			if (flag)
			{
				float num3 = velocityAngleRad + (float)Math.PI;
				List<Surface> exposedSurfaces = AeroModule.GetExposedSurfaces(Aero_Rocket.GetDragSurfaces(partHolder, Matrix2x2.Angle(0f - num3)));
				Line2[] topSurfaces_World = AeroModule.RotateSurfaces(topSurfaces, localToWorld);
				Line2[] bottomSurfaces_World = AeroModule.RotateSurfaces(exposedSurfaces, Matrix2x2.Angle(num3));
				bool num4 = burn.burn == null;
				burn.SetBurn(Vector2.up * localToWorld, partHolder.transform, num2, topSurfaces_World, bottomSurfaces_World, num);
				burn.ApplyEverything();
				if (num4)
				{
					BaseMesh[] modules = partHolder.parts[frameIndex % partHolder.parts.Count].GetModules<BaseMesh>();
					for (int i = 0; i < modules.Length; i++)
					{
						modules[i].GenerateMesh();
					}
				}
			}
		}
		if (num != currentMarkOpacity)
		{
			currentMarkOpacity = num;
			ApplyOpacity();
		}
	}

	public void ApplyOpacity()
	{
		foreach (Part part in partHolder.parts)
		{
			if (part.burnMark != null)
			{
				part.burnMark.SetOpacity(currentMarkOpacity, forceApply: false);
			}
		}
	}

	private BurnMark GetBurn(int frameIndex, out bool newBurnMark)
	{
		List<Part> parts = partHolder.parts;
		Part part = parts[frameIndex % parts.Count];
		newBurnMark = part.burnMark == null;
		if (newBurnMark)
		{
			part.burnMark = part.gameObject.AddComponent<BurnMark>();
		}
		return part.burnMark;
	}
}
