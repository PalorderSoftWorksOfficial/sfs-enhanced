using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.Translations;
using SFS.Variables;
using SFS.World;
using UnityEngine;
using UnityEngine.Events;

namespace SFS.Parts.Modules;

public class SplitModule : SeparatorBase, Rocket.INJ_Rocket, I_InitializePartModule
{
	[Serializable]
	public class Fragment
	{
		public string fragmentName;

		public GameObject[] toEnable;

		public GameObject[] toDisable;

		public SurfaceData[] attachmentSurfaces;

		public Composed_Vector2 separationForce;
	}

	public SplitModule prefab;

	public Fragment[] fragments;

	[Space]
	public bool showDescription = true;

	[Space]
	public bool showForceMultiplier;

	public Float_Reference forceMultiplier;

	[Space]
	public bool fairing;

	public Bool_Reference detachEdges;

	[Space]
	public String_Reference fragmentName;

	[Space]
	public UnityEvent onDeploy;

	private Part part;

	public Rocket Rocket { private get; set; }

	private float ForceMultiplier
	{
		get
		{
			if (!showForceMultiplier)
			{
				return 1f;
			}
			return forceMultiplier.Value * 2f;
		}
	}

	public override bool ShowDescription => showDescription;

	int I_InitializePartModule.Priority => 0;

	public override void Draw(List<SeparatorBase> modules, StatsMenu drawer, PartDrawSettings settings)
	{
		float force = fragments.Sum((Fragment f) => f.separationForce.Value.magnitude);
		if (settings.build && showForceMultiplier)
		{
			drawer.DrawSlider(0, GetForce, GetMaxForce, () => forceMultiplier.Value, delegate(float newValue, bool touchStart)
			{
				SeparatorBase.SetForcePercent(newValue, modules, touchStart);
			}, delegate(Action update)
			{
				forceMultiplier.OnChange += update;
			}, delegate(Action update)
			{
				forceMultiplier.OnChange -= update;
			});
		}
		else
		{
			drawer.DrawStat(70, GetForce());
		}
		if (fairing && (settings.build || settings.game))
		{
			drawer.DrawToggle(0, () => Loc.main.Detach_Edges_Label, ToggleDetachEdges, () => detachEdges.Value, delegate(Action update)
			{
				detachEdges.OnChange += update;
			}, delegate(Action update)
			{
				detachEdges.OnChange -= update;
			});
		}
		string GetForce()
		{
			return (force * ForceMultiplier).ToSeparationForceString(forceDecimals: false);
		}
		string GetMaxForce()
		{
			return (force * 2f).ToSeparationForceString(showForceMultiplier);
		}
		void ToggleDetachEdges()
		{
			Undo.main.RecordStatChangeStep(modules, delegate
			{
				bool value = !detachEdges.Value;
				foreach (SeparatorBase module in modules)
				{
					if (module is SplitModule { fairing: not false } splitModule)
					{
						splitModule.detachEdges.Value = value;
					}
				}
			});
		}
	}

	void I_InitializePartModule.Initialize()
	{
		part = base.transform.GetComponentInParentTree<Part>();
		if (!string.IsNullOrEmpty(fragmentName.Value))
		{
			ApplyFragment();
		}
	}

	public void Split(UsePartData data)
	{
		if (!string.IsNullOrEmpty(fragmentName.Value))
		{
			return;
		}
		bool flag = Rocket.isPlayer;
		Double2 globalPosition = WorldView.ToGlobalPosition(Rocket.rb2d.worldCenterOfMass + PlayerController.main.cameraOffset);
		List<SplitModule> partsToPush = new List<SplitModule>();
		if (fairing)
		{
			foreach (SplitModule connectedFairing in Rocket.jointsGroup.GetConnectedFairings(part, this))
			{
				connectedFairing.Deploy(ref partsToPush);
			}
		}
		else
		{
			Deploy(ref partsToPush);
		}
		JointGroup.RecreateRockets(Rocket, out var childRockets);
		List<Rocket> list = childRockets.ToList();
		list.Add(Rocket);
		if (flag)
		{
			Rocket.SetPlayerToBestControllable(list.ToArray());
			Rocket rocket = (Rocket)PlayerController.main.player.Value;
			rocket.location.position.Value = WorldView.ToGlobalPosition(rocket.physics.PhysicsObject.LocalPosition);
			PlayerController.main.SetOffset(WorldView.ToLocalPosition(globalPosition) - (rocket.rb2d.worldCenterOfMass + PlayerController.main.cameraOffset), 4f);
		}
		foreach (SplitModule item in partsToPush)
		{
			UsePartData.SharedData sharedData = data.sharedData;
			sharedData.onPostPartsActivation = (Action)Delegate.Combine(sharedData.onPostPartsActivation, new Action(item.ApplySeparationForce));
		}
	}

