using System;
using SFS.Variables;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Parts.Modules;

[Serializable]
public class MoveData
{
	public enum Type
	{
		RotationZ,
		Scale,
		Position,
		CenterOfMass,
		CenterOfDrag,
		SpriteColor,
		ImageColor,
		Active,
		Inactive,
		SoundVolume,
		AnchoredPosition,
		RotationXY,
		FloatVariable,
		BoolVariable
	}

	public Type type;

	public float offset;

	public Transform transform;

	public SpriteRenderer spriteRenderer;

	public Image image;

	public AnimationCurve X = new AnimationCurve();

	public AnimationCurve Y = new AnimationCurve();

	public Gradient gradient = new Gradient();

	public AudioSource audioSource;

	public Float_Reference floatVariable;

	public Bool_Reference boolVariable;

	private bool ShowTransform()
	{
		return (new bool[14]
		{
			true, true, true, false, false, false, false, true, true, false,
			true, true, false, false
		})[(int)type];
	}

	private bool ShowSpriteRenderer()
	{
		return type == Type.SpriteColor;
	}

	private bool ShowImage()
	{
		return type == Type.ImageColor;
	}

	private bool ShowCurveX()
	{
		return (new bool[14]
		{
			true, true, true, true, true, false, false, true, true, true,
			true, true, true, true
		})[(int)type];
	}

	private bool ShowCurveY()
	{
		return (new bool[14]
		{
			false, true, true, true, true, false, false, false, false, false,
			true, true, false, false
		})[(int)type];
	}

	private bool ShowGradient()
	{
		if (type != Type.SpriteColor)
		{
			return type == Type.ImageColor;
		}
		return true;
	}

	private bool ShowAudio()
	{
		return type == Type.SoundVolume;
	}
}
