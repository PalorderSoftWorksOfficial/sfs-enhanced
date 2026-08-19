using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SFS.IO;

public class FilePath : BasePath
{
	public string FileName
	{
		get
		{
			return System.IO.Path.GetFileName(base.Path);
		}
		set
		{
			base.Path = GetParentPath() + "/" + value;
		}
	}

	public string CleanFileName => System.IO.Path.GetFileNameWithoutExtension(base.Path);

	public string Extension
	{
		get
		{
			string fileName = FileName;
			if (!fileName.Contains("."))
			{
				return null;
			}
			return fileName.Split('.', StringSplitOptions.None).Last();
		}
	}

	public FilePath(string initialLocation)
		: base(initialLocation)
	{
	}

	public void WriteText(string text)
	{
		Write(delegate
		{
			File.WriteAllText(this, text);
		});
	}

	public void WriteBytes(byte[] data)
	{
		Write(delegate
		{
			File.WriteAllBytes(this, data);
		});
	}

	public void AppendText(string text)
	{
		if (!FileExists())
		{
			WriteText("");
		}
		File.AppendAllText(this, text);
	}

	public byte[] ReadBytes()
	{
		return File.ReadAllBytes(this);
	}

	public string ReadText()
	{
		if (!FileExists())
		{
			throw new Exception("File " + base.Path + " does not exist!");
		}
		return File.ReadAllText(this);
	}

	public Task<string> ReadTextAsync()
	{
		return File.ReadAllTextAsync((string)this, default(CancellationToken));
	}

	public void DeleteFile()
	{
		if (FileExists())
		{
			File.Delete(this);
		}
	}

	public FolderPath GetParent()
	{
		return new FolderPath(GetParentPath());
	}

	public bool FileExists()
	{
		return File.Exists(this);
	}

	public void Move(FilePath path)
	{
		File.Copy(this, path, overwrite: true);
		DeleteFile();
	}

	public void Copy(FilePath path)
	{
		File.Copy(this, path, overwrite: true);
	}

	public static string CleanupName(string fileName)
	{
		char[] invalidPathChars = System.IO.Path.GetInvalidPathChars();
		foreach (char oldChar in invalidPathChars)
		{
			fileName = fileName.Replace(oldChar, '_');
		}
		invalidPathChars = System.IO.Path.GetInvalidFileNameChars();
		foreach (char oldChar2 in invalidPathChars)
		{
			fileName = fileName.Replace(oldChar2, '_');
		}
		return fileName;
	}

	public string GetRelativePath(string root)
	{
		return base.Path.Replace(System.IO.Path.GetFullPath(root).Replace("\\", "/"), "");
	}

	private void Write(Action writeAction)
	{
		if (base.Path != null)
		{
			writeAction();
		}
	}
}
