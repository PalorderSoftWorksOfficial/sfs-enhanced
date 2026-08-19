using SFS.Variables;
using UnityEngine;

namespace SFS.UI;

public class ScreenTracker : MonoBehaviour
{
	public Vector2_Local size;

	public ScreenOrientation_Local orientation;

	public bool Portrait => orientation.Value == ScreenOrientation.Portrait;

	private void Update()
	{
		size.Value = new Vector2(Screen.width, Screen.height);
		orientation.Value = ((Screen.width <= Screen.height) ? ScreenOrientation.Portrait : ScreenOrientation.LandscapeLeft);
	}
}
