using System;
using System.Collections.Generic;
using SFS.Parsers.Constructed;

namespace SFS.Variables;

[Serializable]
public class Composed_Double : Composed<double>
{
	public string input;

	private Compute.I_Node compiled;

	protected override double GetResult(bool initialize)
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

	protected override bool Equals(double a, double b)
	{
		return a == b;
	}
}
