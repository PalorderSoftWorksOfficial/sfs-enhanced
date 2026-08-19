using System;
using SFS.Input;
using SFS.Translations;
using UnityEngine;

namespace SFS.UI;

public class OptionsMenuDrawer : Screen_Menu
{
	public RectTransform holder;

	public Button closeButton;

	public TextAdapter closeButtonText;

	public RectTransform holderPrefab;

	public RectTransform contentHolder;

	private RectTransform currentHolder;

	private Action onOpen;

	private Action onClose;

	private CloseMode onEscape;

	protected override CloseMode OnEscape => onEscape;

	public override void OnOpen()
	{
		holder.gameObject.SetActive(value: true);
		onOpen?.Invoke();
	}

	public override void OnClose()
	{
		if (currentHolder != null)
		{
			UnityEngine.Object.Destroy(currentHolder.gameObject);
		}
		if (holder != null)
		{
			holder.gameObject.SetActive(value: false);
		}
		onClose?.Invoke();
	}

	public Func<Screen_Base> CreateDelegate(CloseMode onEscape, CancelButton cancelButton, Action onOpen, Action onClose, params MenuElement[] elements)
	{
		return delegate
		{
			currentHolder = UnityEngine.Object.Instantiate(holderPrefab, contentHolder);
			currentHolder.gameObject.SetActive(value: true);
			MenuElement[] array = elements;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Invoke(currentHolder.gameObject);
			}
			this.onOpen = onOpen;
			this.onClose = onClose;
			this.onEscape = onEscape;
			string text = ((cancelButton == CancelButton.Cancel) ? ((string)Loc.main.Cancel) : ((cancelButton == CancelButton.Close) ? ((string)Loc.main.Close) : ""));
			return this;
		};
	}
}
