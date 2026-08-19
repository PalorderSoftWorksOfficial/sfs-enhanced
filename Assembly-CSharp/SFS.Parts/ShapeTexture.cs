using UnityEngine;

namespace SFS.Parts;

[CreateAssetMenu]
public class ShapeTexture : TextureAssetBase
{
	public PartTexture shapeTex;

	[Space]
	public ShadowTexture shadowTex;

	public string[] tags;

	public bool pack_Redstone_Atlas;

	protected override PartTexture Texture => shapeTex;
}
