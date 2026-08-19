namespace SFS.Input;

public class ZoomData
{
	public float zoomDelta;

	public TouchPosition zoomPosition;

	public ZoomData(float zoomDelta, TouchPosition zoomPosition)
	{
		this.zoomDelta = zoomDelta;
		this.zoomPosition = zoomPosition;
	}
}
