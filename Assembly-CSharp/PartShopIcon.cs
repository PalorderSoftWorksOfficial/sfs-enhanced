using SFS.Parts.Modules;
using SFS.UI;
using UnityEngine;
using UnityEngine.UI;

public class PartShopIcon : MonoBehaviour
{
	public TextAdapter partName;

	public RawImage partIcon;

	[Space]
	public VariantRef[] partVariants;

	public bool center = true;

	public bool customAspectRatio;

	public float maxAspectRatio = 1.5f;

	private void Start()
	{
		string text = partVariants[0].part.GetDisplayName();
		if (text == "Fuel Tank")
		{
			Variants.Variable[] changes = partVariants[0].GetVariant().changes;
			text = text + " " + changes[0].numberValue + "x" + changes[1].numberValue;
		}
		partName.Text = text;
		partIcon.texture = PartIconCreator.main.CreatePartIcon_TechTree(partVariants, 240, center, customAspectRatio ? maxAspectRatio : 1.5f);
		partIcon.rectTransform.sizeDelta = new Vector2((float)partIcon.texture.width / 2f, (float)partIcon.texture.height / 2f);
	}
}
