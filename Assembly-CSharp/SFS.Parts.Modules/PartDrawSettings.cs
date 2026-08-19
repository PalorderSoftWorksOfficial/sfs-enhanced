namespace SFS.Parts.Modules;

public class PartDrawSettings
{
	public bool showTitle;

	public bool build;

	public bool game;

	public bool showDescription;

	public bool canBoardWorld;

	public static readonly PartDrawSettings PickGridSettings = new PartDrawSettings(showTitle: true, build: false, game: false, showDescription: true);

	public static readonly PartDrawSettings BuildSettings = new PartDrawSettings(showTitle: true, build: true, game: false, showDescription: false);

	public static readonly PartDrawSettings WorldSettings = new PartDrawSettings(showTitle: true, build: false, game: true, showDescription: false);

	private PartDrawSettings(bool showTitle, bool build, bool game, bool showDescription)
	{
		this.showTitle = showTitle;
		this.build = build;
		this.game = game;
		this.showDescription = showDescription;
	}
}
