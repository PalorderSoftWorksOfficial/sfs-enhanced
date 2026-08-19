using System;
using System.Collections.Generic;
using System.Globalization;
using SFS.Builds;
using SFS.Translations;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class VariablesDrawer : MonoBehaviour
{
	[Serializable]
	public class DrawElement
	{
		public int priority;

		public TranslationVariable label;

		public VariableType variableType;

		public Float_Reference floatReference;

		public Bool_Reference boolReference;

		public String_Reference stringReference;

		public FloatDrawType floatDrawType;

		public StringDrawType stringDrawType;

		public string units;

		public float minValue;

		public float maxValue;

		private bool DrawUnits
		{
			get
			{
				if (variableType != VariableType.Float)
				{
					if (variableType == VariableType.String)
					{
						return stringDrawType == StringDrawType.Stat;
					}
					return false;
				}
				return true;
			}
		}

		private bool DrawMinMaxValue
		{
			get
			{
				if (variableType == VariableType.Float)
				{
					return floatDrawType == FloatDrawType.Slider;
				}
				return false;
			}
		}
	}

	public enum VariableType
	{
		Float,
		String,
		Bool
	}

	public enum FloatDrawType
	{
		Stat,
		Slider
	}

	public enum StringDrawType
	{
		Text,
		Stat
	}

	public DrawElement[] elements;

	public void Draw(List<VariablesDrawer> modules, StatsMenu drawer, PartDrawSettings settings)
	{
		if (!settings.build && !settings.game)
		{
			return;
		}
		for (int i = 0; i < elements.Length; i++)
		{
			DrawElement element = elements[i];
			int ii = i;
			switch (element.variableType)
			{
			case VariableType.Float:
				if (element.floatDrawType == FloatDrawType.Stat)
				{
					drawer.DrawStat_Separate(element.priority, () => element.label.Field, GetValue, null, delegate(Action action)
					{
						element.floatReference.OnChange += action;
					}, delegate(Action action)
					{
						element.floatReference.OnChange -= action;
					});
				}
				else if (element.floatDrawType == FloatDrawType.Slider)
				{
					drawer.DrawSlider_Separate(element.priority, () => element.label.Field, GetValue, null, () => (element.floatReference.Value - element.minValue) / (element.maxValue - element.minValue), SetFillAmount, delegate(Action action)
					{
						element.floatReference.OnChange += action;
					}, delegate(Action action)
					{
						element.floatReference.OnChange -= action;
					});
				}
				break;
			case VariableType.Bool:
				drawer.DrawToggle(element.priority, () => element.label.Field, Toggle, () => element.boolReference.Value, delegate(Action action)
				{
					element.boolReference.OnChange += action;
				}, delegate(Action action)
				{
					element.boolReference.OnChange -= action;
				});
				break;
			case VariableType.String:
				if (element.stringDrawType == StringDrawType.Text)
				{
					drawer.DrawText(element.priority, element.stringReference.Value);
				}
				else if (element.stringDrawType == StringDrawType.Stat)
				{
					drawer.DrawStat_Separate(element.priority, () => element.label.Field, () => element.stringReference.Value + (string.IsNullOrWhiteSpace(element.units) ? "" : (" " + element.units)), null, delegate(Action action)
					{
						element.stringReference.OnChange += action;
					}, delegate(Action action)
					{
						element.stringReference.OnChange -= action;
					});
				}
				break;
			}
			string GetValue()
			{
				return element.floatReference.Value.ToString(CultureInfo.InvariantCulture) + (string.IsNullOrWhiteSpace(element.units) ? "" : (" " + element.units));
			}
			void SetFillAmount(float t, bool touchStart)
			{
				Undo.main.RecordStatChangeStep(modules, delegate
				{
					float value = Mathf.Lerp(element.minValue, element.maxValue, t);
					foreach (VariablesDrawer module in modules)
					{
						module.elements[ii].floatReference.Value = value;
					}
				}, touchStart);
			}
			void Toggle()
			{
				Undo.main.RecordStatChangeStep(modules, delegate
				{
					bool value = !element.boolReference.Value;
					foreach (VariablesDrawer module2 in modules)
					{
						module2.elements[ii].boolReference.Value = value;
					}
				});
			}
		}
	}
}
