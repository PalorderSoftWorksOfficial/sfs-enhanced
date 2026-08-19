using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SFS.Parsers.Ini;
using SFS.Parsers.Regex;
using UnityEngine;

namespace SFS.Translations;

public static class TranslationSerialization
{
	public static string Serialize<T>(T translation)
	{
		List<(PropertyInfo, Group)> fieldReferences = GetFieldReferences<T>();
		foreach (var (propertyInfo, obj) in fieldReferences)
		{
			if (obj.subExports.Count == 0)
			{
				continue;
			}
			Field field = propertyInfo.GetValue(translation) as Field;
			foreach (Func<string, string> subExport in obj.subExports)
			{
				field?.SetSub(field.GetSubs().Length, subExport(field));
			}
		}
		IniConverter iniConverter = new IniConverter();
		foreach (var (propertyInfo2, obj2) in fieldReferences)
		{
			if (propertyInfo2.GetCustomAttribute<Unexported>() != null)
			{
				continue;
			}
			FieldReference fieldReference = new FieldReference(propertyInfo2.Name, obj2.Name);
			Field obj3 = propertyInfo2.GetValue(translation) as Field;
			bool flag = obj3.GetSubs().Length > 1 || obj2.hasSubs || propertyInfo2.GetCustomAttributes<MarkAsSub>().Any();
			bool flag2 = true;
			(int, string)[] subs = obj3.GetSubs();
			for (int i = 0; i < subs.Length; i++)
			{
				(int, string) tuple3 = subs[i];
				IniDataSection section = iniConverter.GetSection(fieldReference.group);
				IniDataEnv.Value value = new IniDataEnv.Value(tuple3.Item2);
				string index;
				if (!flag)
				{
					index = fieldReference.name;
				}
				else
				{
					string name = fieldReference.name;
					int item = tuple3.Item1;
					index = name + "{" + item + "}";
				}
				section[index] = value;
				LocSpace customAttribute = propertyInfo2.GetCustomAttribute<LocSpace>();
				Documentation customAttribute2 = propertyInfo2.GetCustomAttribute<Documentation>();
				if (!flag2)
				{
					continue;
				}
				flag2 = false;
				if (customAttribute2 != null)
				{
					if (customAttribute2.attachToGroup)
					{
						section.comment = customAttribute2.comment;
					}
					else if (customAttribute2.afterLine)
					{
						value.aftLineComment = customAttribute2.comment;
					}
					else
					{
						value.preLineComment = customAttribute2.comment;
					}
				}
				if (customAttribute != null)
				{
					if (customAttribute.attachToGroup)
					{
						section.whitespacesBefore = customAttribute.amount;
					}
					else
					{
						value.whitespacesBefore = customAttribute.amount;
					}
				}
			}
		}
		return iniConverter.Serialize();
	}

	public static SFS_Translation Deserialize(string iniText, out List<FieldReference> unused, out List<FieldReference> missing, out List<FieldReference> changed)
	{
		Dictionary<FieldReference, Field> dictionary = ParseTranslations(iniText);
		SFS_Translation sFS_Translation = new SFS_Translation();
		unused = new List<FieldReference>(dictionary.Keys);
		missing = new List<FieldReference>();
		changed = new List<FieldReference>();
		foreach (var fieldReference2 in GetFieldReferences<SFS_Translation>())
		{
			PropertyInfo item = fieldReference2.Item1;
			Group item2 = fieldReference2.Item2;
			FieldReference fieldReference = new FieldReference(item.Name, item2.Name);
			Unexported customAttribute = item.GetCustomAttribute<Unexported>();
			if (!dictionary.ContainsKey(fieldReference))
			{
				if (customAttribute == null)
				{
					missing.Add(fieldReference);
				}
				continue;
			}
			unused.Remove(fieldReference);
			Field field = dictionary[fieldReference];
			Field field2 = item.GetMethod.Invoke(sFS_Translation, new object[0]) as Field;
			(int, string)[] subs = field2.GetSubs();
			for (int i = 0; i < subs.Length; i++)
			{
				(int, string) tuple = subs[i];
				if (!field.HasSub(tuple.Item1) || tuple.Item2 != field2.GetSub(tuple.Item1))
				{
					changed.Add(fieldReference);
				}
			}
			field2.subs = field.subs;
		}
		return sFS_Translation;
	}

	private static Dictionary<FieldReference, Field> ParseTranslations(string iniText)
	{
		IniConverter iniConverter = new IniConverter(iniText);
		Dictionary<FieldReference, Field> dictionary = new Dictionary<FieldReference, Field>();
		SimpleRegex simpleRegex = new SimpleRegex("(?<name>.*){(?<number>\\d*)}");
		string[] sectionNames = iniConverter.GetSectionNames();
		foreach (string text in sectionNames)
		{
			IniDataSection section = iniConverter.GetSection(text);
			string text2 = text;
			foreach (KeyValuePair<string, IniDataEnv.Value> datum in section.data)
			{
				FieldReference fieldReference = new FieldReference(datum.Key, text2);
				int result = 0;
				if (simpleRegex.Input(fieldReference.name))
				{
					fieldReference.name = simpleRegex.GetGroup("name").Value;
					if (!int.TryParse(simpleRegex.GetGroup("number").Value, out result))
					{
						continue;
					}
				}
				Field field = new Field();
				if (dictionary.ContainsKey(fieldReference))
				{
					field = dictionary[fieldReference];
				}
				else
				{
					dictionary[fieldReference] = field;
				}
				field.SetSub(result, datum.Value.value);
			}
		}
		return dictionary;
	}

	public static T CreateTranslation<T>() where T : new()
	{
		return new T();
	}

	public static List<(PropertyInfo, Group)> GetFieldReferences<T>()
	{
		List<(PropertyInfo, Group)> list = new List<(PropertyInfo, Group)>();
		Group item = new Group("None");
		PropertyInfo[] properties = typeof(T).GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			try
			{
				if (propertyInfo.PropertyType == typeof(Field))
				{
					if (propertyInfo.GetCustomAttribute<Group>() != null)
					{
						item = propertyInfo.GetCustomAttribute<Group>();
					}
					list.Add((propertyInfo, item));
				}
			}
			catch (Exception ex)
			{
				Debug.Log("Lang prop error: " + propertyInfo.Name + "\n" + ex);
			}
		}
		return list;
	}
}
