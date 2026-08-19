using System;

namespace SFS.Variables;

[Serializable]
public class Double_Reference : ReferenceVariable<double>
{
	public override VariableList<double>.Variable GetVariable(string variableName)
	{
		return referenceToVariables.doubleVariables.GetVariable(variableName);
	}

	public override VariableList<double> GetVariableList()
	{
		return referenceToVariables.doubleVariables;
	}

	protected override bool IsEqual(double a, double b)
	{
		return a == b;
	}
}
