using UnityEngine;

namespace SFS.UI.ModGUI;

public class ButtonWithLabel : GUIElement
{
	public Label label;

	public Button button;

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		label = new Label();
		label.Init(rectTransform.Find("Label").gameObject, rectTransform);
		button = new Button();
		button.Init(rectTransform.Find("Button").gameObject, rectTransform);
	}
}
