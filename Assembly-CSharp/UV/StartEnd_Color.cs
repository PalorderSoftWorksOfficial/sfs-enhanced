namespace UV;

public class StartEnd_Color
{
	public Color2 color_Edge;

	public Line height;

	public StartEnd_Color(Color2 color_Edge, Line height)
	{
		this.color_Edge = color_Edge;
		this.height = height;
	}

	public StartEnd_Color Cut(Line cut)
	{
		Line line = new Line(height.InverseLerp(cut.start), height.InverseLerp(cut.end));
		return new StartEnd_Color(new Color2(color_Edge.Lerp(line.start), color_Edge.Lerp(line.end)), cut);
	}
}
