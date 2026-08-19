using SFS.Input;
using UnityEngine;

namespace SFS.UI;

public class BasicMenu : Screen_Menu
{
	public GameObject menuHolder;

	public bool closeAnyway;

	protected override CloseMode OnEscape => CloseMode.Current;

	public override void Close()
	{
		if (ScreenManager.main.CurrentScreen == this)
		{
			ScreenManager.main.CloseCurrent();
		}
		else if (closeAnyway && menuHolder != null)
		{
			menuHolder.SetActive(value: false);
		}
	}

	public override void OnOpen()
	{
		menuHolder.SetActive(value: true);
	}

	public override void OnClose()
	{
		if (menuHolder != null)
		{
			menuHolder.SetActive(value: false);
		}
	}
}
