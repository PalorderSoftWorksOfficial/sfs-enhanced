using System;
using SFS.Parts;
using SFS.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Builds;

[Serializable]
public class SkinMenuButton : MonoBehaviour
{
	public SFS.UI.Button button;

	public Image image;

	public PartTexture texture;

	public void SetTexture(PartTexture texture)
	{
		if (this.texture != texture)
		{
			this.texture = texture;
			if (texture.icon != null)
			{
				image.sprite = texture.icon;
				return;
			}
			Texture2D texture2D = texture.textures[0].texture;
			Rect rect = new Rect(0f, 0f, texture2D.width, texture2D.height);
			image.sprite = Sprite.Create(texture2D, rect, Vector2.one * 0.5f);
		}
	}
}
