using System;
using SFS.Audio;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class MoveModule : MonoBehaviour, I_InitializePartModule
{
	public Float_Reference time;

	public Float_Reference targetTime;

	public float animationTime;

	public bool unscaledTime;

	[Space]
	public MoveData[] animationElements = new MoveData[0];

	private bool initialized;

	int I_InitializePartModule.Priority => 0;

	void I_InitializePartModule.Initialize()
	{
		Start();
	}

	private void Start()
	{
		if (!initialized)
		{
			time.OnChange += new Action(ApplyAnimation);
			targetTime.OnChange += (Action)delegate
			{
				base.enabled = true;
			};
			initialized = true;
		}
	}

	private void Update()
	{
		float num = (unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
		if (num != 0f)
		{
			float maxDelta = ((animationTime > 0f) ? (num / animationTime) : 10000f);
			time.Value = Mathf.MoveTowards(time.Value, targetTime.Value, maxDelta);
			if (time.Value == targetTime.Value)
			{
				base.enabled = false;
			}
		}
	}

	private void ApplyAnimation()
	{
		MoveData[] array = animationElements;
		foreach (MoveData moveData in array)
		{
			switch (moveData.type)
			{
			case MoveData.Type.RotationZ:
				moveData.transform.localEulerAngles = new Vector3(0f, 0f, moveData.X.Evaluate(time.Value - moveData.offset));
				break;
			case MoveData.Type.Position:
				moveData.transform.localPosition = new Vector3(moveData.X.Evaluate(time.Value - moveData.offset), moveData.Y.Evaluate(time.Value - moveData.offset), 0f);
				break;
			case MoveData.Type.Scale:
				moveData.transform.localScale = new Vector3(moveData.X.Evaluate(time.Value - moveData.offset), moveData.Y.Evaluate(time.Value - moveData.offset), 0f);
				break;
			case MoveData.Type.SpriteColor:
				moveData.spriteRenderer.color = moveData.gradient.Evaluate(time.Value - moveData.offset);
				break;
			case MoveData.Type.ImageColor:
				moveData.image.color = moveData.gradient.Evaluate(time.Value - moveData.offset);
				break;
			case MoveData.Type.Active:
			{
				float num = moveData.X.Evaluate(time.Value - moveData.offset);
				if (moveData.transform.gameObject.activeSelf != num > 0f)
				{
					moveData.transform.gameObject.SetActive(num > 0f);
				}
				AudioSource component = moveData.transform.gameObject.GetComponent<AudioSource>();
				if ((bool)component && Application.isPlaying)
				{
					component.volume = (float)SFS.Audio.AudioSettings.main.soundVolume * num;
				}
				break;
			}
			case MoveData.Type.Inactive:
			{
				bool flag = moveData.X.Evaluate(time.Value - moveData.offset) <= 0f;
				if (moveData.transform.gameObject.activeSelf != flag)
				{
					moveData.transform.gameObject.SetActive(flag);
				}
				break;
			}
			case MoveData.Type.SoundVolume:
				if (Application.isPlaying)
				{
					moveData.audioSource.volume = (float)SFS.Audio.AudioSettings.main.soundVolume * moveData.X.Evaluate(time.Value - moveData.offset);
				}
				break;
			case MoveData.Type.AnchoredPosition:
				(moveData.transform as RectTransform).anchoredPosition = new Vector3(moveData.X.Evaluate(time.Value - moveData.offset), moveData.Y.Evaluate(time.Value - moveData.offset), 0f);
				break;
			case MoveData.Type.RotationXY:
				moveData.transform.localEulerAngles = new Vector3(moveData.X.Evaluate(time.Value - moveData.offset), moveData.Y.Evaluate(time.Value - moveData.offset), 0f);
				break;
			case MoveData.Type.FloatVariable:
				moveData.floatVariable.Value = moveData.X.Evaluate(time.Value - moveData.offset);
				break;
			case MoveData.Type.BoolVariable:
				moveData.boolVariable.Value = moveData.X.Evaluate(time.Value - moveData.offset) > 0f;
				break;
			}
		}
	}

	public void Toggle()
	{
		targetTime.Value = ((targetTime.Value == 0f) ? 1 : 0);
	}

	public void Activate()
	{
		targetTime.Value = 1f;
	}

	public void SetTargetTime(float newTargetTime)
	{
		targetTime.Value = newTargetTime;
	}

	public void SetTime(float newTime)
	{
		time.Value = newTime;
	}
}
