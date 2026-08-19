using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TranslucentImage;

public class TranslucentImage : Image, IMeshModifier
{
	public static TranslucentImageNode source;

	private Shader correctShader;

	private static int _blurTexPropId;

	private static int _cropRegionPropId;

	[Tooltip("Blend between the sprite and background blur")]
	[Range(0f, 1f)]
	public float spriteBlending = 0.65f;

	protected override void Start()
	{
		base.Start();
		PrepShader();
		if (source == null)
		{
			source = Object.FindObjectOfType<TranslucentImageNode>();
		}
		if (source != null)
		{
			material.SetTexture(_blurTexPropId, source.source.BlurredScreen);
		}
		if (base.canvas != null)
		{
			base.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
		}
	}

	public void PrepShader()
	{
		correctShader = Shader.Find("UI/TranslucentImage");
		_blurTexPropId = Shader.PropertyToID("_BlurTex");
		_cropRegionPropId = Shader.PropertyToID("_CropRegion");
	}

	private void LateUpdate()
	{
		if (source == null)
		{
			source = Object.FindObjectOfType<TranslucentImageNode>();
		}
		if (IsActive() && !(source == null) && !(source.source.BlurredScreen == null))
		{
			materialForRendering.SetTexture(_blurTexPropId, source.source.BlurredScreen);
			materialForRendering.SetVector(_cropRegionPropId, new Vector4(source.source.BlurRegion.xMin, source.source.BlurRegion.yMin, source.source.BlurRegion.xMax, source.source.BlurRegion.yMax));
		}
	}

	public virtual void ModifyMesh(VertexHelper vh)
	{
		List<UIVertex> list = new List<UIVertex>();
		vh.GetUIVertexStream(list);
		for (int i = 0; i < list.Count; i++)
		{
			UIVertex value = list[i];
			value.uv1 = new Vector2(spriteBlending, 0f);
			list[i] = value;
		}
		vh.Clear();
		vh.AddUIVertexTriangleStream(list);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		SetVerticesDirty();
	}

	protected override void OnDisable()
	{
		SetVerticesDirty();
		base.OnDisable();
	}

	protected override void OnDidApplyAnimationProperties()
	{
		SetVerticesDirty();
		base.OnDidApplyAnimationProperties();
	}

	public virtual void ModifyMesh(Mesh mesh)
	{
		using VertexHelper vertexHelper = new VertexHelper(mesh);
		ModifyMesh(vertexHelper);
		vertexHelper.FillMesh(mesh);
	}
}
