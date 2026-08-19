using UnityEngine;

namespace SFS.Parts;

[CreateAssetMenu]
public class ShadowTexture : TextureAssetBase
{
	public PartTexture texture;

	protected override PartTexture Texture => texture;
}
