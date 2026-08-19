using SFS.Audio;
using SFS.Input;
using SFS.IO;
using SFS.Parts;
using SFS.Translations;
using SFS.UI;
using SFS.WorldBase;
using UnityEngine;

namespace SFS;

public class BaseAssigner : MonoBehaviour
{
	public LanguageLoader language;

	public SceneLoader sceneLoader;

	public WorldBaseManager worldBase;

	public SoundPlayer sound;

	public PlanetLoader planetLoader;

	public PartsLoader partsLoader;

	public InputManager inputManager;

	public ScreenTracker screenTracker;

	public DisableSavingReceiver saver;

	private void Awake()
	{
		Base.language = language;
		Base.sceneLoader = sceneLoader;
		Base.worldBase = worldBase;
		Base.sound = sound;
		Base.planetLoader = planetLoader;
		Base.partsLoader = partsLoader;
		Base.inputManager = inputManager;
		Base.screenTracker = screenTracker;
		Base.saver = saver;
	}
}
