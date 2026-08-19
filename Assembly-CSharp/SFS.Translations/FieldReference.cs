using System;

namespace SFS.Translations;

[Serializable]
public class FieldReference
{
	public string name;

	public string group;

	public string MenuName
	{
		get
		{
			if (group == null)
			{
				return name;
			}
			return group + "/" + name;
		}
	}

	public FieldReference(string name, Group group)
	{
		this.name = name;
		this.group = group.Name;
	}

	public FieldReference(string name, string group)
	{
		this.name = name;
		this.group = group;
	}

	public override bool Equals(object obj)
	{
		if (obj is FieldReference fieldReference)
		{
			if (name == fieldReference.name && group == fieldReference.group)
			{
				return MenuName == fieldReference.MenuName;
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (17 * 23 + (name ?? "").GetHashCode()) * 23 + (group ?? "").GetHashCode();
	}

	public static bool operator ==(FieldReference a, FieldReference b)
	{
		if ((object)a == null)
		{
			return (object)b == null;
		}
		if ((object)b == null)
		{
			return false;
		}
		return a.Equals(b);
	}

	public static bool operator !=(FieldReference a, FieldReference b)
	{
		return !(a == b);
	}
}
