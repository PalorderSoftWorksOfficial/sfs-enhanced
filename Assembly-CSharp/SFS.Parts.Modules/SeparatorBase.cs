using System.Collections.Generic;
using SFS.Builds;
using UnityEngine;

namespace SFS.Parts.Modules;

public abstract class SeparatorBase : MonoBehaviour
{
	public abstract bool ShowDescription { get; }

	public abstract void Draw(List<SeparatorBase> modules, StatsMenu drawer, PartDrawSettings settings);

	protected static void SetForcePercent(float newValue, List<SeparatorBase> modules, bool touchStart)
	{
		Undo.main.RecordStatChangeStep(modules, delegate
		{
			foreach (SeparatorBase module in modules)
			{
				if (!(module is DetachModule detachModule))
				{
					if (module is SplitModule { showForceMultiplier: not false } splitModule)
					{
						splitModule.forceMultiplier.Value = newValue;
					}
				}
				else if (detachModule.showForceMultiplier)
				{
					detachModule.forceMultiplier.Value = newValue;
				}
			}
		}, touchStart);
	}
}
