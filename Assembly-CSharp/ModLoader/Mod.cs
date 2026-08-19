using System;
using System.Collections.Generic;

namespace ModLoader;

public abstract class Mod
{
	private string assetsFilename;

	public string ModFolder { get; set; }

	public abstract string ModNameID { get; }

	public abstract string DisplayName { get; }

	public abstract string Author { get; }

	public abstract string MinimumGameVersionNecessary { get; }

	public abstract string ModVersion { get; }

	public abstract string Description { get; }

	public virtual string IconLink => null;

	public virtual Dictionary<string, string> Dependencies => null;

	public virtual Action LoadKeybindings => null;

	public virtual void Early_Load()
	{
	}

	public virtual void Load()
	{
	}
}
