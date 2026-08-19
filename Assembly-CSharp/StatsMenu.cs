using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.UI;
using UnityEngine;
using UnityEngine.UI;

public class StatsMenu : MonoBehaviour
{
	private class ElementData
	{
		public readonly int priority;

		public readonly int index;

		public readonly Action draw;

		public readonly bool statElement;

		public ElementData(int priority, int index, Action draw, bool statElement)
		{
			this.priority = priority;
			this.index = index;
			this.draw = draw;
			this.statElement = statElement;
		}
	}

	public RectTransform menuHolder;

	public VerticalLayoutGroup statsHolderGroup;

	[Space]
	public VerticalLayout statsHolderLayout;

	public NewElement rootElement;

	[Space]
	public GameObject titleHolder;

	public TextAdapter titleText;

	[Space]
	public PopupAnimation popupAnimation;

	[Space]
	public RectTransform spacePrefab;

	public RectTransform textPrefab;

	public RectTransform statPrefab;

	public RectTransform statBackPrefab;

	public RectTransform togglePrefab;

	public RectTransform sliderPrefab;

	public RectTransform buttonPrefab;

	public RectTransform fullButtonPrefab;

	private List<ElementData> drawQueue = new List<ElementData>();

	private List<GameObject> elements = new List<GameObject>();

	private Func<bool> hasControl;

	private Action onClose;

	private bool disableAtAwake = true;

	private bool HasControl
	{
		get
		{
			if (hasControl != null)
			{
				return hasControl();
			}
			return true;
		}
	}

	private void Awake()
	{
		if (disableAtAwake)
		{
			Close();
		}
		spacePrefab.gameObject.SetActive(value: false);
		textPrefab.gameObject.SetActive(value: false);
		statPrefab.gameObject.SetActive(value: false);
		statBackPrefab.gameObject.SetActive(value: false);
		togglePrefab.gameObject.SetActive(value: false);
		sliderPrefab.gameObject.SetActive(value: false);
		buttonPrefab.gameObject.SetActive(value: false);
		fullButtonPrefab.gameObject.SetActive(value: false);
	}

