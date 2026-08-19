using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SFS.Parts;

public class PartHolder : MonoBehaviour
{
	private Part[] cachedArray;

	public List<Part> parts = new List<Part>();

	public HashSet<Part> partsSet = new HashSet<Part>();

	public Action<Part[]> onPartsAdded;

	public Action<Part[]> onPartsRemoved;

	public Action onPartsChanged;

	private Dictionary<string, object> modules = new Dictionary<string, object>();

	private Dictionary<string, int> moduleCount = new Dictionary<string, int>();

	public Part[] GetArray()
	{
		if (cachedArray == null)
		{
			cachedArray = parts.ToArray();
		}
		return cachedArray;
	}

	public void AddParts(params Part[] parts)
	{
		this.parts.AddRange(parts);
		parts.ForEach(delegate(Part part)
		{
			partsSet.Add(part);
		});
		cachedArray = null;
		ResetModules();
		onPartsAdded?.Invoke(parts);
		onPartsChanged?.Invoke();
	}

	public void AddPartAtIndex(int index, Part part)
	{
		parts.Insert(index, part);
		partsSet.Add(part);
		cachedArray = null;
		ResetModules();
		onPartsAdded?.Invoke(new Part[1] { part });
		onPartsChanged?.Invoke();
	}

	public void RemoveParts(params Part[] parts)
	{
		this.parts.RemoveRange(parts);
		foreach (Part item in parts)
		{
			partsSet.Remove(item);
		}
		cachedArray = null;
		ResetModules();
		onPartsRemoved?.Invoke(parts);
		onPartsChanged?.Invoke();
	}

	public void RemovePartAtIndex(int index)
	{
		Part part = parts[index];
		parts.RemoveAt(index);
		partsSet.Remove(part);
		cachedArray = null;
		ResetModules();
		onPartsRemoved?.Invoke(new Part[1] { part });
		onPartsChanged?.Invoke();
	}

	public void SetParts(params Part[] newParts)
	{
		Part[] array = GetArray();
		parts = new List<Part>(newParts);
		partsSet = new HashSet<Part>(newParts);
		cachedArray = null;
		ResetModules();
		onPartsRemoved?.Invoke(array);
		onPartsAdded?.Invoke(newParts);
		onPartsChanged?.Invoke();
	}

	public void ClearParts()
	{
		Part[] array = GetArray();
		parts.Clear();
		partsSet.Clear();
		cachedArray = null;
		ResetModules();
		onPartsRemoved?.Invoke(array);
		onPartsChanged?.Invoke();
	}

	public bool ContainsPart(Part part)
	{
		return partsSet.Contains(part);
	}

	public T[] GetModules<T>()
	{
		string key = typeof(T).Name;
		if (!modules.ContainsKey(key))
		{
			modules.Add(key, CollectModules<T>());
		}
		return (T[])modules[key];
	}

	public int GetModuleCount<T>()
	{
		string key = typeof(T).Name;
		if (!moduleCount.ContainsKey(key))
		{
			moduleCount.Add(key, CollectModuleCount<T>());
		}
		return moduleCount[key];
	}

	public bool HasModule<T>()
	{
		string key = typeof(T).Name;
		if (!moduleCount.ContainsKey(key))
		{
			moduleCount.Add(key, CollectModuleCount<T>());
		}
		return moduleCount[key] > 0;
	}

	private T[] CollectModules<T>()
	{
		List<T> list = new List<T>();
		foreach (Part part in parts)
		{
			IEnumerable<T> enumerable = part.GetModules<T>();
			if (enumerable.Any())
			{
				list.AddRange(enumerable);
			}
		}
		return list.ToArray();
	}

	private int CollectModuleCount<T>()
	{
		return parts.Sum((Part part) => part.GetModuleCount<T>());
	}

	private void ResetModules()
	{
		modules.Clear();
		moduleCount.Clear();
	}

	public void TrackParts(Action<Part> onPartAdded, Action<Part> onPartRemoved, Action onPartRemoved_After)
	{
		foreach (Part part in parts)
		{
			onPartAdded(part);
		}
		onPartsAdded = (Action<Part[]>)Delegate.Combine(onPartsAdded, (Action<Part[]>)delegate(Part[] addedParts)
		{
			foreach (Part obj in addedParts)
			{
				onPartAdded(obj);
			}
		});
		onPartsRemoved = (Action<Part[]>)Delegate.Combine(onPartsRemoved, (Action<Part[]>)delegate(Part[] removedParts)
		{
			foreach (Part obj in removedParts)
			{
				onPartRemoved(obj);
			}
			onPartRemoved_After();
		});
	}

	public void TrackModules<T>(Action<T> onModuleAdded, Action<T> onModuleRemoved, Action onModulesRemoved_After)
	{
		T[] array = GetModules<T>();
		foreach (T obj in array)
		{
			onModuleAdded(obj);
		}
		onPartsAdded = (Action<Part[]>)Delegate.Combine(onPartsAdded, (Action<Part[]>)delegate(Part[] addedParts)
		{
			foreach (T module in Part_Utility.GetModules<T>(addedParts))
			{
				onModuleAdded(module);
			}
		});
		onPartsRemoved = (Action<Part[]>)Delegate.Combine(onPartsRemoved, (Action<Part[]>)delegate(Part[] removedParts)
		{
			foreach (T module2 in Part_Utility.GetModules<T>(removedParts))
			{
				onModuleRemoved(module2);
			}
			onModulesRemoved_After();
		});
	}
}
