using System.Collections.Generic;
using SFS.Input;
using UnityEngine;

namespace SFS.UI;

public class SkipUI : MonoBehaviour, I_Touchable
{
	public RectTransform skipEnd;

	private void OnEnable()
	{
		TouchElements.AddElement(this);
	}

	private void OnDisable()
	{
		TouchElements.RemoveElement(this);
	}

	public List<int> GetPriority()
	{
		return TouchElements.GetPriority(base.transform);
	}

	public bool PointcastElement(TouchPosition position)
	{
		return TouchElements.PointcastElement(position, base.transform, advancedBorder: false, 0f, 0f, 0f, 0f, 0f);
	}

	void I_Touchable.OnInputStart(OnInputStartData data)
	{
	}

	void I_Touchable.OnInputStay(OnInputStayData data)
	{
	}

	void I_Touchable.OnInputEnd(OnInputEndData data)
	{
	}

	void I_Touchable.OnTouchLongClick(OnTouchLongClickData data)
	{
	}

	void I_Touchable.OnNotStationary(OnNotStationary data)
	{
	}

	void I_Touchable.OnDrag(DragData data)
	{
	}

	void I_Touchable.OnZoom(ZoomData data)
	{
	}

	void I_Touchable.OnMouseEnter()
	{
	}

	void I_Touchable.OnMouseExit()
	{
	}
}
