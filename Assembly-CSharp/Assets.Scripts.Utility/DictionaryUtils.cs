using System.Collections.Generic;

namespace Assets.Scripts.Utility;

public static class DictionaryUtils
{
	public static V At<K, V>(this Dictionary<K, V> dictionary, K key) where V : class
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}
}
