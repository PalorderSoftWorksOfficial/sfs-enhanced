using System;
using System.Collections.Generic;
using System.Globalization;
using SFS.Parsers.Constructed;

namespace SFS.Variables;

[Serializable]
public class Composed_Float : Composed<float>
{
	public string input;

	private Compute.I_Node compiled;

	protected override float GetResult(bool initialize)
	{
		if (initialize)
		{
			compiled = Compute.Compile(input, variables, out var usedVariables);
			foreach (string item in usedVariables)
			{
				variables.doubleVariables.RegisterOnVariableChange(base.Recalculate, item);
			}
		}
		List<string> usedVariables2;
		if (compiled == null)
		{
			return Compute.Compile(input, variables, out usedVariables2).Value;
		}
		return compiled.Value;
	}

	protected override bool Equals(float a, float b)
	{
		return a == b;
	}

	public Composed_Float(string a)
	{
		input = a;
		if (input.Contains("E"))
		{
			input = "0";
		}
	}

	public void Offset(float offset)
	{
		if (Compute.GetVariablesUsed(input).Count <= 0)
		{
			float num = Compute.Compile(input, variables, out var _).Value + offset;
			input = (num.ToString().Contains("E") ? "0" : num.ToString(CultureInfo.InvariantCulture));
		}
	}
}
