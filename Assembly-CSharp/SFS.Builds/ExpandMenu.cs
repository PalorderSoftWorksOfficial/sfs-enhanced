using SFS.Variables;
using UnityEngine;

namespace SFS.Builds;

public class ExpandMenu : MonoBehaviour
{
	public ExpandMenu[] toClose;

	public Bool_Local expanded;

	public void ToggleExpanded()
	{
		if ((bool)expanded)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	public void Open()
	{
		expanded.Value = true;
		ExpandMenu[] array = toClose;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].expanded.Value = false;
		}
	}

	public void Close()
	{
		expanded.Value = false;
	}

	private void OnDisable()
	{
		Close();
	}
}
