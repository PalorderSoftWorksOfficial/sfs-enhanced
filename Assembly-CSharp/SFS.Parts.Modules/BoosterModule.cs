using System;
using System.Collections;
using System.Linq;
using SFS.Career;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class BoosterModule : MonoBehaviour, Rocket.INJ_Rocket, I_PartMenu, I_InitializePartModule, ResourceDrawer.I_Resource, Rocket.INJ_Throttle
{
	public bool showDescription = true;

	[Space]
	public SurfaceData surfaceForCover;

	[Space]
	public ResourceType resourceType;

	public Composed_Float ISP;

	public Composed_Vector2 thrustVector;

	public Composed_Vector2 thrustPosition;

	public Composed_Float wetMass;

	public Composed_Float dryMassPercent;

	public Float_Reference fuelPercent;

	public Bool_Reference boosterPrimed;

	private float newIgnitionTime = -1f;

	public Float_Reference throttle_Out;

	public Double_Reference mass_Out = new Double_Reference();

	public Bool_Reference heatOn;

	public GameObject heatHolder;

	private Vector3 originalPosition;

	private Float_Local throttle_Input = new Float_Local();

	public Rocket Rocket { get; set; }

	public float Throttle
	{
		set
		{
			throttle_Input.Value = value;
		}
	}

	private float ThrustDuration => FuelMass / (thrustVector.Value.magnitude / (ISP.Value * (float)Base.worldBase.settings.difficulty.IspMultiplier));

	private float FuelMass => (1f - DryMassPercent) * wetMass.Value;

	private float DryMassPercent => dryMassPercent.Value * (float)Base.worldBase.settings.difficulty.DryMassMultiplier;

	int I_InitializePartModule.Priority => -1;

	ResourceType ResourceDrawer.I_Resource.ResourceType => resourceType;

	float ResourceDrawer.I_Resource.WetMass => wetMass.Value;

	Double_Reference ResourceDrawer.I_Resource.ResourcePercent => fuelPercent;

	void I_PartMenu.Draw(StatsMenu drawer, PartDrawSettings settings)
	{
		if (showDescription)
		{
			drawer.DrawStat(51, thrustVector.Value.magnitude.ToThrustString());
			drawer.DrawStat(50, ThrustDuration.ToBurnTimeString(forceDecimals: false));
			drawer.DrawStat(49, ISP.Value.ToEfficiencyString());
			string value = FuelMass.ToString(2, forceDecimals: false) + resourceType.resourceUnit.Field;
			drawer.DrawStat(48, Loc.main.Info_Resource_Amount.InjectField(resourceType.displayName, "resource").Inject(value, "amount"));
		}
	}

	void I_InitializePartModule.Initialize()
	{
		base.enabled = fuelPercent.Value < 1f && fuelPercent.Value > 0f;
		throttle_Input.OnChange += new Action(TryIgnite);
		wetMass.OnChange += new Action(RecalculateMass);
		dryMassPercent.OnChange += new Action(RecalculateMass);
		fuelPercent.OnChange += new Action(RecalculateMass);
		void RecalculateMass()
		{
			mass_Out.Value = DryMassPercent * wetMass.Value + FuelMass * fuelPercent.Value;
		}
	}

	private void Start()
	{
		heatHolder.gameObject.SetActive(heatOn.Value);
		if (GameManager.main != null)
		{
			originalPosition = heatHolder.transform.localPosition;
			WorldView main = WorldView.main;
			main.onVelocityOffset = (Action<Vector2>)Delegate.Combine(main.onVelocityOffset, new Action<Vector2>(PositionFlameHitbox));
		}
	}

	private void OnDestroy()
	{
		if (GameManager.main != null)
		{
			WorldView main = WorldView.main;
			main.onVelocityOffset = (Action<Vector2>)Delegate.Remove(main.onVelocityOffset, new Action<Vector2>(PositionFlameHitbox));
		}
	}

	public void Fire()
	{
		if (!CanUseBooster(this, Rocket.isPlayer.Value))
		{
			return;
		}
		if (CareerState.main.HasFeature(WorldSave.CareerState.throttleFeature))
		{
			boosterPrimed.Value = !boosterPrimed.Value;
			TryIgnite();
			if (!base.enabled)
			{
				MsgDrawer.main.Log("Booster: " + (boosterPrimed.Value ? "On" : "Off"));
			}
			return;
		}
		BoosterModule[] array = (from b in base.transform.GetComponentInParentTree<Rocket>().partHolder.GetModules<BoosterModule>()
			where CanUseBooster(b, showMsg: false)
			select b).ToArray();
		if (array.Length == 1)
		{
			Ignite();
			return;
		}
		newIgnitionTime = Time.unscaledTime + 0.75f;
		array.Where((BoosterModule b) => b.newIgnitionTime != -1f).ForEach(delegate(BoosterModule b)
		{
			b.newIgnitionTime = newIgnitionTime;
		});
		StartCoroutine(FireDelayed());
		IEnumerator FireDelayed()
		{
			while (Time.unscaledTime < newIgnitionTime)
			{
				yield return new WaitForFixedUpdate();
			}
			Ignite();
		}
	}

	private void TryIgnite()
	{
		if (boosterPrimed.Value && throttle_Input.Value > 0.01f && CanUseBooster(this, showMsg: false))
		{
			Ignite();
		}
	}

	public void Fire_Instantly()
	{
		if (CanUseBooster(this, Rocket.isPlayer.Value))
		{
			Ignite();
		}
	}

	private void Ignite()
	{
		boosterPrimed.Value = false;
		throttle_Out.Value = 1f;
		base.enabled = true;
	}

	private bool CanUseBooster(BoosterModule boosterModule, bool showMsg)
	{
		if (surfaceForCover != null && SurfaceData.IsSurfaceCovered(surfaceForCover))
		{
			if (showMsg)
			{
				MsgDrawer.main.Log("Cannot ignite a covered booster");
			}
			return false;
		}
		if (boosterModule.enabled)
		{
			if (showMsg)
			{
				MsgDrawer.main.Log("Solid fuel boosters cannot be turned off once ignited");
			}
			return false;
		}
		if (!SandboxSettings.main.settings.infiniteFuel && boosterModule.fuelPercent.Value == 0f)
		{
			if (showMsg)
			{
				MsgDrawer.main.Log(Loc.main.Msg_No_Resource_Left.InjectField(resourceType.displayName, "resource"));
			}
			return false;
		}
		return true;
	}

	private void FixedUpdate()
	{
		if (Rocket == null)
		{
			return;
		}
		if (!SandboxSettings.main.settings.infiniteFuel)
		{
			fuelPercent.Value -= Time.fixedDeltaTime / ThrustDuration * throttle_Out.Value;
			if (fuelPercent.Value <= 0f)
			{
				throttle_Out.Value = 0f;
				fuelPercent.Value = 0f;
				base.enabled = false;
				if (Rocket.isPlayer.Value)
				{
					MsgDrawer.main.Log(Loc.main.Msg_No_Resource_Left.InjectField(resourceType.displayName, "resource"));
				}
				return;
			}
		}
		Rigidbody2D rb2d = Rocket.rb2d;
		Vector2 vector = thrustVector.Value * 9.8f;
		Vector2 force = (Base.worldBase.AllowsCheats ? ((Vector2)base.transform.TransformVector(vector)) : base.transform.TransformVectorUnscaled(vector));
		Vector2 relativePoint = rb2d.GetRelativePoint(Transform_Utility.LocalToLocalPoint(base.transform, rb2d, thrustPosition.Value));
		rb2d.AddForceAtPosition(force, relativePoint, ForceMode2D.Force);
		PositionFlameHitbox();
	}

	private void PositionFlameHitbox(Vector2 _)
	{
		PositionFlameHitbox();
	}

	private void PositionFlameHitbox()
	{
		heatHolder.transform.localPosition = originalPosition + heatHolder.transform.parent.InverseTransformVector(Rocket.rb2d.velocity * Time.fixedDeltaTime);
	}
}
