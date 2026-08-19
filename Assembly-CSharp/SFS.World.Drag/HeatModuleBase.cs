using UnityEngine;

namespace SFS.World.Drag;

public abstract class HeatModuleBase : MonoBehaviour
{
	public Valid valid = new Valid();

	public abstract string Name { get; }

	public abstract bool IsHeatShield { get; }

	public abstract float Temperature { get; set; }

	public abstract int LastAppliedIndex { get; set; }

	public abstract float ExposedSurface { get; set; }

	public abstract float HeatTolerance { get; }

	private void OnEnable()
	{
		valid.valid = true;
	}

	private void OnDisable()
	{
		valid.valid = false;
	}

	public abstract void OnOverheat(bool breakup);
}
