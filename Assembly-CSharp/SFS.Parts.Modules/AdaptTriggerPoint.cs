using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

[Serializable]
public class AdaptTriggerPoint
{
	public bool toggle;

	public Bool_Reference enabled;

	public Composed_Vector2 position;

	public Vector2 normal;

	public AdaptModule.TriggerType type;

	public Composed_Float output;

	public float outputOffset;

	public bool shareIsOccupied;

	public int shareIndex;

	[NonSerialized]
	public AdaptTriggerModule owner;

	[NonSerialized]
	private AdaptModule.Point occupied;

	[NonSerialized]
	public Vector2 worldPosition;

	[NonSerialized]
	public Vector2 worldNormal;

	public AdaptModule.Point Occupied
	{
		get
		{
			if (!shareIsOccupied)
			{
				return occupied;
			}
			return owner.occupied[shareIndex];
		}
		set
		{
			if (shareIsOccupied)
			{
				owner.occupied[shareIndex] = value;
			}
			else
			{
				occupied = value;
			}
		}
	}
}
