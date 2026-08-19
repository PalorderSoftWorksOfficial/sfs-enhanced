using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace SFS.Input;

public class Screen_Game : Screen_Base, I_Touchable
{
	public Action<OnInputStartData> onInputStart;

	public Action<OnInputStayData> onInputStay;

	public Action<OnInputEndData> onInputEnd;

	public Action<ZoomData> onZoom;

	public Action<DragData> onDrag;

	public Action<OnTouchLongClickData> onTouchLongClick;

	public Action<OnNotStationary> onNotStationary;

	public KeysNode keysNode;

	public UnityEvent onOpen;

	public UnityEvent onClose;

	public override bool PauseWhileOpen => false;

	public override void ProcessInput()
	{
		keysNode.ProcessInput(Screen_Base.keys);
	}

	public override void OnOpen()
	{
		onOpen.Invoke();
		TouchElements.elements.Add(this);
	}

	public override void OnClose()
	{
		onClose.Invoke();
		TouchElements.elements.Remove(this);
	}

	public void Close()
	{
		ScreenManager.main.CloseCurrent();
	}

	List<int> I_Touchable.GetPriority()
	{
		return new List<int> { -1 };
	}

	bool I_Touchable.PointcastElement(TouchPosition position)
	{
		return true;
	}

	void I_Touchable.OnInputStart(OnInputStartData data)
	{
		onInputStart?.Invoke(data);
	}

	void I_Touchable.OnInputStay(OnInputStayData data)
	{
		onInputStay?.Invoke(data);
	}

	void I_Touchable.OnInputEnd(OnInputEndData data)
	{
		onInputEnd?.Invoke(data);
	}

	void I_Touchable.OnTouchLongClick(OnTouchLongClickData data)
	{
		onTouchLongClick?.Invoke(data);
	}

	void I_Touchable.OnNotStationary(OnNotStationary data)
	{
		onNotStationary?.Invoke(data);
	}

	void I_Touchable.OnDrag(DragData data)
	{
		onDrag?.Invoke(data);
	}

	void I_Touchable.OnZoom(ZoomData data)
	{
		onZoom?.Invoke(data);
	}

	void I_Touchable.OnMouseEnter()
	{
	}

	void I_Touchable.OnMouseExit()
	{
	}
}