	private void Deploy(ref List<SplitModule> partsToPush)
	{
		fragmentName.Value = "!";
		onDeploy?.Invoke();
		Rocket.EnableCollisionImmunity(0.1f);
		JointGroup jointsGroup = Rocket.jointsGroup;
		List<PartJoint> connectedJoints = jointsGroup.GetConnectedJoints(part);
		jointsGroup.RemovePartAndItsJoints(part);
		for (int i = 0; i < fragments.Length; i++)
		{
			SplitModule splitModule = CreateFragment(i);
			jointsGroup.parts.Add(splitModule.GetComponent<Part>());
			partsToPush.Add(splitModule);
			foreach (PartJoint item in connectedJoints)
			{
				Part otherPart = item.GetOtherPart(part);
				if (ArePartsConnected(splitModule.fragments[i], otherPart) && (!fairing || !detachEdges.Value || (otherPart.HasModule<SplitModule>() && otherPart.GetModules<SplitModule>()[0].fairing)))
				{
					jointsGroup.AddJoint(new PartJoint(splitModule.part, otherPart, item.GetRelativeAnchor(part)));
				}
			}
		}
		part.DestroyPart(createExplosion: false, updateJoints: false, DestructionReason.Intentional);
	}

	private SplitModule CreateFragment(int index)
	{
		SplitModule splitModule = PartsLoader.DuplicateParts(null, part)[0].GetModules<SplitModule>()[0];
		splitModule.name = prefab.name;
		Transform obj = splitModule.part.transform;
		Transform transform = part.transform;
		obj.parent = transform.parent;
		obj.localPosition = transform.localPosition;
		obj.localRotation = transform.localRotation;
		if (part.burnMark != null)
		{
			splitModule.part.burnMark = splitModule.part.gameObject.AddComponent<BurnMark>();
			splitModule.part.burnMark.Initialize();
			splitModule.part.burnMark.burn = part.burnMark.burn.GetCopy();
			splitModule.part.burnMark.ApplyEverything();
		}
		splitModule.fragmentName.Value = fragments[index].fragmentName;
		splitModule.ApplyFragment();
		return splitModule;
	}

	private void ApplyFragment()
	{
		Fragment[] array = fragments;
		foreach (Fragment fragment in array)
		{
			if (fragmentName.Value == fragment.fragmentName)
			{
				GameObject[] toEnable = fragment.toEnable;
				for (int j = 0; j < toEnable.Length; j++)
				{
					toEnable[j].SetActive(value: true);
				}
				toEnable = fragment.toDisable;
				for (int j = 0; j < toEnable.Length; j++)
				{
					toEnable[j].SetActive(value: false);
				}
				break;
			}
		}
	}

	private static bool ArePartsConnected(Fragment fragment, Part other)
	{
		Line2[] attachmentSurfacesWorld = other.GetAttachmentSurfacesWorld();
		SurfaceData[] attachmentSurfaces = fragment.attachmentSurfaces;
		for (int i = 0; i < attachmentSurfaces.Length; i++)
		{
			foreach (Surfaces surface in attachmentSurfaces[i].surfaces)
			{
				if (SurfaceUtility.SurfacesConnect(attachmentSurfacesWorld, surface.GetSurfacesWorld(), out var _, out var _))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ApplySeparationForce()
	{
		Vector2 force = fragments.Single((Fragment p) => p.fragmentName == fragmentName.Value).separationForce.Value * ForceMultiplier;
		Rocket.rb2d.AddForceAtPosition(base.transform.TransformVectorUnscaled(force), base.transform.TransformPoint(part.centerOfMass.Value), ForceMode2D.Impulse);
		PlayerController.main.CreateShakeEffect(force.magnitude / 200f, 1.2f, 500f, Rocket.rb2d.position);
	}
}
