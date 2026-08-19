using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SFS.UI.ModGUI;

public static class Builder
{
	public enum SceneToAttach
	{
		BaseScene,
		CurrentScene
	}

	private static readonly HashSet<int> RandomIDs = new HashSet<int>();

	private static readonly Dictionary<int, Vector2> windowPositions = new Dictionary<int, Vector2>();

	public static int GetRandomID()
	{
		int num;
		do
		{
			num = UnityEngine.Random.Range(0, int.MaxValue);
		}
		while (RandomIDs.Contains(num));
		RandomIDs.Add(num);
		return num;
	}

	public static GameObject CreateHolder(SceneToAttach mode, string name)
	{
		GameObject gameObject = new GameObject(name);
		AttachToCanvas(gameObject, mode);
		return gameObject;
	}

	public static ButtonWithLabel CreateButtonWithLabel(Transform parent, int width, int height, int posX = 0, int posY = 0, string labelText = "", string buttonText = "", Action onClick = null)
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.buttonWithLabelPrefab);
		ButtonWithLabel buttonWithLabel = new ButtonWithLabel();
		buttonWithLabel.Init(self, parent);
		buttonWithLabel.Size = new Vector2(width, height);
		buttonWithLabel.Position = new Vector2(posX, posY);
		buttonWithLabel.label.Text = labelText;
		buttonWithLabel.button.Text = buttonText;
		if (onClick != null)
		{
			Button button = buttonWithLabel.button;
			button.OnClick = (Action)Delegate.Combine(button.OnClick, onClick);
		}
		return buttonWithLabel;
	}

	public static ToggleWithLabel CreateToggleWithLabel(Transform parent, int width, int height, Func<bool> getToggleValue, Action onChange = null, int posX = 0, int posY = 0, string labelText = "")
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.toggleWithLabelPrefab);
		ToggleWithLabel toggleWithLabel = new ToggleWithLabel();
		toggleWithLabel.Init(self, parent);
		toggleWithLabel.Size = new Vector2(width, height);
		toggleWithLabel.Position = new Vector2(posX, posY);
		toggleWithLabel.label.Text = labelText;
		toggleWithLabel.toggle.toggleButton.Bind(onChange ?? ((Action)delegate
		{
		}), getToggleValue);
		return toggleWithLabel;
	}

	public static InputWithLabel CreateInputWithLabel(Transform parent, int width, int height, int posX = 0, int posY = 0, string labelText = "", string inputText = "", UnityAction<string> onInputChange = null)
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.inputWithLabelPrefab);
		InputWithLabel inputWithLabel = new InputWithLabel();
		inputWithLabel.Init(self, parent);
		inputWithLabel.Size = new Vector2(width, height);
		inputWithLabel.Position = new Vector2(posX, posY);
		inputWithLabel.label.Text = labelText;
		inputWithLabel.textInput.Text = inputText;
		if (onInputChange != null)
		{
			TextInput textInput = inputWithLabel.textInput;
			textInput.OnChange = (UnityAction<string>)Delegate.Combine(textInput.OnChange, onInputChange);
		}
		return inputWithLabel;
	}

	public static Window CreateWindow(Transform parent, int ID, int width, int height, int posX = 0, int posY = 0, bool draggable = false, bool savePosition = true, float opacity = 1f, string titleText = "")
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.windowPrefab);
		Window window = new Window();
		window.Init(self, parent);
		window.ID = ID;
		window.Size = new Vector2(width, height);
		if (!windowPositions.ContainsKey(ID) & savePosition)
		{
			windowPositions.Add(ID, new Vector2(posX, posY));
		}
		if (savePosition & draggable)
		{
			window.gameObject.GetComponent<DraggableWindowModule>().OnDropAction += delegate
			{
				windowPositions[ID] = window.Position;
			};
		}
		window.Position = (savePosition ? windowPositions[ID] : new Vector2(posX, posY));
		window.Draggable = draggable;
		window.WindowOpacity = opacity;
		window.Title = titleText;
		return window;
	}

	public static Button CreateButton(Transform parent, int width, int height, int posX = 0, int posY = 0, Action onClick = null, string text = "")
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.buttonPrefab);
		Button button = new Button();
		button.Init(self, parent);
		button.Size = new Vector2(width, height);
		button.Position = new Vector2(posX, posY);
		if (onClick != null)
		{
			button.OnClick = (Action)Delegate.Combine(button.OnClick, onClick);
		}
		button.Text = text;
		return button;
	}

	public static TextInput CreateTextInput(Transform parent, int width, int height, int posX = 0, int posY = 0, string text = "", UnityAction<string> onChange = null)
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.inputFieldPrefab);
		TextInput textInput = new TextInput();
		textInput.Init(self, parent);
		textInput.Size = new Vector2(width, height);
		textInput.Position = new Vector2(posX, posY);
		textInput.Text = text;
		if (onChange != null)
		{
			textInput.OnChange = onChange;
		}
		return textInput;
	}

	public static Toggle CreateToggle(Transform parent, Func<bool> getToggleValue, int posX = 0, int posY = 0, Action onChange = null)
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.togglePrefab);
		Toggle toggle = new Toggle();
		toggle.Init(self, parent);
		toggle.Position = new Vector2(posX, posY);
		toggle.toggleButton.Bind(onChange ?? ((Action)delegate
		{
		}), getToggleValue);
		return toggle;
	}

	public static Label CreateLabel(Transform parent, int width, int height, int posX = 0, int posY = 0, string text = "")
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.labelPrefab);
		Label label = new Label();
		label.Init(self, parent);
		label.Size = new Vector2(width, height);
		label.Position = new Vector2(posX, posY);
		label.Text = text;
		return label;
	}

	public static Container CreateContainer(Transform parent, int posX = 0, int posY = 0)
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.containerPrefab);
		Container container = new Container();
		container.Init(self, parent);
		container.Position = new Vector2(posX, posY);
		return container;
	}

	public static Box CreateBox(Transform parent, int width, int height, int posX = 0, int posY = 0, float opacity = 0.3f)
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.boxPrefab);
		Box box = new Box();
		box.Init(self, parent);
		box.Opacity = opacity;
		box.Size = new Vector2(width, height);
		box.Position = new Vector2(posX, posY);
		return box;
	}

	public static WebImage CreateWebImage(Transform parent, int width, int height, int posX = 0, int posY = 0, string link = null, bool useMask = true, SDWebImage.LoadingIndicatorType loadingIndicatorType = SDWebImage.LoadingIndicatorType.Circles, bool preserveAspect = false)
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.webImage);
		WebImage webImage = new WebImage();
		webImage.Init(self, parent);
		webImage.Size = new Vector2(width, height);
		webImage.Position = new Vector2(posX, posY);
		webImage.LoadingIndicatorType = loadingIndicatorType;
		webImage.PreserveAspect = preserveAspect;
		webImage.UseMask = useMask;
		if (!string.IsNullOrWhiteSpace(link))
		{
			webImage.ImageLink = link;
		}
		return webImage;
	}

	public static Separator CreateSeparator(Transform parent, int width, int posX = 0, int posY = 0)
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.separatorPrefab);
		Separator separator = new Separator();
		separator.Init(self, parent);
		separator.Size = new Vector2(width, 10f);
		separator.Position = new Vector2(posX, posY);
		return separator;
	}

	public static Space CreateSpace(Transform parent, int width, int height)
	{
		Space space = new Space();
		space.Init(new GameObject("Space"), parent);
		space.Size = new Vector2(width, height);
		return space;
	}

	public static Slider CreateSlider(Transform parent, int size, float value, (float min, float max) minMaxValue, bool wholeNumbers = false, UnityAction<float> onSliderChange = null, Func<float, string> getValueWithUnits = null)
	{
		GameObject self = UnityEngine.Object.Instantiate(ModGUIPrefabsLoader.main.sliderPrefab);
		Slider slider = new Slider();
		slider.Init(self, parent);
		slider.Size = new Vector2(size, 50f);
		slider.MinMaxValue = minMaxValue;
		slider.WholeNumbersOnly = wholeNumbers;
		if (onSliderChange != null)
		{
			slider.OnSliderChanged = onSliderChange;
		}
		slider.GetValueWithUnit = getValueWithUnits ?? ((Func<float, string>)((float f) => ""));
		slider.Value = value;
		return slider;
	}

	public static void AttachToCanvas(GameObject holder, SceneToAttach mode)
	{
		switch (mode)
		{
		case SceneToAttach.BaseScene:
		{
			GameObject[] rootGameObjects = SceneManager.GetSceneByName("Base_PC").GetRootGameObjects();
			foreach (GameObject gameObject2 in rootGameObjects)
			{
				if (gameObject2.name == "UI")
				{
					holder.transform.SetParent(gameObject2.transform, worldPositionStays: false);
					break;
				}
			}
			break;
		}
		case SceneToAttach.CurrentScene:
		{
			GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
			foreach (GameObject gameObject in rootGameObjects)
			{
				if (gameObject.name == "--- UI ---")
				{
					holder.transform.SetParent(gameObject.transform, worldPositionStays: false);
					break;
				}
			}
			break;
		}
		}
	}
}
