using System;

namespace SFS.Parts;

[Serializable]
public class BorderData
{
	public float uvSize;

	public VerticalSizeMode sizeMode;

	public float size = 0.5f;

	private bool Valid => uvSize > 0f;
}
