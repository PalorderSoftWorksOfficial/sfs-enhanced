using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class LinkModule : MonoBehaviour
{
	public Composed_Float input;

	public Float_Reference output;

	private void Start()
	{
		input.OnChange += new Action(RecalculateOutput);
	}

	public void RecalculateOutput()
	{
		output.Value = input.Value;
	}
}
