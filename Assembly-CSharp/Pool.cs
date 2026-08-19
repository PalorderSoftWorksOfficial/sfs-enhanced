using System;
using System.Collections.Generic;

public class Pool<T>
{
	private Func<T> createNew;

	private Action<T> onReset;

	public List<T> Items { get; }

	public int Index { get; private set; }

	public Pool(Func<T> createNew, Action<T> onReset)
	{
		this.createNew = createNew;
		this.onReset = onReset;
		Items = new List<T>();
	}

	public T GetItem()
	{
		if (Index == Items.Count)
		{
			Items.Add(createNew());
		}
		Index++;
		return Items[Index - 1];
	}

	public void Reset()
	{
		foreach (T item in Items)
		{
			onReset(item);
		}
		ResetIndex();
	}

	public void ResetIndex()
	{
		Index = 0;
	}
}
