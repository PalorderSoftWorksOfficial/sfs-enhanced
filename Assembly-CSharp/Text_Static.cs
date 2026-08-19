using UnityEngine.UI;

public static class Text_Static
{
	public static void SetLines(this Text text, params string[] lines)
	{
		text.Clear();
		foreach (string text2 in lines)
		{
			text.text += ((text.text.Length == 0) ? text2 : ("\n" + text2));
		}
	}

	public static void AddLines(this Text text, params string[] lines)
	{
		foreach (string text2 in lines)
		{
			text.text += ((text.text.Length == 0) ? text2 : ("\n" + text2));
		}
	}

	public static void Clear(this Text text)
	{
		text.text = "";
	}
}
