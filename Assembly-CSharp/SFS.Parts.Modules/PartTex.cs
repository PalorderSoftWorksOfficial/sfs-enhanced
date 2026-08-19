using UnityEngine;

namespace SFS.Parts.Modules;

public struct PartTex
{
	public Texture2D color;

	public Texture2D shape;

	public Texture2D shadow;

	public static readonly int ColorTexture = Shader.PropertyToID("_ColorTexture");

	public static readonly int ShapeTexture = Shader.PropertyToID("_ShapeTexture");

	public static readonly int ShadowTexture = Shader.PropertyToID("_ShadowTexture");
}
