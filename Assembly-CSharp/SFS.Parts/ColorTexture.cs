using UnityEngine;

namespace SFS.Parts;

[CreateAssetMenu]
public class ColorTexture : TextureAssetBase
{
	public PartTexture colorTex;

	[Space]
	public string[] tags;

	public bool pack_Redstone_Atlas;

	protected override PartTexture Texture => colorTex;
}
