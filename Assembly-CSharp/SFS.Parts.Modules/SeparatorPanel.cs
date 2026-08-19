using System;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class SeparatorPanel : MonoBehaviour, I_InitializePartModule
{
	public Float_Reference height;

	public Transform panel;

	public Transform arrow;

	public PipeMesh pipeMesh;

	public ColorTexture defaultTexture;

	int I_InitializePartModule.Priority => 0;

	void I_InitializePartModule.Initialize()
	{
		height.OnChange += new Action(Position);
		pipeMesh.onSetColorTexture += new Action(Enable);
		FlipToLight();
	}

	private void Position()
	{
		float num = ((height.Value > 0.7f) ? height.Value : 0f);
		panel.localPosition = new Vector2(0f, 0.25f + num);
		arrow.localPosition = new Vector2(0f, num);
	}

	private void Enable()
	{
		bool active = pipeMesh.textures.texture.colorTexture == defaultTexture;
		panel.gameObject.SetActive(active);
		arrow.gameObject.SetActive(active);
	}

	public void FlipToLight()
	{
		Vector2 lightDirection = GetLightDirection();
		float x = ((!(Vector2.Angle(base.transform.TransformVector(Vector2.left), lightDirection) < 90f)) ? 1 : (-1));
		float y = ((!(Vector2.Angle(base.transform.TransformVector(Vector2.up), lightDirection) > 90f)) ? 1 : (-1));
		panel.localScale = new Vector2(x, y);
	}

	private Vector2 GetLightDirection()
	{
		Vector2 vector = new Vector2(-1f, 1f);
		if (GameManager.main != null && base.transform.root.childCount > 0 && base.transform.root.GetChild(0).name == "Parts Holder")
		{
			return base.transform.root.GetChild(0).TransformDirection(vector);
		}
		return vector;
	}
}
