using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.World;

[Serializable]
public class JointGroup
{
	public Dictionary<Part, List<PartJoint>> dictionary;

	public List<PartJoint> joints;

	public List<Part> parts;

	public JointGroup(List<PartJoint> joints, List<Part> parts)
	{
		this.joints = joints;
		this.parts = parts;
		dictionary = new Dictionary<Part, List<PartJoint>>();
		foreach (PartJoint joint in joints)
		{
			AddJointToDictionary(joint);
		}
	}

	private void AddJointToDictionary(PartJoint joint)
	{
		Add(joint.a);
		Add(joint.b);
		void Add(Part a)
		{
			if (!dictionary.TryGetValue(a, out var value))
			{
				value = new List<PartJoint>();
				dictionary.Add(a, value);
			}
			value.Add(joint);
		}
	}

	public static void OnPartDestroyed(Part part, Rocket rocket, DestructionReason reason)
	{
		rocket.jointsGroup.RemovePartAndItsJoints(part);
		if (RecreateRockets(rocket, out var childRockets).Count > 0)
		{
			if ((bool)rocket.isPlayer && !rocket.hasControl)
			{
				SetFirstControllableChildAsPlayer();
			}
			return;
		}
		if ((bool)rocket.isPlayer)
		{
			SetFirstControllableChildAsPlayer();
		}
		RocketManager.DestroyRocket(rocket, reason);
		void SetFirstControllableChildAsPlayer()
		{
			if (childRockets.Count != 0)
			{
				PlayerController.main.player.Value = childRockets.FirstOrDefault((Rocket a) => a.hasControl) ?? childRockets[0];
			}
		}
	}

	public static void DestroyJoint(PartJoint joint, Rocket rocket, out bool split, out Rocket newRocket)
	{
		rocket.jointsGroup.RemoveJoint(joint);
		RecreateRockets(rocket, out var childRockets);
		split = childRockets.Count > 0;
		newRocket = (split ? childRockets[0] : null);
	}

	public static List<JointGroup> RecreateRockets(Rocket rocket, out List<Rocket> childRockets)
	{
		rocket.jointsGroup.RecreateGroups(out var newGroups);
		Vector2 vector = Vector2.zero;
		if (newGroups.Count > 0)
		{
			vector = newGroups[0].parts[0].Position;
			((I_Physics)rocket).LocalPosition += (Vector2)rocket.partHolder.transform.TransformVector(vector);
			rocket.SetJointGroup(newGroups[0]);
		}
		childRockets = new List<Rocket>();
		for (int i = 1; i < newGroups.Count; i++)
		{
			Rocket rocket2 = RocketManager.CreateRocket_Child(newGroups[i], rocket, vector);
			childRockets.Add(rocket2);
			rocket2.EnableCollisionImmunity(0.1f);
		}
		return newGroups;
	}

	public void RecreateGroups(out List<JointGroup> newGroups)
	{
		HashSet<Part> hashSet = new HashSet<Part>();
		newGroups = new List<JointGroup>();
		foreach (Part part2 in parts)
		{
			if (hashSet.Contains(part2))
			{
				continue;
			}
			List<Part> list = new List<Part>();
			HashSet<PartJoint> hashSet2 = new HashSet<PartJoint>();
			Stack<Part> stack = new Stack<Part>();
			hashSet.Add(part2);
			stack.Push(part2);
			while (stack.Count > 0)
			{
				Part part = stack.Pop();
				list.Add(part);
				if (!dictionary.TryGetValue(part, out var value))
				{
					continue;
				}
				foreach (PartJoint item2 in value)
				{
					if (!hashSet2.Contains(item2))
					{
						hashSet2.Add(item2);
					}
					Part otherPart = item2.GetOtherPart(part);
					if (!hashSet.Contains(otherPart))
					{
						hashSet.Add(otherPart);
						stack.Push(otherPart);
					}
				}
			}
			JointGroup item = new JointGroup(hashSet2.ToList(), list);
			newGroups.Add(item);
		}
	}

	public void AddJoint(PartJoint joint)
	{
		joints.Add(joint);
		AddJointToDictionary(joint);
	}

	public void RemovePartAndItsJoints(Part part)
	{
		if (dictionary.TryGetValue(part, out var value))
		{
			PartJoint[] array = value.ToArray();
			foreach (PartJoint joint in array)
			{
				RemoveJoint(joint);
			}
		}
		parts.Remove(part);
		dictionary.Remove(part);
	}

