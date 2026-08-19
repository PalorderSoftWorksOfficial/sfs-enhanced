using UnityEngine;

namespace SFS.UI;

public abstract class NewElementBuilder<T> : ElementBuilder where T : NewElementBuilder<T>
{
	private readonly NewElement element;

	private float? minWidth;

	private float? minHeight;

	private float? maxWidth;

	private float? maxHeight;

	private SizeSyncerBuilder.Carrier syncCarrier;

	protected NewElementBuilder(NewElement element)
	{
		this.element = element;
	}

	public T MinSize(float? minWidth, float? minHeight)
	{
		this.minWidth = minWidth;
		this.minHeight = minHeight;
		return (T)this;
	}

	public T MaxSize(float? maxWidth, float? maxHeight)
	{
		this.maxWidth = maxWidth;
		this.maxHeight = maxHeight;
		return (T)this;
	}

	public T SizeSync(SizeSyncerBuilder.Carrier carrier)
	{
		syncCarrier = carrier;
		return (T)this;
	}

	protected override void CreateElement(GameObject holder)
	{
		NewElement newElement = Object.Instantiate(element, holder.transform);
		newElement.name = "Instantiation of: " + element.name;
		OnCreated(newElement);
		ClampSize component = newElement.GetComponent<ClampSize>();
		component.minWidth.Value = minWidth;
		component.maxWidth.Value = maxWidth;
		component.minHeight.Value = minHeight;
		component.maxHeight.Value = maxHeight;
		if (syncCarrier != null)
		{
			syncCarrier.sizeSyncGroup.children.Add(newElement);
			newElement.SetParent(syncCarrier.sizeSyncGroup);
		}
	}

	protected abstract void OnCreated(NewElement element);
}
