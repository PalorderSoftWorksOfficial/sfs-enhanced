using System;
using UnityEngine;

namespace SFS.UI.ModGUI;

public class Toggle : GUIElement
{
	public ToggleButton toggleButton;

	[Obsolete("Size change not recommended for toggle. Please use scaling.")]
	public override Vector2 Size
	{
		get
		{
			return base.Size;
		}
		set
		{
			base.Size = value;
		}
	}

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		toggleButton = gameObject.GetComponent<ToggleButton>();
	}
}
