using System;
using SFS.UI;
using UnityEngine;

namespace UI;

public class ScrollToButtonAttach : MonoBehaviour
{
	public Button button;

	public ScrollElement scrollElement;

	public int speed = 30;

	private void Start()
	{
		button.onScroll += (Action<float>)delegate(float delta)
		{
			if (scrollElement.vertical)
			{
				scrollElement.Move(new Vector2(0f, delta * (float)speed));
			}
			if (scrollElement.horizontal)
			{
				scrollElement.Move(new Vector2(delta * (float)speed, 0f));
			}
		};
	}
}
