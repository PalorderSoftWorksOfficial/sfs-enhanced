using SFS.Audio;
using SFS.Input;
using SFS.IO;
using SFS.Parts;
using SFS.Translations;
using SFS.UI;
using SFS.WorldBase;

namespace SFS;

public static class Base
{
	public static LanguageLoader language;

	public static SceneLoader sceneLoader;

	public static WorldBaseManager worldBase;

	public static SoundPlayer sound;

	public static PlanetLoader planetLoader;

	public static PartsLoader partsLoader;

	public static InputManager inputManager;

	public static ScreenTracker screenTracker;

	public static DisableSavingReceiver saver;
}
