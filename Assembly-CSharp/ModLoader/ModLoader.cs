using UnityEngine;

namespace ModLoader;

public class ModLoader : MonoBehaviour
{
	public Loader loader;

	private void Awake()
	{
		if (DevSettings.HasModLoader)
		{
			Loader.main = loader;
			loader.Initialize_EarlyLoad();
		}
	}

	private void Start()
	{
		if (DevSettings.HasModLoader)
		{
			loader.Initialize_Load();
		}
	}
}
