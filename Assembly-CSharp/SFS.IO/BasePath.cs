using System.IO;

namespace SFS.IO;

public abstract class BasePath
{
	private string path;

	protected string Path
	{
		get
		{
			path = System.IO.Path.GetFullPath(path).Replace("\\", "/");
			if (!path.EndsWith("/"))
			{
				return path;
			}
			string text = path;
			return text.Substring(0, text.Length - 1);
		}
		set
		{
			path = System.IO.Path.GetFullPath(value).Replace("\\", "/");
		}
	}

	protected BasePath(string initialLocation)
	{
		Path = initialLocation;
	}

	protected string GetParentPath()
	{
		string text = path;
		while (text.EndsWith("/"))
		{
			string text2 = text;
			text = text2.Substring(0, text2.Length - 1);
		}
		return System.IO.Path.GetDirectoryName(text);
	}

	public override string ToString()
	{
		return Path;
	}

	public static implicit operator string(BasePath wrapper)
	{
		return wrapper.path;
	}
}
