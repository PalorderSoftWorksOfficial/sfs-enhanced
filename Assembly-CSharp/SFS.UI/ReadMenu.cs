using System;
using System.Text;
using SFS.Input;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class ReadMenu : Screen_Menu, I_MsgLogger
{
	public GameObject holder;

	public Image background;

	public GameObject closeButtonBlack;

	public GameObject closeButtonTranslucent;

	public Text text;

	private Action onClose;

	private CloseMode closeMode;

	public Transform menuTransform;

	protected override CloseMode OnEscape => closeMode;

	public void ShowReport(string report, Action callback)
	{
		Open(() => report.Substring(0, Math.Min(5000, report.Length)), callback);
	}

	public void ShowReport(StringBuilder report, Action callback)
	{
		Open(() => report.ToString().Substring(0, Math.Min(5000, report.Length)), callback);
	}

	public void Open(Func<string> text, CloseMode closeMode = CloseMode.Current, bool background = true, bool centerText = true)
	{
		Open(text, null, closeMode, background, centerText);
	}

	public void Open(Func<string> text, Action onClose, CloseMode closeMode = CloseMode.Current, bool background = true, bool centerText = true)
	{
		ScreenManager.main.OpenScreen(Create(text, onClose, closeMode, background, centerText));
	}

	public Func<Screen_Base> Create(Func<string> text, Action onClose = null, CloseMode closeMode = CloseMode.Current, bool background = true, bool centerText = true)
	{
		return delegate
		{
			this.background.enabled = background;
			closeButtonBlack.SetActive(background);
			closeButtonTranslucent.SetActive(!background);
			this.text.text = text();
			this.text.alignment = (centerText ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
			this.closeMode = closeMode;
			this.onClose = onClose;
			return this;
		};
	}

	public override void Close()
	{
		if (closeMode == CloseMode.Stack)
		{
			CloseStack();
		}
		else
		{
			base.Close();
		}
	}

	public override void OnClose()
	{
		holder.SetActive(value: false);
		onClose?.Invoke();
	}

	public override void OnOpen()
	{
		holder.SetActive(value: true);
	}

	void I_MsgLogger.Log(string msg)
	{
		Open(() => msg);
	}
}
