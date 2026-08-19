using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseUtility : MonoBehaviour
{
	public static FirebaseUtility main;

	public bool firebaseReady;

	public bool logs;

	public Action<bool> initCallback;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(delegate(Task<DependencyStatus> task)
		{
			DependencyStatus result = task.Result;
			if (logs)
			{
				Debug.Log($"Firebase successfully initialized: {result == DependencyStatus.Available}");
			}
			firebaseReady = result == DependencyStatus.Available;
			initCallback?.Invoke(result == DependencyStatus.Available);
		});
	}
}
