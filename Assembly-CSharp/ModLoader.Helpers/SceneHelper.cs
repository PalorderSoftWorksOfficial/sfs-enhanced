using UnityEngine.SceneManagement;

namespace ModLoader.Helpers;

public static class SceneHelper
{
	public static OptionalDelegate<Scene> OnHomeSceneLoaded = new OptionalDelegate<Scene>();

	public static OptionalDelegate<Scene> OnHomeSceneUnloaded = new OptionalDelegate<Scene>();

	public static OptionalDelegate<Scene> OnHubSceneLoaded = new OptionalDelegate<Scene>();

	public static OptionalDelegate<Scene> OnHubSceneUnloaded = new OptionalDelegate<Scene>();

	public static OptionalDelegate<Scene> OnBuildSceneLoaded = new OptionalDelegate<Scene>();

	public static OptionalDelegate<Scene> OnBuildSceneUnloaded = new OptionalDelegate<Scene>();

	public static OptionalDelegate<Scene> OnWorldSceneLoaded = new OptionalDelegate<Scene>();

	public static OptionalDelegate<Scene> OnWorldSceneUnloaded = new OptionalDelegate<Scene>();

	public static OptionalDelegate<Scene> OnSceneLoaded = new OptionalDelegate<Scene>();

	public static OptionalDelegate<Scene> OnSceneUnloaded = new OptionalDelegate<Scene>();
}
