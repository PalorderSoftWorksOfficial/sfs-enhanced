using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class IconButton : MonoBehaviour
{
	public Image icon;

	public Button button;

	public TextAdapter text;

	public bool Show
	{
		set
		{
			if (button.gameObject.activeSelf != value)
			{
				button.gameObject.SetActive(value);
			}
		}
	}

	public Sprite Icon
	{
		set
		{
			if (icon != null)
			{
				icon.sprite = value;
			}
		}
	}

	public string Text
	{
		set
		{
			text.Text = value;
		}
	}
}
