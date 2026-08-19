using System;
using SFS.Variables;
using UnityEngine;
using UnityEngine.Events;

namespace SFS.Parts.Modules;

public class SeparatorColor : ColorModule
{
	public Color buildColor = Color.white;

	public Color worldColor = Color.white;

	public Float_Reference height;

	public UnityEvent onToggleInterior;

	private bool initialized;

	public override Color GetColor()
	{
		if (Application.isPlaying && (!InteriorManager.main.interiorView.Value || !(height.Value > 1f)))
		{
			return worldColor;
		}
		return buildColor;
	}

	private void Awake()
	{
		InteriorManager.main.interiorView.OnChange += new Action(OnToggleInterior);
		initialized = true;
	}

	private void OnDestroy()
	{
		InteriorManager.main.interiorView.OnChange -= new Action(OnToggleInterior);
	}

	private void OnToggleInterior()
	{
		if (initialized)
		{
			onToggleInterior.Invoke();
		}
	}
}
