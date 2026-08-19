namespace SFS.Input;

public class OnInputStayData
{
	public InputType inputType;

	public TouchPosition position;

	public DragData delta;

	public OnInputStayData(InputType inputType, TouchPosition position, DragData delta)
	{
		this.inputType = inputType;
		this.position = position;
		this.delta = delta;
	}
}
