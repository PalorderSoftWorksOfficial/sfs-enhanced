using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SFS.Input;
using SFS.IO;
using SFS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModLoader.IO;

public class Console : Screen_Menu
{
	public static Console main;

	public static List<Func<string, bool>> commands = new List<Func<string, bool>>();

	private const int MaxLines = 150;

	private Queue<string> queue = new Queue<string>();

	private string lastLog = string.Empty;

	public GameObject holder;

	public TextMeshProUGUI text;

	public SFS.UI.Button runButton;

	public TMP_InputField commandInput;

	public RectTransform textLayout;

	private ScrollElement scroller;

	protected override CloseMode OnEscape => CloseMode.Current;

	private void Awake()
	{
		main = this;
		Application.logMessageReceivedThreaded += HandleLog;
		runButton.onClick += new Action(ReadConsoleInput);
		scroller = textLayout.GetComponent<ScrollElement>();
		commands.Add(delegate(string s)
		{
			if (s != "clear")
			{
				return false;
			}
			main.queue.Clear();
			main.WriteText("Cleared console!");
			return true;
		});
		commands.Add(delegate(string s)
		{
			Match match = Regex.Match(s, "savelog ([A-z.]+)");
			if (!match.Success)
			{
				return false;
			}
			FilePath filePath = FileLocations.BaseFolder.Extend("Logs").CreateFolder().ExtendToFile(match.Groups[1].Value);
			filePath.WriteText(string.Join(Environment.NewLine, main.queue));
			main.WriteText($"Successfully saved log to {filePath}");
			return true;
		});
		commandInput.onSubmit.AddListener(delegate
		{
			main.ReadConsoleInput();
		});
	}

	private void HandleLog(string message, string stackTrace, LogType type)
	{
		if (!(message == lastLog))
		{
			if (queue.Count >= 150)
			{
				queue.Dequeue();
			}
			string text = DateTime.UtcNow.ToString("HH:mm:ss");
			string item = type switch
			{
				LogType.Error => "[" + text + "] ERROR: " + message, 
				LogType.Exception => "[" + text + "] EXCEPTION: " + message + "\n" + stackTrace, 
				LogType.Warning => "[" + text + "] WARNING: " + message + "\n" + stackTrace, 
				_ => "[" + text + "] LOG: " + message, 
			};
			queue.Enqueue(item);
			lastLog = message;
		}
	}

	public override void OnOpen()
	{
		holder.SetActive(value: true);
		UpdateText();
	}

	public override void OnClose()
	{
		holder.SetActive(value: false);
	}

	private void Start()
	{
		Screen_Base.AddOnKeyDown(KeybindingsPC.keys.Toggle_Console, delegate
		{
			if (holder.activeSelf)
			{
				Close();
			}
			else
			{
				Open();
			}
		});
	}

	private void UpdateText()
	{
		text.gameObject.SetActive(value: false);
		text.text = string.Join(Environment.NewLine, queue);
		text.gameObject.SetActive(value: true);
		LayoutRebuilder.ForceRebuildLayoutImmediate(textLayout);
		scroller.ResetPosition();
	}

	private void ReadConsoleInput()
	{
		if (!string.IsNullOrWhiteSpace(commandInput.text))
		{
			queue.Enqueue($"[{DateTime.UtcNow:HH:mm:ss}] > {commandInput.text}");
			if (!commands.Any((Func<string, bool> command) => command(commandInput.text)))
			{
				WriteText("Command \"" + commandInput.text + "\" not found!");
			}
			commandInput.text = string.Empty;
		}
	}

	public void Copy()
	{
		GUIUtility.systemCopyBuffer = string.Join(Environment.NewLine, queue);
		WriteText("Copied log to clipboard!");
	}

	public void WriteText(string input)
	{
		queue.Enqueue(input);
		UpdateText();
	}
}
