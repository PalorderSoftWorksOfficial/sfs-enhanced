using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Variables;
using SFS.World;
using UnityEngine;
using UnityEngine.Events;

namespace SFS.Parts.Modules;

public class DetachModule : SeparatorBase, Rocket.INJ_Rocket
{
	private struct DetachData
	{
		public PartJoint joint;

		public float overlapSurface;

		public Vector2 connectionPosition;
	}

	public SurfaceData separationSurface;

	public Composed_Vector2 separationForce;

	[Space]
	public bool showDescription = true;

	[Space]
	public bool showForceMultiplier;

	public bool useForceMultiplierEvenIfNotShown;

	public Float_Reference forceMultiplier;

	[Space]
	public bool cannotDetachIfSurfaceCovered;

	public SurfaceData surfaceForCover;

	[Space]
	public bool activatedByLES;

	[Space]
	public UnityEvent onDetach;

	public Rocket Rocket { private get; set; }

	private bool Use
	{
		get
		{
			if (!showForceMultiplier)
			{
				return useForceMultiplierEvenIfNotShown;
			}
			return true;
		}
	}

	private float ForceMultiplier
	{
		get
		{
			if (!Use)
			{
				return 1f;
			}
			return forceMultiplier.Value * 2f;
		}
	}

	public override bool ShowDescription => showDescription;

	public override void Draw(List<SeparatorBase> modules, StatsMenu drawer, PartDrawSettings settings)
	{
		float force = separationForce.Value.magnitude;
		if (settings.build && showForceMultiplier)
		{
			drawer.DrawSlider(70, GetForce, GetMaxForce, () => forceMultiplier.Value, delegate(float newValue, bool touchStart)
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
		string GetForce()
		{
			return (force * ForceMultiplier).ToSeparationForceString(forceDecimals: false);
		}
		string GetMaxForce()
		{
			return (force * 2f).ToSeparationForceString(showForceMultiplier);
		}
	}

	public void Detach(UsePartData data)
	{
		if (cannotDetachIfSurfaceCovered && SurfaceData.IsSurfaceCovered(surfaceForCover))
		{
			return;
		}
		List<DetachData> jointsToDetach = GetJointsToDetach();
		if (jointsToDetach.Count == 0)
		{
			return;
		}
		bool flag = true;
		if (!((Rocket.location.velocity.Value.magnitude < 10.0) | flag))
		{
			return;
		}
		float num = separationSurface.surfaces.Select((Surfaces x) => Vector2.Distance(x.points[0], x.points[1])).Aggregate((float x, float y) => x + y);
		foreach (DetachData detachData in jointsToDetach)
		{
			Rocket rocket = Rocket;
			Double2 globalPosition = WorldView.ToGlobalPosition(Rocket.rb2d.worldCenterOfMass + PlayerController.main.cameraOffset);
			JointGroup.DestroyJoint(detachData.joint, Rocket, out var split, out var newRocket);
			if (!split)
			{
				continue;
			}
			Vector3 force = base.transform.TransformVectorUnscaled(separationForce.Value * ForceMultiplier) * (detachData.overlapSurface / num);
			Part otherPart = detachData.joint.GetOtherPart(base.transform.GetComponentInParentTree<Part>());
			UsePartData.SharedData sharedData = data.sharedData;
			sharedData.onPostPartsActivation = (Action)Delegate.Combine(sharedData.onPostPartsActivation, (Action)delegate
			{
				if (otherPart != null)
				{
					otherPart.Rocket.rb2d.AddForceAtPosition(force, detachData.connectionPosition, ForceMode2D.Impulse);
				}
				Rocket.rb2d.AddForceAtPosition(-force, detachData.connectionPosition, ForceMode2D.Impulse);
				PlayerController.main.CreateShakeEffect(force.magnitude / 50f, 1.2f, 500f, Rocket.rb2d.position);
			});
			rocket.EnableCollisionImmunity(0.1f);
			newRocket.EnableCollisionImmunity(0.1f);
			if ((bool)rocket.isPlayer)
			{
				Rocket.SetPlayerToBestControllable(rocket, newRocket);
				Rocket rocket2 = (Rocket)PlayerController.main.player.Value;
				rocket2.location.position.Value = WorldView.ToGlobalPosition(rocket2.physics.PhysicsObject.LocalPosition);
				PlayerController.main.SetOffset(WorldView.ToLocalPosition(globalPosition) - (rocket2.rb2d.worldCenterOfMass + PlayerController.main.cameraOffset), 4f);
			}
		}
		onDetach?.Invoke();
	}

	private List<DetachData> GetJointsToDetach()
	{
		Part componentInParentTree = base.transform.GetComponentInParentTree<Part>();
		Line2[] array = separationSurface.surfaces.SelectMany((Surfaces x) => x.GetSurfacesWorld()).ToArray();
		List<DetachData> list = new List<DetachData>();
		foreach (PartJoint connectedJoint in Rocket.jointsGroup.GetConnectedJoints(componentInParentTree))
		{
			Line2[] attachmentSurfacesWorld = connectedJoint.GetOtherPart(componentInParentTree).GetAttachmentSurfacesWorld();
			bool flag = false;
			float num = 0f;
			Vector2 zero = Vector2.zero;
			Line2[] array2 = array;
			foreach (Line2 a in array2)
			{
				Line2[] array3 = attachmentSurfacesWorld;
				foreach (Line2 b in array3)
				{
					if (SurfaceUtility.SurfacesConnect(a, b, out var overlap, out var center))
					{
						flag = true;
						num += overlap;
						zero += center * overlap;
					}
				}
			}
			if (flag)
			{
				zero /= num;
				list.Add(new DetachData
				{
					joint = connectedJoint,
					overlapSurface = num,
					connectionPosition = zero
				});
			}
		}
		return list;
	}
}
