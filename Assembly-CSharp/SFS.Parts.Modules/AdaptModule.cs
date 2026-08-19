using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class AdaptModule : MonoBehaviour, I_InitializePartModule
{
	[Serializable]
	public class Point
	{
		public ReceiverType reciverType;

		public Composed_Vector2 position;

		public Vector2 normal;

		public MinMaxRange adaptRange;

		public TriggerType type;

		public ExtraType[] extraTypes;

		public PriorityType[] priority;

		public Composed_Rect inputArea;

		public Composed_Float defaultOutput;

		public float outputOffset;

		public Float_Reference output;

		public Bool_Reference isOccupied;

		[Space]
		public Float_Reference differenceX;

		public Float_Reference differenceY;

		[NonSerialized]
		public Vector2 worldPosition;

		[NonSerialized]
		public Vector2 worldNormal;
	}

	public enum TriggerType
	{
		Fuselage,
		Fairing
	}

	public enum ReceiverType
	{
		Point,
		Area
	}

	public enum PriorityType
	{
		MinDistance,
		MaxDistance,
		MinValue,
		MaxValue,
		ClosestValue
	}

	[Serializable]
	public class ExtraType
	{
		public Bool_Reference apply;

		public TriggerType type;

		public float outputOffset;
	}

	private const float Range = 0.05f;

	public Point[] adaptPoints;

	public bool ignoreOccupied;

	[Space]
	public bool applySeparatorFix;

	public bool applyFairingConeFix;

	public Float_Reference original;

	public Float_Reference width;

	private bool initialized;

	public int Priority => 8;

	public void Initialize()
	{
		if (applyFairingConeFix && original.Value == -1f)
		{
			original.Value = width.Value;
		}
	}

	private void Start()
	{
		if (BuildManager.main != null)
		{
			Point[] array = adaptPoints;
			for (int i = 0; i < array.Length; i++)
			{
				ExtraType[] extraTypes = array[i].extraTypes;
				for (int j = 0; j < extraTypes.Length; j++)
				{
					extraTypes[j].apply.OnChange += (Action)delegate
					{
						OnCanAdaptChange(base.transform, initialized);
					};
				}
			}
		}
		initialized = true;
	}

	public static void OnCanAdaptChange(Transform owner, bool initialized)
	{
		if (initialized)
		{
			UpdateAdaptation(owner.GetComponentInParentTree<PartHolder>().GetArray());
		}
	}

	public static void UpdateAdaptation(params Part[] parts)
	{
		Dictionary<Vector2Int, List<AdaptTriggerPoint>> triggers = new Dictionary<Vector2Int, List<AdaptTriggerPoint>>();
		foreach (AdaptTriggerModule module in Part_Utility.GetModules<AdaptTriggerModule>(parts))
		{
			AdaptTriggerPoint[] points = module.points;
			foreach (AdaptTriggerPoint adaptTriggerPoint in points)
			{
				adaptTriggerPoint.worldPosition = module.transform.TransformPoint(adaptTriggerPoint.position.Value);
				adaptTriggerPoint.worldNormal = module.transform.TransformVectorUnscaled(adaptTriggerPoint.normal);
				Vector2 worldPosition = adaptTriggerPoint.worldPosition;
				Vector2Int key = new Vector2Int(Mathf.FloorToInt(worldPosition.x / 0.05f), Mathf.FloorToInt(worldPosition.y / 0.05f));
				if (triggers.ContainsKey(key))
				{
					triggers[key].Add(adaptTriggerPoint);
					continue;
				}
				triggers[key] = new List<AdaptTriggerPoint> { adaptTriggerPoint };
			}
		}
		AdaptModule[] adaptModules = Part_Utility.GetModules<AdaptModule>(parts).ToArray();
		AdaptModule[] array = adaptModules;
		foreach (AdaptModule adaptModule in array)
		{
			Point[] array2 = adaptModule.adaptPoints;
			foreach (Point point in array2)
			{
				point.worldPosition = adaptModule.transform.TransformPoint(point.position.Value);
				point.worldNormal = adaptModule.transform.TransformVectorUnscaled(point.normal);
			}
		}
		Adapt();
		Adapt();
		void Adapt()
		{
			foreach (AdaptTriggerModule module2 in Part_Utility.GetModules<AdaptTriggerModule>(parts))
			{
				AdaptTriggerPoint[] points2 = module2.points;
				for (int k = 0; k < points2.Length; k++)
				{
					points2[k].Occupied = null;
				}
			}
			AdaptModule[] array3 = adaptModules;
			for (int k = 0; k < array3.Length; k++)
			{
				array3[k].Adapt(triggers, justMarkTriggerAsOccupied: true);
			}
			array3 = Part_Utility.GetModules<AdaptModule>(parts.Reverse()).ToArray();
			for (int k = 0; k < array3.Length; k++)
			{
				array3[k].Adapt(triggers, justMarkTriggerAsOccupied: false);
			}
		}
	}

	private void Adapt(Dictionary<Vector2Int, List<AdaptTriggerPoint>> triggers, bool justMarkTriggerAsOccupied)
	{
		Point[] array = adaptPoints;
		foreach (Point point in array)
		{
			List<AdaptTriggerPoint> list = new List<AdaptTriggerPoint>();
			switch (point.reciverType)
			{
			case ReceiverType.Point:
			{
				Vector2Int[] dictionaryKeys = MagnetModule.GetDictionaryKeys(point.worldPosition, 0.05f);
				foreach (Vector2Int key in dictionaryKeys)
				{
					if (!triggers.TryGetValue(key, out var value))
					{
						continue;
					}
					foreach (AdaptTriggerPoint item in value)
					{
						if (Accepted(point, item) && !list.Contains(item))
						{
							list.Add(item);
						}
					}
				}
				break;
			}
			case ReceiverType.Area:
				foreach (List<AdaptTriggerPoint> value2 in triggers.Values)
				{
					foreach (AdaptTriggerPoint item2 in value2)
					{
						if (Accepted(point, item2))
						{
							list.Add(item2);
						}
					}
				}
				break;
			}
			bool flag = list.Count > 0;
			AdaptTriggerPoint bestTrigger = (flag ? GetBestPoint(point, list) : null);
			float num = (flag ? (bestTrigger.output.Value + GetOutputOffset() + bestTrigger.outputOffset) : point.defaultOutput.Value);
			float num2 = (((point.reciverType == ReceiverType.Area) & flag) ? GetPositionDifference_Local().x : 0f);
			float num3 = (((point.reciverType == ReceiverType.Area) & flag) ? GetPositionDifference_Local().y : 0f);
			if (justMarkTriggerAsOccupied)
			{
				if (flag && !ignoreOccupied && point.output.Value == num && point.differenceX.Value == num2 && point.differenceY.Value == num3)
				{
					bestTrigger.Occupied = point;
				}
				continue;
			}
			point.isOccupied.Value = flag;
			point.output.Value = num;
			point.differenceX.Value = num2;
			point.differenceY.Value = num3;
			if (flag && !ignoreOccupied)
			{
				bestTrigger.Occupied = point;
			}
			float GetOutputOffset()
			{
				if (bestTrigger.type == point.type)
				{
					return point.outputOffset;
				}
				ExtraType[] extraTypes = point.extraTypes;
				foreach (ExtraType extraType in extraTypes)
				{
					if (extraType.apply.Value && extraType.type == bestTrigger.type)
					{
						return extraType.outputOffset;
					}
				}
				return 0f;
			}
			Vector2 GetPositionDifference_Local()
			{
				return (Vector2)base.transform.InverseTransformPoint(bestTrigger.worldPosition) - point.position.Value;
			}
		}
	}

	private bool Accepted(Point receiver, AdaptTriggerPoint trigger)
	{
		bool flag = trigger.Occupied == null || trigger.Occupied == receiver;
		bool flag2 = ignoreOccupied | flag;
		if (applySeparatorFix && !flag && trigger.outputOffset == 0.4f)
		{
			return false;
		}
		if (flag2 && (!trigger.toggle || trigger.enabled.Value) && MatchingType(receiver, trigger) && InPositionRange(receiver, trigger) && MatchingNormals(receiver, trigger))
		{
			return InAdaptRange(receiver, trigger);
		}
		return false;
	}

	private static bool MatchingType(Point receiver, AdaptTriggerPoint trigger)
	{
		if (receiver.type == trigger.type)
		{
			return true;
		}
		ExtraType[] extraTypes = receiver.extraTypes;
		foreach (ExtraType extraType in extraTypes)
		{
			if (extraType.apply.Value && extraType.type == trigger.type)
			{
				return true;
			}
		}
		return false;
	}

	private bool InAdaptRange(Point receiver, AdaptTriggerPoint trigger)
	{
		if (applySeparatorFix && trigger.outputOffset == 0.4f && ((Vector2)base.transform.InverseTransformPoint(trigger.worldPosition) - receiver.position.Value).y < 0.001f)
		{
			return false;
		}
		if (trigger.output.Value > receiver.adaptRange.min.Value - 0.001f)
		{
			return trigger.output.Value < receiver.adaptRange.max.Value + 0.001f;
		}
		return false;
	}

	private bool InPositionRange(Point receiver, AdaptTriggerPoint trigger)
	{
		if (receiver.reciverType == ReceiverType.Area)
		{
			Vector2 position = base.transform.InverseTransformPoint(trigger.worldPosition);
			return Math_Utility.InArea(receiver.inputArea.Value, position, 0.05f);
		}
		return (trigger.worldPosition - receiver.worldPosition).sqrMagnitude < 0.0025000002f;
	}

	private bool MatchingNormals(Point receiver, AdaptTriggerPoint trigger)
	{
		return (receiver.worldNormal + trigger.worldNormal).sqrMagnitude < 0.01f;
	}

	private AdaptTriggerPoint GetBestPoint(Point adaptPoint, List<AdaptTriggerPoint> points)
	{
		AdaptTriggerPoint[] array = points.ToArray();
		PriorityType[] priority = adaptPoint.priority;
		foreach (PriorityType priorityType in priority)
		{
			array = GetBestPoints(adaptPoint, array, priorityType);
			if (array.Length == 1)
			{
				break;
			}
		}
		return array[0];
	}

	private AdaptTriggerPoint[] GetBestPoints(Point adaptPoint, AdaptTriggerPoint[] pointsToCheck, PriorityType priorityType)
	{
		List<AdaptTriggerPoint> list = new List<AdaptTriggerPoint>();
		float num = float.NegativeInfinity;
		foreach (AdaptTriggerPoint adaptTriggerPoint in pointsToCheck)
		{
			float num2 = 0f;
			switch (priorityType)
			{
			case PriorityType.MinDistance:
				num2 = 0f - GetPositionDifference(adaptPoint, adaptTriggerPoint).sqrMagnitude;
				break;
			case PriorityType.MaxDistance:
				num2 = GetPositionDifference(adaptPoint, adaptTriggerPoint).sqrMagnitude;
				break;
			case PriorityType.MinValue:
				num2 = 0f - adaptTriggerPoint.output.Value;
				break;
			case PriorityType.MaxValue:
				num2 = adaptTriggerPoint.output.Value;
				break;
			case PriorityType.ClosestValue:
				num2 = 0f - Mathf.Abs(adaptPoint.defaultOutput.Value - (adaptTriggerPoint.output.Value + adaptTriggerPoint.outputOffset));
				break;
			}
			if (num2 > num)
			{
				list.Clear();
			}
			if (num2 >= num)
			{
				list.Add(adaptTriggerPoint);
				num = num2;
			}
		}
		return list.ToArray();
	}

	private Vector2 GetPositionDifference(Point adaptPoint, AdaptTriggerPoint trigger)
	{
		return trigger.worldPosition - adaptPoint.worldPosition;
	}
}
