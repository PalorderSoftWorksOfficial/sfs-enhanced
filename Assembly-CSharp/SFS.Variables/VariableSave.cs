using System;

namespace SFS.Variables;

[Serializable]
public class VariableSave
{
	public string name;

	public bool save;

	public byte[] data;

	public object runtimeVariable;

	public VariableSave(string name)
	{
		this.name = name;
	}
}
