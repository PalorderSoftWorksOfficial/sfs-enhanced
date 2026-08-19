using UnityEngine.UI;

namespace SFS.Career;

public class TT_Info : TT_Base
{
	public Text title;

	public Text info;

	private TT_InfoData data;

	public override I_TechTreeData Data => data;

	public void Initialize(TT_InfoData data)
	{
		this.data = data;
		info.text = data.info;
		UpdateUI();
	}

	private void MarkSeen()
	{
		UpdateUI();
		OnComplete();
	}

	private void UpdateUI()
	{
		title.text = ((!data.IsUnlocked()) ? "??" : data.title);
	}

	public override void OnSelect()
	{
		MarkSeen();
	}

	protected override void OnParentsComplete()
	{
		UpdateUI();
	}
}
