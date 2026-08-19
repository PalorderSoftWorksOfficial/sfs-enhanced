using System;

namespace SFS.Variables;

[Serializable]
public class Bool_Reference : ReferenceVariable<bool>
{
	public override VariableList<bool>.Variable GetVariable(string variableName)
	{
		return referenceToVariables.boolVariables.GetVariable(variableName);
	}

	public override VariableList<bool> GetVariableList()
	{
		return referenceToVariables.boolVariables;
	}

	protected override bool IsEqual(bool a, bool b)
	{
		return a == b;
	}
}
