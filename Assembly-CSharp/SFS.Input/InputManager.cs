using System;
using System.Collections.Generic;
using System.Linq;
using SFS.UI;
using UnityEngine;

namespace SFS.Input;

public class InputManager : MonoBehaviour
{
	[Serializable]
	public class InputState
	{
		public I_Touchable element;

		public Vector2 touchDownPosPixel;

		public float touchDownTime;

		public bool hasMoved;

		public Vector2 lastTouchPosPixel;

		public Vector2 deltaPixel;

		public InputState(I_Touchable element, Vector2 touchDownPosPixel, float touchDownTime, Vector2 lastTouchPosPixel)
		{
			this.element = element;
			this.touchDownPosPixel = touchDownPosPixel;
			this.touchDownTime = touchDownTime;
			this.lastTouchPosPixel = lastTouchPosPixel;
		}
	}

	private InputState[] state;

	private I_Touchable mouseOverElement;

	public event Action onReset;

	private void Start()
	{
		state = new InputState[(Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer) ? 5 : 2];
	}

	private void Update()
	{
		onReset?.Invoke();
		onReset = null;
		if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
		{
			GetTouchInputs();
		}
		else
		{
			GetMouseInputs();
		}
	}

	private void GetTouchInputs()
	{
		Dictionary<I_Touchable, (Vector2, int)> delta = new Dictionary<I_Touchable, (Vector2, int)>();
		Dictionary<I_Touchable, List<InputState>> dictionary = new Dictionary<I_Touchable, List<InputState>>();
		Touch[] touches = UnityEngine.Input.touches;
		for (int i = 0; i < touches.Length; i++)
		{
			Touch touch = touches[i];
			TouchPosition touchPosition = new TouchPosition(touch.position);
			if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
			{
				TouchEnd(touch.fingerId, InputType.Touch, touchPosition);
				continue;
			}
			if (touch.phase == TouchPhase.Began)
			{
				InputStart(touch.fingerId, InputType.Touch, touchPosition);
			}
			InputStay(touch.fingerId, InputType.Touch, touchPosition, ref delta);
			I_Touchable element = state[touch.fingerId].element;
			if (element != null)
			{
				if (!dictionary.ContainsKey(element))
				{
					dictionary.Add(element, new List<InputState>());
				}
				dictionary[element].Add(state[touch.fingerId]);
			}
		}
		ApplyDrag(delta);
		ApplyZoom(dictionary);
	}

	private void GetMouseInputs()
	{
		Dictionary<I_Touchable, (Vector2 delta, int count)> delta = new Dictionary<I_Touchable, (Vector2, int)>();
		ProcessMouseButton(0, InputType.MouseLeft);
		ProcessMouseButton(1, InputType.MouseRight);
		CheckMouseOverState(new TouchPosition(UnityEngine.Input.mousePosition));
		ApplyDrag(delta);
		if (UnityEngine.Input.mouseScrollDelta.y != 0f)
		{
			Button button = TouchElements.RaycastElements_MouseScroll(new TouchPosition(UnityEngine.Input.mousePosition));
			if (button != null)
			{
				button.onScroll.Invoke(0f - UnityEngine.Input.mouseScrollDelta.y);
			}
			else
			{
				TouchElements.elements.Last()?.OnZoom(new ZoomData(1f - UnityEngine.Input.mouseScrollDelta.y * 0.1f, new TouchPosition(UnityEngine.Input.mousePosition)));
			}
		}
		void ProcessMouseButton(int index, InputType inputType)
		{
			if (UnityEngine.Input.GetMouseButtonUp(index))
			{
				TouchEnd(index, inputType, new TouchPosition(UnityEngine.Input.mousePosition));
			}
			if (UnityEngine.Input.GetMouseButtonDown(index))
			{
				InputStart(index, inputType, new TouchPosition(UnityEngine.Input.mousePosition));
			}
			if (UnityEngine.Input.GetMouseButton(index))
			{
				InputStay(index, inputType, new TouchPosition(UnityEngine.Input.mousePosition), ref delta);
			}
		}
	}

	private void CheckMouseOverState(TouchPosition touchPosition)
	{
		I_Touchable i_Touchable = TouchElements.RaycastElements(touchPosition);
		if (i_Touchable != mouseOverElement)
		{
			mouseOverElement?.OnMouseExit();
			i_Touchable?.OnMouseEnter();
			mouseOverElement = i_Touchable;
		}
	}

