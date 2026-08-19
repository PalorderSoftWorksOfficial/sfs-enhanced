using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parsers.Regex;
using UnityEngine;

namespace SFS.Translations;

public class Field
{
	public class Builder
	{
		private string translation;

		public Builder(string translation)
		{
			this.translation = translation;
		}

		public Builder InjectField(Field subs, string variableName)
		{
			SimpleRegex simpleRegex = new SimpleRegex("%" + variableName + "{(?<number>\\d*?)}%");
			while (simpleRegex.Input(translation))
			{
				if (int.TryParse(simpleRegex.GetGroup("number").Value, out var result))
				{
					string sub = subs.GetSub(result);
					translation = translation.Remove(simpleRegex.Match.Index, simpleRegex.Match.Length);
					translation = translation.Insert(simpleRegex.Match.Index, sub);
				}
			}
			return Inject(subs, variableName);
		}

		public Builder Inject(string value, string variableName)
		{
			translation = translation.Replace("%" + variableName + "%", value);
			return this;
		}

		public string GetText()
		{
			return translation;
		}

		public static implicit operator string(Builder a)
		{
			return a.translation;
		}
	}

	public Dictionary<int, string> subs = new Dictionary<int, string>();

	public string GetSub(int index)
	{
		if (!subs.ContainsKey(index))
		{
			return subs[0];
		}
		return subs[index];
	}

	public (int, string)[] GetSubs()
	{
		return subs.Select((KeyValuePair<int, string> pair) => (Key: pair.Key, Value: pair.Value)).ToArray();
	}

	public bool HasSub(int index)
	{
		return subs.ContainsKey(index);
	}

	public void SetSub(int index, string sub)
	{
		subs[index] = sub;
	}

	public Builder InjectField(TranslationVariable subs, string variableName)
	{
		return InjectField(subs.Field, variableName);
	}

	public Builder InjectField(Field subs, string variableName)
	{
		return GetBuilder(0).InjectField(subs, variableName);
	}

	public Builder Inject(string value, string variableName)
	{
		return GetBuilder(0).Inject(value, variableName);
	}

	private Builder GetBuilder(int subIndex)
	{
		return new Builder(GetSub(subIndex));
	}

	public static implicit operator string(Field field)
	{
		try
		{
			return field.subs[0];
		}
		catch (Exception message)
		{
			Debug.Log(message);
			throw;
		}
	}

	public static Field Text(string sub)
	{
		return new Field
		{
			subs = { [0] = sub }
		};
	}

	public static Field Subs(params string[] subs)
	{
		Field field = new Field();
		for (int i = 0; i < subs.Length; i++)
		{
			field.subs[i] = subs[i];
		}
		return field;
	}

	public static Field MultilineText(params string[] lines)
	{
		return Text(string.Join("\n", lines));
	}
}