	public OpenTracker Open(Func<bool> hasControl, Action<StatsMenu> draw, bool skipAnimation, Action onOpen = null, Action onClose = null)
	{
		disableAtAwake = false;
		Close();
		draw(this);
		drawQueue.Sort((ElementData A, ElementData B) => (A.priority != B.priority) ? B.priority.CompareTo(A.priority) : A.index.CompareTo(B.index));
		for (int num = 0; num < drawQueue.Count; num++)
		{
			ElementData elementData = drawQueue[num];
			if (!elementData.statElement)
			{
				elementData.draw();
				continue;
			}
			RectTransform component = CreateElement(statBackPrefab);
			int num2 = 0;
			while (true)
			{
				elementData.draw();
				num2++;
				if (!drawQueue.IsValidIndex(num + 1) || !drawQueue[num + 1].statElement)
				{
					break;
				}
				num++;
				elementData = drawQueue[num];
			}
			RectTransform rectTransform = component.FindByName<RectTransform>("Back");
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, (float)num2 * statPrefab.sizeDelta.y + (float)(num2 - 1) * statsHolderLayout.Spacing);
			CreateElement(spacePrefab);
		}
		drawQueue.Clear();
		this.hasControl = hasControl;
		menuHolder.gameObject.SetActive(value: true);
		if (skipAnimation)
		{
			popupAnimation.SkipAnimation();
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetRect());
		rootElement.ActAsRoot();
		onOpen?.Invoke();
		this.onClose = (Action)Delegate.Combine(this.onClose, onClose);
		return new OpenTracker(Close, ref this.onClose);
	}

	public void Close()
	{
		titleHolder.SetActive(value: false);
		onClose?.Invoke();
		onClose = null;
		GameObject[] array = elements.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object.Destroy(array[i]);
		}
		elements.Clear();
		while (statsHolderLayout.ElementCount > 2)
		{
			statsHolderLayout.RemoveElement(2);
		}
		menuHolder.gameObject.SetActive(value: false);
		if (popupAnimation != null)
		{
			popupAnimation.Disable();
		}
	}

	public void DrawTitle(string title)
	{
		titleHolder.SetActive(value: true);
		titleText.Text = title;
	}

	public void DrawText(int priority, string text)
	{
		Draw(priority, delegate
		{
			CreateElement(textPrefab).FindByName<TextAdapter>("Text").Text = text;
		});
	}

	public void DrawStat(int priority, string labelAndValue, string valueSize_Optional = null)
	{
		Func<string> valueSize_Optional2 = null;
		if (valueSize_Optional != null)
		{
			valueSize_Optional2 = () => valueSize_Optional;
		}
		DrawStat(priority, () => labelAndValue, valueSize_Optional2, null, null);
	}

	public void DrawStat(int priority, Func<string> labelAndValue, Func<string> valueSize_Optional, Action<Action> register, Action<Action> unregister)
	{
		Func<string> valueSize_Optional2 = null;
		if (valueSize_Optional != null)
		{
			valueSize_Optional2 = () => GetStatPart(1, valueSize_Optional());
		}
		DrawStat_Separate(priority, () => GetStatPart(0, labelAndValue()), () => GetStatPart(1, labelAndValue()), valueSize_Optional2, register, unregister);
	}

	public void DrawStat_Separate(int priority, Func<string> label, Func<string> value, Func<string> valueSize_Optional, Action<Action> register, Action<Action> unregister)
	{
		TextAdapter labelText = null;
		TextAdapter valueText = null;
		TextAdapter sizeText = null;
		Draw(priority, delegate
		{
			RectTransform component = CreateElement(statPrefab);
			labelText = component.FindByName<TextAdapter>("Label Text");
			valueText = component.FindByName<TextAdapter>("Value Text");
			sizeText = component.FindByName<TextAdapter>("Value Size");
		}, delegate
		{
			labelText.Text = label().Replace("\n", " ");
			valueText.Text = value();
			sizeText.Text = ((valueSize_Optional != null) ? valueSize_Optional() : valueText.Text);
		}, register, unregister, statElement: true);
	}

	public void DrawToggle(int priority, Func<string> labelText, Action toggle, Func<bool> getValue, Action<Action> register, Action<Action> unregister)
	{
		TextAdapter labelText_UI = null;
		Action updateToggleUI = null;
		Draw(priority, delegate
		{
			RectTransform rectTransform = CreateElement(togglePrefab);
			labelText_UI = rectTransform.FindByName<TextAdapter>("Label Text");
			rectTransform.GetComponentInChildren<ToggleButton>().Bind(delegate
			{
				CheckControl(toggle);
			}, getValue, out updateToggleUI);
		}, delegate
		{
			labelText_UI.Text = labelText().Replace("\n", " ");
			updateToggleUI();
		}, register, unregister);
	}

	public void DrawSlider(int priority, Func<string> labelAndValue, Func<string> valueSize_Optional, Func<float> getFillPercent, Action<float, bool> setFillPercent, Action<Action> register, Action<Action> unregister, bool startUndoStep = false)
	{
		Func<string> valueSize_Optional2 = null;
		if (valueSize_Optional != null)
		{
			valueSize_Optional2 = () => GetStatPart(1, valueSize_Optional());
		}
		DrawSlider_Separate(priority, () => GetStatPart(0, labelAndValue()), () => GetStatPart(1, labelAndValue()), valueSize_Optional2, getFillPercent, setFillPercent, register, unregister);
	}

	public void DrawSlider_Separate(int priority, Func<string> labelText, Func<string> valueText, Func<string> valueSize_Optional, Func<float> getFillPercent, Action<float, bool> setFillPercent, Action<Action> register, Action<Action> unregister, bool startUndoStep = false)
	{
		TextAdapter labelText_UI = null;
		TextAdapter valueText_UI = null;
		TextAdapter sizeText = null;
		Image sliderBack_UI = null;
		Image sliderImage_UI = null;
		float lastOutput = float.NaN;
		bool startedSlide = false;
		float fillPercent;
		Draw(priority, delegate
		{
			RectTransform component = CreateElement(sliderPrefab);
			labelText_UI = component.FindByName<TextAdapter>("Label Text");
			valueText_UI = component.FindByName<TextAdapter>("Value Text");
			sizeText = component.FindByName<TextAdapter>("Value Size");
			sliderBack_UI = component.FindByName<Image>("Back");
			sliderImage_UI = component.FindByName<Image>("Slider");
			fillPercent = getFillPercent();
			component.FindByName<SFS.UI.Button>("Back").onDown += (Action)delegate
			{
				CheckControl(delegate
				{
					fillPercent = getFillPercent();
				});
				startedSlide = true;
			};
			component.FindByName<SFS.UI.Button>("Back").onHold += new Action<OnInputStayData>(OnDrag);
		}, delegate
		{
			labelText_UI.Text = labelText().Replace("\n", " ");
			valueText_UI.Text = valueText();
			sizeText.Text = ((valueSize_Optional != null) ? valueSize_Optional() : valueText_UI.Text);
			sliderImage_UI.transform.localScale = new Vector3(getFillPercent(), 1f, 1f);
		}, register, unregister);
		void OnDrag(OnInputStayData data)
		{
			if (HasControl)
			{
				float x = sliderBack_UI.transform.TransformVector(sliderBack_UI.rectTransform.rect.size).x;
				fillPercent += data.delta.deltaPixel.x / x;
				float num = Mathf.Clamp01(fillPercent.Round(0.05f));
				if (num != lastOutput)
				{
					lastOutput = num;
					setFillPercent(num, startedSlide);
					startedSlide = false;
				}
			}
		}
	}

	public void DrawButton(int priority, string labelText, string buttonText, Action buttonAction, bool enabled = true)
	{
		DrawButton(priority, () => labelText, () => buttonText, buttonAction, () => enabled, null, null);
	}

	public void DrawButton(int priority, Func<string> labelText, Func<string> buttonText, Action buttonAction, Func<bool> enabled, Action<Action> register, Action<Action> unregister)
	{
		TextAdapter labelText_UI = null;
		TextAdapter buttonText_UI = null;
		SFS.UI.Button button = null;
		Draw(priority, delegate
		{
			RectTransform rectTransform = CreateElement(buttonPrefab);
			labelText_UI = rectTransform.FindByName<TextAdapter>("Label Text");
			buttonText_UI = rectTransform.FindByName<TextAdapter>("Text");
			button = rectTransform.GetComponentInChildren<SFS.UI.Button>();
			button.onClick += (Action)delegate
			{
				CheckControl(buttonAction);
				labelText_UI.Text = labelText();
				buttonText_UI.Text = buttonText();
			};
		}, delegate
		{
			labelText_UI.Text = labelText();
			buttonText_UI.Text = buttonText();
			button.SetEnabled(enabled == null || enabled());
		}, register, unregister);
	}

	public void DrawFullButton(int priority, string buttonText, Action buttonAction, bool enabled = true)
	{
		DrawFullButton(priority, () => buttonText, buttonAction, () => enabled, null, null);
	}

	public void DrawFullButton(int priority, Func<string> buttonText, Action buttonAction, Func<bool> enabled, Action<Action> register, Action<Action> unregister)
	{
		TextAdapter buttonText_UI = null;
		SFS.UI.Button button = null;
		Draw(priority, delegate
		{
			RectTransform rectTransform = CreateElement(fullButtonPrefab);
			buttonText_UI = rectTransform.FindByName<TextAdapter>("Text");
			button = rectTransform.GetComponentInChildren<SFS.UI.Button>();
			button.onClick += (Action)delegate
			{
				CheckControl(buttonAction);
			};
		}, delegate
		{
			buttonText_UI.Text = buttonText();
			button.SetEnabled(enabled == null || enabled());
		}, register, unregister);
	}

	public void DrawSpace(int priority, float height = 5f)
	{
		if (priority == int.MinValue)
		{
			priority = ((drawQueue.Count > 0) ? drawQueue.Last().priority : 0);
		}
		Draw(priority, delegate
		{
			RectTransform rectTransform = CreateElement(spacePrefab);
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
		});
	}

	private void Draw(int priority, Action createAction, Action updateAction, Action<Action> register, Action<Action> unregister, bool statElement = false)
	{
		bool created = false;
		Draw(priority, delegate
		{
			register?.Invoke(Update);
			createAction();
			created = true;
			Update();
			onClose = (Action)Delegate.Combine(onClose, (Action)delegate
			{
				unregister?.Invoke(Update);
			});
		}, statElement);
		void Update()
		{
			if (created)
			{
				updateAction();
			}
		}
	}

	private void Draw(int priority, Action drawAction, bool statElement = false)
	{
		drawQueue.Add(new ElementData(priority, drawQueue.Count, drawAction, statElement));
	}

	private T CreateElement<T>(T obj) where T : Component
	{
		T val = UnityEngine.Object.Instantiate(obj, statsHolderGroup.transform);
		val.gameObject.SetActive(value: true);
		elements.Add(val.gameObject);
		statsHolderLayout.AddElement(val.GetComponent<NewElement>());
		return val;
	}

	private static string GetStatPart(int partIndex, string labelAndValue)
	{
		if (labelAndValue.Contains(':'))
		{
			return labelAndValue.Split(':', StringSplitOptions.None)[partIndex];
		}
		if (labelAndValue.Contains('：'))
		{
			return labelAndValue.Split('：', StringSplitOptions.None)[partIndex];
		}
		return "ERROR";
	}

	private void CheckControl(Action action)
	{
		if (HasControl)
		{
			action();
		}
	}
}
