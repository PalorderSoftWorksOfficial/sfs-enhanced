using System;
using SFS.Parts.Modules;

namespace SFS.UI;

public class ToggleButton : Button
{
	public MoveModule moveModule;

	private Action toggle;

	private Func<bool> getValue;

	public void Bind(Action toggle, Func<bool> getValue)
	{
		Bind(toggle, getValue, out var _);
	}

	public void Bind(Action toggle, Func<bool> getValue, out Action update)
	{
		this.toggle = toggle;
		this.getValue = getValue;
		UpdateUI(instantAnimation: true);
		update = delegate
		{
			UpdateUI(instantAnimation: false);
		};
	}

	protected override void Start()
	{
		base.Start();
		onClick += new Action(OnClick);
	}

	private void OnClick()
	{
		toggle();
		UpdateUI(instantAnimation: false);
	}

	public void UpdateUI(bool instantAnimation)
	{
		moveModule.targetTime.Value = (getValue() ? 1 : 0);
		if (instantAnimation)
		{
			moveModule.time.Value = (getValue() ? 1 : 0);
		}
	}
}
