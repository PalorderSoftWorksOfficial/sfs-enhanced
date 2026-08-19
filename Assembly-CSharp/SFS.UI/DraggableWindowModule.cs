using System;
using SFS.Input;
using UnityEngine;

namespace SFS.UI;

public class DraggableWindowModule : MonoBehaviour
{
	public Button button;

	public bool draggable;

	public event Action OnDropAction;

	private void Start()
	{
		button.onHold += new Action<OnInputStayData>(OnHold);
		button.onUp += new Action(OnDrop);
	}

	private void OnHold(OnInputStayData data)
	{
		if (draggable)
		{
			base.transform.position += (Vector3)data.delta.deltaPixel;
		}
	}

	private void OnDrop()
	{
		OnDropAction?.Invoke();
	}
}
