using UnityEngine;

namespace SFS.UI;

public class Menu : MonoBehaviour
{
	public static ReadMenu read;

	public ReadMenu _read;

	public static BasicMenu settings;

	public BasicMenu _settings;

	public static TextInputMenu textInput;

	public TextInputMenu _textInput;

	public static OptionsMenuDrawer options;

	public OptionsMenuDrawer _options;

	public static LoadMenu load;

	public LoadMenu _load;

	public static LoadingScreen loading;

	public LoadingScreen _loading;

	private void Awake()
	{
		read = _read;
		settings = _settings;
		textInput = _textInput;
		options = _options;
		load = _load;
		loading = _loading;
	}
}
