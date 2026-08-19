using System.Collections.Generic;

namespace SFS.Input;

public interface I_Touchable
{
	List<int> GetPriority();

	bool PointcastElement(TouchPosition position);

	void OnInputStart(OnInputStartData data);

	void OnInputStay(OnInputStayData data);

	void OnInputEnd(OnInputEndData data);

	void OnTouchLongClick(OnTouchLongClickData data);

	void OnNotStationary(OnNotStationary data);

	void OnDrag(DragData data);

	void OnZoom(ZoomData data);

	void OnMouseEnter();

	void OnMouseExit();
}
