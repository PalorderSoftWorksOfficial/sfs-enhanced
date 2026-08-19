using UnityEngine;

public class AeroData : MonoBehaviour
{
	public bool testShock;

	public float shockOpacity;

	public bool testReentry;

	public float reentryPercent;

	public StraightMesh shock_Edge;

	public StraightMesh shock_Outer;

	public StraightMesh reentry_Edge;

	public CurvedMesh reentry_Outer;

	public TemperatureTest formulaHolder;

	public AeroFormula Formula => formulaHolder.formula;
}
