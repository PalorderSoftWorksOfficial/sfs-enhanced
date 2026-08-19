using SFS.Tween;
using SFS.World;
using UnityEngine;
using UnityEngine.UI;

public class GravityTurnDrawer : MonoBehaviour
{
	public Image rocket;

	public Image targetAngle;

	public CanvasGroup canvasGroup;

	private void Update()
	{
		if (LocationDrawer.main.currentAngleInfo != null)
		{
			if ((canvasGroup.alpha == 0f && LocationDrawer.main.currentAngleInfo.showAngle) || (canvasGroup.alpha == 1f && !LocationDrawer.main.currentAngleInfo.showAngle))
			{
				canvasGroup.alpha = (LocationDrawer.main.currentAngleInfo.showAngle ? 1 : 0);
			}
			rocket.transform.eulerAngles = new Vector3(0f, 0f, (float)(0.0 - LocationDrawer.main.currentAngleInfo.angle));
			targetAngle.transform.TweenRotate(new Vector3(0f, 0f, (float)(0.0 - LocationDrawer.main.currentAngleInfo.targetAngle)), 2f, setValueToTargetOnKill: false);
		}
	}
}
