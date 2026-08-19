using Beebyte.Obfuscator;
using SFS.Translations;

namespace SFS.WorldBase;

[Skip]
public class WorldMode
{
	[Skip]
	public enum Mode
	{
		Sandbox,
		Career,
		Challenge
	}

	public Mode mode;

	public bool allowQuicksaves = true;

	public bool AllowsCheats => mode == Mode.Sandbox;

	public WorldMode(Mode mode)
	{
		this.mode = mode;
	}

	public string GetModeName()
	{
		return mode switch
		{
			Mode.Sandbox => Loc.main.Mode_Sandbox, 
			Mode.Career => Loc.main.Mode_Career, 
			Mode.Challenge => Loc.main.Mode_Challenge, 
			_ => "", 
		};
	}
}
