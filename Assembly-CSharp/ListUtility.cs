using System;
using System.Collections;
using System.Collections.Generic;

public static class ListUtility
{
	public static bool IsValidIndex(this IList a, int index)
	{
		if (index > -1)
		{
			return index < a.Count;
		}
		return false;
	}

	public static bool IsValidInsert(this IList a, int index)
	{
		if (index > -1)
		{
			return index <= a.Count;
		}
		return false;
	}

	public static int RemoveRange<T>(this List<T> a, IList collection)
	{
		return a.RemoveAll((T x) => collection.Contains(x));
	}

	public static void ForEach<T>(this IEnumerable<T> list, Action<T> onItem)
	{
		foreach (T item in list)
		{
			onItem(item);
		}
	}

	public static T GetBest<T, T2>(this IEnumerable<T> list, Func<T, T2> getScore, out T2 bestScore) where T2 : IComparable
	{
		(T, T2)? tuple = null;
		foreach (T item2 in list)
		{
			T2 item = getScore(item2);
			if (!tuple.HasValue || item.CompareTo(tuple.Value.Item2) > 0)
			{
				tuple = (item2, item);
			}
		}
		bestScore = tuple.Value.Item2;
		return tuple.Value.Item1;
	}

	public static T GetBest<T>(this IEnumerable<T> list, Func<T, T, bool> isBetter)
	{
		bool flag = false;
		T val = default(T);
		foreach (T item in list)
		{
			if (!flag || isBetter(item, val))
			{
				val = item;
				flag = true;
			}
		}
		return val;
	}

	public static List<T> Collapse<T>(this IEnumerable<IEnumerable<T>> lists)
	{
		List<T> list = new List<T>();
		foreach (IEnumerable<T> list2 in lists)
		{
			list.AddRange(list2);
		}
		return list;
	}
}
