using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace SFS.Variables;

[Serializable]
public class VariableList<T>
{
	public class Variable : IVariable
	{
		private VariableSave save;

		private T value;

		[HideInInspector]
		public Action onValueChange;

		[HideInInspector]
		public Action<T> onValueChangeNew;

		[HideInInspector]
		public Action<T, T> onValueChangeOldNew;

		public bool Save
		{
			get
			{
				return save.save;
			}
			set
			{
				save.save = value;
			}
		}

		public T Value
		{
			get
			{
				return value;
			}
			set
			{
				if (!object.Equals(value, this.value))
				{
					T arg = this.value;
					this.value = value;
					onValueChange?.Invoke();
					onValueChangeNew?.Invoke(value);
					onValueChangeOldNew?.Invoke(arg, value);
				}
			}
		}

		public Variable()
		{
			save = new VariableSave("");
		}

		public Variable(VariableSave save)
		{
			this.save = save;
			if (save.data != null && save.data.Length != 0)
			{
				using (MemoryStream memoryStream = new MemoryStream(save.data))
				{
					memoryStream.Seek(0L, SeekOrigin.Begin);
					value = (T)new BinaryFormatter().Deserialize(memoryStream);
				}
			}
		}

		VariableSave IVariable.GetSave()
		{
			return save;
		}

		void IVariable.SaveAsBytes()
		{
			if (value != null)
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					new BinaryFormatter().Serialize(memoryStream, value);
					save.data = memoryStream.ToArray();
				}
			}
		}
	}

	private interface IVariable
	{
		void SaveAsBytes();

		VariableSave GetSave();
	}

	[SerializeField]
	[HideInInspector]
	public List<VariableSave> saves = new List<VariableSave>();

	private List<Variable> _variables;

	private List<Variable> variables
	{
		get
		{
			if (_variables == null)
			{
				_variables = saves.Select((VariableSave save) => GetVariable(save.name)).ToList();
			}
			return _variables;
		}
		set
		{
		}
	}

	public T GetValue(string variableName)
	{
		if (!Has(variableName))
		{
			return default(T);
		}
		return GetVariable(variableName).Value;
	}

	public Variable GetVariable(string variableName)
	{
		VariableSave variableSave = null;
		foreach (VariableSave safe in saves)
		{
			if (safe.name == variableName)
			{
				variableSave = safe;
			}
		}
		if (variableSave == null)
		{
			return null;
		}
		if (variableSave.runtimeVariable == null)
		{
			variableSave.runtimeVariable = new Variable(variableSave);
		}
		return (Variable)variableSave.runtimeVariable;
	}

	public void SetValue(string variableName, T newValue, (bool, bool) addMissingVariables = default((bool, bool)))
	{
		if (!Has(variableName))
		{
			if (!addMissingVariables.Item1)
			{
				return;
			}
			saves.Add(new VariableSave(variableName)
			{
				save = addMissingVariables.Item2
			});
		}
		GetVariable(variableName).Value = newValue;
	}

	public bool Has(string variableName)
	{
		foreach (VariableSave safe in saves)
		{
			if (safe.name == variableName)
			{
				return true;
			}
		}
		return false;
	}

	public void RegisterOnVariableChange(Action onChange, string variableName)
	{
		Variable variable = GetVariable(variableName);
		variable.onValueChange = (Action)Delegate.Combine(variable.onValueChange, onChange);
	}

	public List<string> GetVariableNameList()
	{
		return saves.Select((VariableSave a) => a.name).ToList();
	}

	public Dictionary<string, T> GetSaveDictionary()
	{
		Dictionary<string, T> dictionary = new Dictionary<string, T>();
		foreach (VariableSave safe in saves)
		{
			if (safe.save)
			{
				dictionary[safe.name] = GetVariable(safe.name).Value;
			}
		}
		return dictionary;
	}

	public void LoadDictionary(Dictionary<string, T> inputs, (bool, bool) addMissingVariables)
	{
		if (inputs == null)
		{
			return;
		}
		foreach (KeyValuePair<string, T> input in inputs)
		{
			SetValue(input.Key, input.Value, addMissingVariables);
		}
	}

	private void AddVariable()
	{
		saves.Add(new VariableSave(""));
	}

	private void RemoveVariable(Variable toRemove)
	{
		saves.Remove(((IVariable)toRemove).GetSave());
	}

	private void RemoveVariableByIndex(int index)
	{
		saves.RemoveAt(index);
	}

	private bool SyncVariables(List<Variable> variables)
	{
		foreach (Variable variable in variables)
		{
			((IVariable)variable).SaveAsBytes();
		}
		saves = variables.Select((Variable var) => ((IVariable)var).GetSave()).ToList();
		return true;
	}
}
