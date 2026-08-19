using System;
using System.Collections;
using ModLoader.Helpers;
using SFS.Builds;
using SFS.Career;
using SFS.Platform;
using SFS.UI;
using SFS.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFS;

public class SceneLoader : MonoBehaviour
{
	[Serializable]
	public class Settings
	{
		public string openShop;

		public bool askBuildNew;

		public bool launch;
	}

	public Camera loadingCamera;

	public Settings sceneSettings = new Settings();

	public bool isUnloading;

	private bool loading;

	private static bool firstLoad = true;

	public Action onSceneUnload_Once;

	private static string BaseSceneName => "Base";

	private static string HomeSceneName => "Home";

	private static string HubSceneName => "Hub";

	private static string BuildSceneName => "Build";

	private static string WorldSceneName => "World";

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Initialize()
	{
		Application.backgroundLoadingPriority = ThreadPriority.Low;
		string text = BaseSceneName + "_" + PlatformType.PC;
		bool flag = false;
		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			flag = SceneManager.GetSceneAt(i).name == text;
			if (flag)
			{
				break;
			}
		}
		if (!flag)
		{
			SceneManager.LoadScene(text, LoadSceneMode.Additive);
		}
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += delegate(Scene scene, LoadSceneMode _)
		{
			if (scene.name != BaseSceneName + "_" + PlatformType.PC)
			{
				SceneManager.SetActiveScene(scene);
			}
		};
		if (firstLoad)
		{
			SceneHelper.OnHomeSceneLoaded?.Invoke(SceneManager.GetActiveScene());
			SceneHelper.OnSceneLoaded?.Invoke(SceneManager.GetActiveScene());
			firstLoad = false;
		}
	}

	private void OnApplicationQuit()
	{
		isUnloading = true;
	}

	public void LoadHomeScene(string openShop)
	{
		Base.worldBase.ExitWorld();
		LoadScene(HomeSceneName, new Settings
		{
			openShop = openShop
		});
	}

	public void LoadHubScene()
	{
		LoadScene(HubSceneName, new Settings());
	}

	public void LoadBuildScene(bool askBuildNew)
	{
		LoadScene(BuildSceneName, new Settings
		{
			askBuildNew = askBuildNew
		});
	}

	public void LoadWorldScene(bool launch = false)
	{
		LoadScene(WorldSceneName, new Settings
		{
			launch = launch
		});
	}

	private void LoadScene(string sceneName, Settings sceneSettings)
	{
		if (!loading)
		{
			onSceneUnload_Once?.Invoke();
			onSceneUnload_Once = null;
			this.sceneSettings = sceneSettings;
			StartCoroutine(LoadSceneAsync(sceneName + "_" + PlatformType.PC));
		}
	}

	private IEnumerator LoadSceneAsync(string sceneName)
	{
		loading = true;
		Menu.loading.Open();
		Base.sound.Clear3DSounds();
		loadingCamera.enabled = true;
		isUnloading = true;
		SceneHelper.OnSceneUnloaded?.Invoke(SceneManager.GetActiveScene());
		switch (SceneManager.GetActiveScene().name)
		{
		case "Home_PC":
			SceneHelper.OnHomeSceneUnloaded?.Invoke(SceneManager.GetActiveScene());
			break;
		case "Build_PC":
			SceneHelper.OnBuildSceneUnloaded?.Invoke(SceneManager.GetActiveScene());
			break;
		case "Hub_PC":
			SceneHelper.OnHubSceneUnloaded?.Invoke(SceneManager.GetActiveScene());
			break;
		case "World_PC":
			SceneHelper.OnWorldSceneUnloaded?.Invoke(SceneManager.GetActiveScene());
			break;
		}
		AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
		while (!asyncUnload.isDone)
		{
			yield return null;
		}
		isUnloading = false;
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		while (!asyncLoad.isDone)
		{
			yield return null;
		}
		loadingCamera.enabled = false;
		Menu.loading.Close();
		loading = false;
		SceneHelper.OnSceneLoaded?.Invoke(SceneManager.GetActiveScene());
		switch (sceneName)
		{
		case "Home_PC":
			SceneHelper.OnHomeSceneLoaded?.Invoke(SceneManager.GetActiveScene());
			break;
		case "Build_PC":
			SceneHelper.OnBuildSceneLoaded?.Invoke(SceneManager.GetActiveScene());
			break;
		case "Hub_PC":
			SceneHelper.OnHubSceneLoaded?.Invoke(SceneManager.GetActiveScene());
			break;
		case "World_PC":
			SceneHelper.OnWorldSceneLoaded?.Invoke(SceneManager.GetActiveScene());
			break;
		}
	}

	public static void ExitToMainMenu()
	{
		if (GameManager.main != null)
		{
			GameManager.main.ExitToMainMenu();
		}
		if (BuildManager.main != null)
		{
			BuildManager.main.ExitToHome();
		}
		if (HubManager.main != null)
		{
			HubManager.main.ExitToMainMenu();
		}
	}
}
