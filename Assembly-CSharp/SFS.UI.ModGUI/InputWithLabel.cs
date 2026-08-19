using UnityEngine;

namespace SFS.UI.ModGUI;

public class InputWithLabel : GUIElement
{
	public Label label;

	public TextInput textInput;

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		label = new Label();
		label.Init(rectTransform.Find("Label").gameObject, rectTransform);
		textInput = new TextInput();
		textInput.Init(rectTransform.Find("Input").gameObject, rectTransform);
	}
}
