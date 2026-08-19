using System;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public abstract class NewElement : MonoBehaviour
{
	private class Cached<T>
	{
		private Func<T> getInstance;

		private bool gotValue;

		private bool missingValue;

		private T value;

		public bool HasValue
		{
			get
			{
				TryGetValue();
				return !missingValue;
			}
		}

		public T Value
		{
			get
			{
				TryGetValue();
				return value;
			}
		}

		public Cached(Func<T> getInstance)
		{
			this.getInstance = getInstance;
		}

		private void TryGetValue()
		{
			if (!gotValue)
			{
				ForceUpdate();
			}
		}

		public void ForceUpdate()
		{
			value = getInstance();
			missingValue = value == null;
			gotValue = true;
		}
	}

	private Cached<NewPadding> cachedPadding;

	private Cached<Margin> cachedMargin;

	private Cached<ForceUpdateLayout> cachedForceUpdate;

	private Cached<MultiCycleUI> cachedMutliCycle;

	private Cached<FitBetween> cachedFitBetween;

	private NewElement parent;

	private bool cyclingUpdate;

	private bool isUpdating;

	protected NewElement()
	{
		cachedPadding = new Cached<NewPadding>(base.GetComponent<NewPadding>);
		cachedMargin = new Cached<Margin>(base.GetComponent<Margin>);
		cachedForceUpdate = new Cached<ForceUpdateLayout>(base.GetComponent<ForceUpdateLayout>);
		cachedMutliCycle = new Cached<MultiCycleUI>(base.GetComponent<MultiCycleUI>);
		cachedFitBetween = new Cached<FitBetween>(base.GetComponent<FitBetween>);
	}

	public void SetSize(Vector2 size)
	{
		SetSize_Internal(ApplyClamp(ApplyOutsideModifiers(size, -1)));
		if (cachedMutliCycle.HasValue && !cyclingUpdate)
		{
			cyclingUpdate = true;
			for (int i = 0; i < cachedMutliCycle.Value.cycleCount; i++)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetRect());
				UpdateHierarchy();
			}
			cyclingUpdate = false;
		}
	}

	public Vector2 GetPreferredSize()
	{
		return ApplyClamp(ApplyOutsideModifiers(GetPreferredSize_Internal(), 1));
	}

	protected abstract Vector2 GetPreferredSize_Internal();

	protected abstract void SetSize_Internal(Vector2 size);

	public virtual void ActAsRoot()
	{
		Debug.LogError(base.name + " cannot be root", this);
	}

	private void Start()
	{
		if (parent == null)
		{
			ActAsRoot();
		}
	}

	public void UpdateHierarchy()
	{
		if (!isUpdating)
		{
			isUpdating = true;
			if (parent != null)
			{
				parent.UpdateHierarchy();
			}
			else
			{
				ActAsRoot();
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetRect());
			if (cachedForceUpdate.HasValue)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(cachedForceUpdate.Value.toUpdate);
				cachedForceUpdate.Value.toUpdate.gameObject.SetActive(value: false);
				cachedForceUpdate.Value.toUpdate.gameObject.SetActive(value: true);
			}
			isUpdating = false;
		}
	}

	public void SetParent(NewElement parent)
	{
		this.parent = parent;
	}

	protected Vector2 ApplyInsideModifiers(Vector2 size, int direction)
	{
		if (cachedPadding.HasValue)
		{
			size.x += cachedPadding.Value.horizontal * (float)direction;
			size.y += cachedPadding.Value.vertical * (float)direction;
		}
		return size;
	}

	private Vector2 ApplyOutsideModifiers(Vector2 size, int direction)
	{
		if (cachedMargin.HasValue)
		{
			size.x += cachedMargin.Value.horizontal * (float)direction;
			size.y += cachedMargin.Value.vertical * (float)direction;
		}
		return size;
	}

	private Vector2 ApplyClamp(Vector2 size)
	{
		ClampSize component = GetComponent<ClampSize>();
		if (component != null)
		{
			size = component.ApplyClamp(size);
		}
		if (cachedFitBetween.HasValue)
		{
			size = cachedFitBetween.Value.ClampSize(size);
		}
		if (float.IsNaN(size.x) || float.IsNaN(size.y))
		{
			throw new Exception("NaN!");
		}
		return size;
	}

	protected Vector2 GetRectSize()
	{
		return this.GetRect().rect.size;
	}

	protected void SetRectSize(Vector2 size)
	{
		RectTransform rect = this.GetRect();
		rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
		rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
	}
}
