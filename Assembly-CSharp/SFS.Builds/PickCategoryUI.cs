using SFS.UI;
using UnityEngine;

namespace SFS.Builds;

public class PickCategoryUI : MonoBehaviour
{
	public PickGridUI.CategoryParts list;

	public Button button;

	public TextAdapter text;

	public GameObject select;

	public RelativeSizeFitter size;

	public void Selected(bool selected)
	{
		((ButtonPC)button).SetSelected(selected);
	}
}
