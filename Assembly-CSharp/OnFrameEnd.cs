using System;
using UnityEngine;

public class OnFrameEnd : MonoBehaviour
{
	public static OnFrameEnd main;

	public Action onBeforeFrameEnd_Once;

	public Transform frameTransform;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
	}

	private void LateUpdate()
	{
		onBeforeFrameEnd_Once?.Invoke();
		onBeforeFrameEnd_Once = null;
	}
}
