using System;
using SFS.Builds;

namespace SFS.Parts.Modules;

[Serializable]
public class Variants
{
	[Serializable]
	public class Variant
	{
		public Variable[] changes = new Variable[0];

		public PickTag[] tags = new PickTag[0];

		public double cost;

		public void ApplyVariant(Part part)
		{
			Variable[] array = changes;
			foreach (Variable variable in array)
			{
				switch (variable.type)
				{
				case Variable.ValueType.Number:
					part.variablesModule.doubleVariables.SetValue(variable.name, variable.numberValue);
					break;
				case Variable.ValueType.Toggle:
					part.variablesModule.boolVariables.SetValue(variable.name, variable.toggleValue);
					break;
				case Variable.ValueType.Text:
					part.variablesModule.stringVariables.SetValue(variable.name, variable.textValue);
					break;
				}
			}
		}
	}

	[Serializable]
	public class Variable
	{
		public enum ValueType
		{
			Number,
			Toggle,
			Text
		}

		public string name;

		public ValueType type;

		public double numberValue;

		public bool toggleValue;

		public string textValue;

		public string GetLabel()
		{
			return type switch
			{
				ValueType.Number => name + numberValue, 
				ValueType.Toggle => name + toggleValue, 
				ValueType.Text => name + textValue, 
				_ => throw new Exception(), 
			};
		}
	}

	[Serializable]
	public class PickTag
	{
		public PickCategory tag;

		public int priority;
	}

	public Variant[] variants = new Variant[0];

	public PickTag[] tags = new PickTag[0];
}
