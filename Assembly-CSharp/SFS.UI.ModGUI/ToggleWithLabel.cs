using UnityEngine;

namespace SFS.UI.ModGUI;

public class ToggleWithLabel : GUIElement
{
	public Toggle toggle;

	public Label label;

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		toggle = new Toggle();
		toggle.Init(rectTransform.Find("Toggle").gameObject, rectTransform);
		label = new Label();
		label.Init(rectTransform.Find("Label").gameObject, rectTransform);
	}
}
