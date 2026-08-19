using System;

public class OpenTracker
{
	public bool isOpen;

	private Action close;

	public OpenTracker(Action close, ref Action onClose)
	{
		isOpen = true;
		this.close = close;
		onClose = (Action)Delegate.Combine(onClose, (Action)delegate
		{
			isOpen = false;
		});
	}

	public OpenTracker()
	{
		isOpen = false;
		close = delegate
		{
		};
	}

	public void Close()
	{
		if (isOpen)
		{
			close();
		}
	}
}
