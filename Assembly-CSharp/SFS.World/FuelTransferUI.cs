using SFS.Cameras;
using SFS.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.World;

public class FuelTransferUI : MonoBehaviour
{
	public RectTransform holder;

	public Image resourceBar;

	public TextAdapter percentText;

	public void DrawFuelPercent(Resources.Transfer transfer)
	{
		Vector3 position = transfer.part.transform.TransformPoint(transfer.part.centerOfMass.Value);
		holder.position = ActiveCamera.Camera.camera.WorldToScreenPoint(position);
		resourceBar.fillAmount = (float)transfer.group.resourcePercent.Value;
		percentText.Text = transfer.group.resourcePercent.Value.ToPercentString();
	}
}
