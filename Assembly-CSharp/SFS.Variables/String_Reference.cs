using System;

namespace SFS.Variables;

[Serializable]
public class String_Reference : ReferenceVariable<string>
{
	public override VariableList<string>.Variable GetVariable(string variableName)
	{
		return referenceToVariables.stringVariables.GetVariable(variableName);
	}

	public override VariableList<string> GetVariableList()
	{
		return referenceToVariables.stringVariables;
	}

	protected override bool IsEqual(string a, string b)
	{
		return a == b;
	}
}
