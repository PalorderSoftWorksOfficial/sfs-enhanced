using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
	private static T _instance;

	public static T instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindObjectOfType<T>();
				if (_instance == null)
				{
					_instance = new GameObject
					{
						name = typeof(T).Name
					}.AddComponent<T>();
				}
			}
			return _instance;
		}
	}

	protected virtual void Awake()
	{
		if (_instance == null)
		{
			_instance = this as T;
			Object.DontDestroyOnLoad(base.gameObject);
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}
}
