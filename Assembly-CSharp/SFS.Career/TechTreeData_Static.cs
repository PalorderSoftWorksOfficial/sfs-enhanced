namespace SFS.Career;

public static class TechTreeData_Static
{
	public static bool IsUnlocked(this I_TechTreeData a)
	{
		TT_Builder.TT_Element tT_Element = TT_Builder.main.elements_Dictionary[a.Name_ID];
		if (tT_Element.HasParent && !tT_Element.parent.asset.IsComplete)
		{
			return a.IsComplete;
		}
		return true;
	}
}
