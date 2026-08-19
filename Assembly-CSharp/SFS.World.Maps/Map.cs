using UnityEngine;

namespace SFS.World.Maps;

public class Map : MonoBehaviour
{
	public static MapManager manager;

	public MapManager _manager;

	public static MapView view;

	public MapView _view;

	public static MapNavigation navigation;

	public MapNavigation _navigation;

	public static MapEnvironment environment;

	public MapEnvironment _environment;

	public static MapDrawer drawer;

	public MapDrawer _drawer;

	public static ElementDrawer elementDrawer;

	public ElementDrawer _elementDrawer;

	public static LineDrawer solidLine;

	public LineDrawer _solidLine;

	public static LineDrawer dashedLine;

	public LineDrawer _dashedLine;

	private void Awake()
	{
		manager = _manager;
		view = _view;
		navigation = _navigation;
		environment = _environment;
		drawer = _drawer;
		elementDrawer = _elementDrawer;
		solidLine = _solidLine;
		dashedLine = _dashedLine;
	}
}
