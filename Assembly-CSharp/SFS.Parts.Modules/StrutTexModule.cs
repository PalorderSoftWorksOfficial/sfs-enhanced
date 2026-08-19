using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class StrutTexModule : MonoBehaviour, I_InitializePartModule
{
	public ColorTexture reference;

	public Float_Reference input;

	public Float_Reference output;

	public int Priority => 12;

	public void Initialize()
	{
		input.OnChange += new Action(Calculate);
	}

	private void Calculate()
	{
		PartTexture colorTex = reference.colorTex;
		float num = (float)colorTex.textures[0].texture.height / (float)colorTex.textures[0].texture.width;
		float num2 = input.Value / num;
		float num3 = colorTex.border_Bottom.uvSize + colorTex.border_Top.uvSize;
		float num4 = 1f - num3;
		int num5 = Mathf.RoundToInt((num2 - num3) / num4 + 0.25f);
		output.Value = ((float)num5 * num4 + num3) * num;
		base.transform.localScale = new Vector2(1f, input.Value / output.Value);
	}
}