	private void RemoveJoint(PartJoint joint)
	{
		joints.Remove(joint);
		if (dictionary.TryGetValue(joint.a, out var value))
		{
			value.Remove(joint);
		}
		if (dictionary.TryGetValue(joint.b, out var value2))
		{
			value2.Remove(joint);
		}
	}

	public void RepositionParts()
	{
		HashSet<Part> hashSet = new HashSet<Part>();
		Queue<(Part, Vector2)> queue = new Queue<(Part, Vector2)>();
		Part part = parts[0];
		hashSet.Add(part);
		queue.Enqueue((part, Vector2.zero));
		while (queue.Count > 0)
		{
			(Part, Vector2) tuple = queue.Dequeue();
			var (part2, _) = tuple;
			Vector2 vector = (part2.Position = tuple.Item2);
			part2.orientation.ApplyOrientation();
			if (!dictionary.TryGetValue(part2, out var value))
			{
				continue;
			}
			foreach (PartJoint item2 in value)
			{
				Part otherPart = item2.GetOtherPart(part2);
				if (!hashSet.Contains(otherPart))
				{
					hashSet.Add(otherPart);
					queue.Enqueue((otherPart, vector + item2.GetRelativeAnchor(part2)));
				}
			}
		}
	}

	public List<List<ResourceModule>> GetResourceGroups()
	{
		List<(ResourceModule, Part)> list = new List<(ResourceModule, Part)>();
		foreach (Part part2 in parts)
		{
			if (part2.HasModule<ResourceModule>())
			{
				list.Add((part2.GetModules<ResourceModule>()[0], part2));
			}
		}
		HashSet<ResourceModule> hashSet = new HashSet<ResourceModule>();
		List<List<ResourceModule>> list2 = new List<List<ResourceModule>>();
		foreach (var item in list)
		{
			if (hashSet.Contains(item.Item1))
			{
				continue;
			}
			List<ResourceModule> list3 = new List<ResourceModule>();
			list2.Add(list3);
			list3.Add(item.Item1);
			hashSet.Add(item.Item1);
			HashSet<Part> hashSet2 = new HashSet<Part> { item.Item2 };
			Stack<Part> stack = new Stack<Part>();
			stack.Push(item.Item2);
			while (stack.Count > 0)
			{
				Part part = stack.Pop();
				if (!dictionary.TryGetValue(part, out var value))
				{
					continue;
				}
				foreach (PartJoint item2 in value)
				{
					Part otherPart = item2.GetOtherPart(part);
					if (hashSet2.Contains(otherPart))
					{
						continue;
					}
					hashSet2.Add(otherPart);
					if (otherPart.HasModule<ResourceModule>())
					{
						ResourceModule resourceModule = otherPart.GetModules<ResourceModule>()[0];
						if (!(item.Item1.resourceType != resourceModule.resourceType))
						{
							stack.Push(otherPart);
							list3.Add(resourceModule);
							hashSet.Add(resourceModule);
						}
					}
				}
			}
		}
		return list2;
	}

	public List<SplitModule> GetConnectedFairings(Part startPart, SplitModule fairing)
	{
		List<SplitModule> list = new List<SplitModule>();
		HashSet<Part> hashSet = new HashSet<Part> { startPart };
		Stack<Part> stack = new Stack<Part>();
		stack.Push(startPart);
		list.Add(fairing);
		while (stack.Count > 0)
		{
			Part part = stack.Pop();
			if (!dictionary.TryGetValue(part, out var value))
			{
				continue;
			}
			foreach (PartJoint item in value)
			{
				Part otherPart = item.GetOtherPart(part);
				if (hashSet.Contains(otherPart))
				{
					continue;
				}
				hashSet.Add(otherPart);
				if (otherPart.HasModule<SplitModule>())
				{
					SplitModule splitModule = otherPart.GetModules<SplitModule>()[0];
					if (splitModule.fairing)
					{
						stack.Push(otherPart);
						list.Add(splitModule);
					}
				}
			}
		}
		return list;
	}

	public List<PartJoint> GetConnectedJoints(Part part)
	{
		if (!dictionary.TryGetValue(part, out var value))
		{
			return new List<PartJoint>();
		}
		return value.ToList();
	}
}
