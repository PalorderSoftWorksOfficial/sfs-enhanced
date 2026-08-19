using System;

namespace SFS.UI;

public class TextBuilder : NewElementBuilder<TextBuilder>
{
	private Func<string> text;

	private TextBuilder(NewElement prefab)
		: base(prefab)
	{
	}

	public static TextBuilder CreateText(Func<string> text)
	{
		return new TextBuilder(ElementGenerator.main.defaultText)
		{
			text = text
		};
	}

	protected override void OnCreated(NewElement element)
	{
		element.GetComponentInChildren<TextAdapter>().Text = text();
	}
}
