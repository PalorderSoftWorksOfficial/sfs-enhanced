using UnityEngine;

namespace SFS.UI;

public static class ImageTools
{
	public static Texture2D RenderTextureTo2D(RenderTexture rTex, int width, int height)
	{
		Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: true);
		RenderTexture.active = rTex;
		texture2D.ReadPixels(new Rect(0f, 0f, rTex.width, rTex.height), 0, 0);
		texture2D.Apply();
		return texture2D;
	}

	public static byte[] RenderTextureToPng(RenderTexture rTex, int width, int height)
	{
		return RenderTextureTo2D(rTex, width, height).EncodeToPNG();
	}

	public static Texture2D PngTo2D(byte[] imageData)
	{
		Texture2D texture2D = new Texture2D(2, 2);
		texture2D.LoadImage(imageData);
		return texture2D;
	}
}
