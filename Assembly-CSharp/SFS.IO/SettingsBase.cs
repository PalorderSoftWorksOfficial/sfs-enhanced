using SFS.Parsers.Json;
using UnityEngine;

namespace SFS.IO;

public abstract class SettingsBase<T> : MonoBehaviour where T : new()
{
	public T settings;

	protected abstract string FileName { get; }

	private FilePath Path => FileLocations.GetSettingsPath(FileName);

	protected abstract void OnLoad();

	protected void Save()
	{
		FilePath path = Path;
		string text = JsonWrapper.ToJson(settings, pretty: true);
		path.WriteText(text);
	}

	protected void Load()
	{
		FilePath path = Path;
		settings = (path.FileExists() ? JsonWrapper.FromJson<T>(path.ReadText()) : new T());
		OnLoad();
	}
}
