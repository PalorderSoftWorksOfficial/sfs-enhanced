using UnityEngine;

namespace SFS.UI;

public class MsgDrawer : MonoBehaviour, I_MsgLogger
{
	public static MsgDrawer main;

	public TextAdapter text;

	public AnimationClip msgAnimation;

	public AnimationClip msgAnimationBig;

	private float startTime = -10f;

	private AnimationClip animationType;

	private void Awake()
	{
		main = this;
	}

	public void Log(string msg)
	{
		Log(msg, big: false);
	}

	public void Log(string msg, bool big)
	{
		text.Text = msg;
		animationType = ((big && msgAnimationBig != null) ? msgAnimationBig : msgAnimation);
		startTime = Time.unscaledTime;
		base.enabled = true;
		text.gameObject.SetActive(value: true);
		Update();
	}

	private void Start()
	{
		animationType = msgAnimation;
	}

	private void Update()
	{
		float num = Time.unscaledTime - startTime;
		if (num < animationType.length)
		{
			animationType.SampleAnimation(text.gameObject, num);
			return;
		}
		text.gameObject.SetActive(value: false);
		base.enabled = false;
	}
}
