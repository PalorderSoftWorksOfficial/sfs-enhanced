using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class ResourceBar : MonoBehaviour
{
	public Image bar;

	public TextAdapter countText;

	public TextAdapter percentText;

	public void UpdatePercent(float percent)
	{
		bar.fillAmount = percent;
		percentText.Text = percent.ToPercentString();
	}
}
