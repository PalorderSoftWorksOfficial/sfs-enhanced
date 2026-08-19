using UnityEngine;

public class DisableAtStart : MonoBehaviour
{
	public GameObject[] a;

	private void Awake()
	{
		GameObject[] array = a;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
	}
}