	private void InputStart(int inputIndex, InputType inputType, TouchPosition touchPosition)
	{
		I_Touchable i_Touchable = TouchElements.RaycastElements(touchPosition);
		state[inputIndex] = new InputState(i_Touchable, touchPosition.pixel, Time.unscaledTime, touchPosition.pixel);
		i_Touchable?.OnInputStart(new OnInputStartData(inputType, touchPosition));
	}

	private void InputStay(int inputIndex, InputType inputType, TouchPosition touchPosition, ref Dictionary<I_Touchable, (Vector2 delta, int count)> delta)
	{
		Vector2 lastTouchPosPixel = state[inputIndex].lastTouchPosPixel;
		Vector2 vector = touchPosition.pixel - lastTouchPosPixel;
		state[inputIndex].lastTouchPosPixel = touchPosition.pixel;
		state[inputIndex].deltaPixel = vector;
		I_Touchable element = state[inputIndex].element;
		if (element == null)
		{
			return;
		}
		element.OnInputStay(new OnInputStayData(inputType, touchPosition, new DragData(vector)));
		if (inputType == InputType.Touch || inputType == InputType.MouseLeft || inputType == InputType.MouseRight)
		{
			if (!delta.ContainsKey(element))
			{
				delta.Add(element, (Vector2.zero, 0));
			}
			delta[element] = (delta[element].delta + vector, delta[element].count + 1);
		}
		if (!state[inputIndex].hasMoved && HasMoved(state[inputIndex], touchPosition))
		{
			state[inputIndex].hasMoved = true;
			element.OnNotStationary(new OnNotStationary(inputType));
		}
		if (!state[inputIndex].hasMoved)
		{
			float num = Time.unscaledTime - state[inputIndex].touchDownTime;
			if (num > 0.45f && num - Time.unscaledDeltaTime <= 0.45f && (inputType == InputType.Touch || (inputType == InputType.MouseLeft && element is ButtonPC { useLongClick: not false }) || Application.isEditor) && GetTouchesCountOnElement(element) == 1)
			{
				element.OnTouchLongClick(new OnTouchLongClickData(touchPosition));
			}
		}
	}

	private void TouchEnd(int inputIndex, InputType inputType, TouchPosition touchPosition)
	{
		if (state[inputIndex] != null)
		{
			I_Touchable element = state[inputIndex].element;
			if (element != null)
			{
				bool click = Time.unscaledTime - state[inputIndex].touchDownTime < 0.35f && !HasMoved(state[inputIndex], touchPosition);
				element.OnInputEnd(new OnInputEndData(inputType, touchPosition, click));
			}
			state[inputIndex] = null;
		}
	}

	private static void ApplyDrag(Dictionary<I_Touchable, (Vector2 delta, int count)> delta)
	{
		foreach (KeyValuePair<I_Touchable, (Vector2, int)> deltum in delta)
		{
			if (deltum.Value.Item2 != 0)
			{
				Vector2 vector = deltum.Value.Item1 / deltum.Value.Item2;
				deltum.Key.OnDrag(new DragData(-vector));
			}
		}
	}

	private static void ApplyZoom(Dictionary<I_Touchable, List<InputState>> zoom)
	{
		foreach (KeyValuePair<I_Touchable, List<InputState>> item in zoom)
		{
			if (item.Value.Count >= 2)
			{
				InputState inputState = item.Value[item.Value.Count - 2];
				InputState inputState2 = item.Value[item.Value.Count - 1];
				float num = Vector2.Distance(inputState.lastTouchPosPixel, inputState2.lastTouchPosPixel);
				float num2 = Vector2.Distance(inputState.lastTouchPosPixel + inputState.deltaPixel, inputState2.lastTouchPosPixel + inputState2.deltaPixel);
				float zoomDelta = 1f + (num / num2 - 1f) * 1.5f;
				Vector2 pixel = (inputState.lastTouchPosPixel + inputState.deltaPixel + (inputState2.lastTouchPosPixel + inputState2.deltaPixel)) / 2f;
				item.Key.OnZoom(new ZoomData(zoomDelta, new TouchPosition(pixel)));
			}
		}
	}

	public static int GetTouchesCountOnElement(I_Touchable touchable)
	{
		return Base.inputManager.state.Count((InputState a) => a != null && a.element == touchable);
	}

	private static bool HasMoved(InputState a, TouchPosition b)
	{
		return (a.touchDownPosPixel - b.pixel).magnitude > (float)(Screen.width + Screen.height) * 0.006f;
	}
}
