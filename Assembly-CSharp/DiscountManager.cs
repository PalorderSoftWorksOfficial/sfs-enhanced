using System;
using UnityEngine;

public class DiscountManager : MonoBehaviour
{
	[Serializable]
	public class BuyButtonToggle
	{
		public GameObject fullPriceButton;

		public GameObject discountButton;

		public void Set(bool discount)
		{
			fullPriceButton.SetActive(!discount);
			discountButton.SetActive(discount);
		}
	}

	public BuyButtonToggle parts;

	public BuyButtonToggle bundle;
}
