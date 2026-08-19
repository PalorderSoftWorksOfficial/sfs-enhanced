using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Component_Utility
{
	public static T GetOrAddComponent<T>(this Component a) where T : Component
	{
		T val = a.GetComponent<T>();
		if (val == null)
		{
			val = a.gameObject.AddComponent<T>();
		}
		return val;
	}

	public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
	{
		T val = obj.GetComponent<T>();
		if (val == null)
		{
			val = obj.AddComponent<T>();
		}
		return val;
	}

	public static List<T> GetOrAddComponents<T>(this Component a, int count) where T : Component
	{
		List<T> list = new List<T>(a.GetComponents<T>());
		while (list.Count < count)
		{
			list.Add(a.gameObject.AddComponent<T>());
		}
		return list;
	}

	public static T GetComponentInParentTree<T>(this Component a)
	{
		return a.transform.GetComponentInParentTree<T>();
	}

	public static T GetComponentInParentTree<T>(this Transform a)
	{
		while (a != null)
		{
			T component = a.GetComponent<T>();
			if (component != null)
			{
				return component;
			}
			a = a.parent;
		}
		return default(T);
	}

	public static T FindByName<T>(this Component component, string name) where T : Component
	{
		T val = component.GetComponentsInChildren<T>(includeInactive: true).ToList().Find((T c) => c.name == name);
		if (val != null)
		{
			return val;
		}
		throw new Exception("Cannot find by name: " + name);
	}

	public static bool HasComponent<T>(this GameObject obj) where T : Component
	{
		return obj.GetComponent<T>() != null;
	}

	public static bool HasComponent<T>(this GameObject obj, out T component) where T : Component
	{
		component = obj.GetComponent<T>();
		return component != null;
	}

	public static RectTransform GetRect(this Component component)
	{
		return component.transform.GetRect();
	}

	public static RectTransform GetRect(this Transform transform)
	{
		return (RectTransform)transform;
	}
}
