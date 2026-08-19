using System;
using UnityEngine;

namespace SFS.Parts;

[Serializable]
public class CenterData
{
	public enum CenterMode
	{
		Stretch,
		Logo,
		Tile
	}

	public CenterMode mode;

	public VerticalSizeMode sizeMode;

	public float size = 0.5f;

	[Range(0f, 1f)]
	public float logoHeightPercent = 0.5f;

	public bool scaleLogoToFit;

	private bool SizeOptions => mode != CenterMode.Stretch;
}
