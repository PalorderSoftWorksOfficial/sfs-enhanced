using System;
using System.Collections;
using SFS.Input;
using SFS.Translations;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class LoadingScreen : Screen_Menu
{
	public GameObject menuHolder;

	public Text text;

	public GameObject loader;

	private Func<bool> isReady;

	private string loadingText = "";

	protected override CloseMode OnEscape => CloseMode.None;

	public override void Open()
	{
		Open(Loc.main.Loading_In_Progress);
	}

	public void Open(string text, Func<bool> isReady = null)
	{
		this.isReady = isReady;
		this.text.text = text;
		loadingText = text.Replace(".", "");
		base.Open();
	}

	public override void OnOpen()
	{
		menuHolder.SetActive(value: true);
		StartCoroutine(Animate());
	}

	public override void OnClose()
	{
		StopAllCoroutines();
		menuHolder.SetActive(value: false);
	}

	private IEnumerator Animate()
	{
		int frameIndex = -1;
		while (isReady == null || !isReady())
		{
			frameIndex++;
			switch (frameIndex % 4)
			{
			default:
				text.text = loadingText + "<color=#00000000>...</color>";
				break;
			case 1:
				text.text = loadingText + ".<color=#00000000>..</color>";
				break;
			case 2:
				text.text = loadingText + "..<color=#00000000>.</color>";
				break;
			case 3:
				text.text = loadingText + "...";
				break;
			}
			yield return new WaitForSecondsRealtime(0.3f);
		}
		Close();
	}
}
