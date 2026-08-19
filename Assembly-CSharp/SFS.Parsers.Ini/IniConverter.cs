using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SFS.Parsers.Ini;

public class IniConverter
{
	private class StringReader
	{
		public readonly string input;

		public int pos;

		public StringReader(string input)
		{
			this.input = input;
		}

		public bool IsAtEnd()
		{
			return pos >= input.Length;
		}

		public char Read()
		{
			return input[pos++];
		}

		public string Read(int length)
		{
			length = Math.Min(input.Length - pos, length);
			string result = input.Substring(pos, length);
			pos += length;
			return result;
		}

		public char Peek()
		{
			return input[pos];
		}

		public string Peek(int length)
		{
			return input.Substring(pos, Math.Min(input.Length - pos, length));
		}

		public StringReader Split()
		{
			return new StringReader(input)
			{
				pos = pos
			};
		}

		public void Merge(StringReader reader)
		{
			pos = reader.pos;
		}
	}

	public IniDataEnv data = new IniDataEnv();

	public IniConverter()
	{
		data = new IniDataEnv();
	}

	public IniConverter(string iniText)
	{
		LoadIni(iniText);
	}

	public IniDataSection GetSection(string section)
	{
		if (!data.sections.ContainsKey(section))
		{
			data.sections[section] = new IniDataSection(section);
		}
		return data.sections[section];
	}

	public void LoadIni(string iniText)
	{
		string[] array = iniText.Split(new string[2] { "\n", "\r\n" }, StringSplitOptions.None);
		IniDataSection iniDataSection = data.Global;
		int num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		string[] array2 = array;
		foreach (string obj in array2)
		{
			string input = obj.Trim();
			if (string.IsNullOrWhiteSpace(obj))
			{
				num++;
				continue;
			}
			StringReader reader = new StringReader(input);
			string sectionName;
			if (ReadComment(reader, out var comment))
			{
				stringBuilder.AppendLine(comment);
			}
			else if (ReadSection(reader, out sectionName))
			{
				iniDataSection = data.GetSection(sectionName);
				iniDataSection.whitespacesBefore = num;
				num = 0;
				if (stringBuilder.Length > 0)
				{
					iniDataSection.comment = stringBuilder.ToString();
				}
				stringBuilder.Clear();
			}
			else
			{
				if (!ReadKey(reader, out var keyName))
				{
					continue;
				}
				ReadValue(reader, out var value, out var aftComment);
				if (iniDataSection.data.ContainsKey(keyName))
				{
					IniDataEnv.Value value2 = iniDataSection[keyName];
					value2.value = value2.value + "\n" + value;
					continue;
				}
				IniDataEnv.Value value3 = new IniDataEnv.Value(value);
				value3.aftLineComment = aftComment;
				value3.whitespacesBefore = num;
				num = 0;
				if (stringBuilder.Length > 0)
				{
					value3.preLineComment = stringBuilder.ToString();
				}
				stringBuilder.Clear();
				iniDataSection[keyName] = value3;
			}
		}
		static bool ReadComment(StringReader stringReader, out string reference)
		{
			string[] obj2 = new string[3] { ";", "//", "#" };
			StringBuilder stringBuilder2 = new StringBuilder();
			string[] array3 = obj2;
			foreach (string text in array3)
			{
				if (stringReader.Peek(text.Length) == text)
				{
					stringReader.Read(text.Length);
					while (!stringReader.IsAtEnd())
					{
						stringBuilder2.Append(stringReader.Read());
					}
					reference = stringBuilder2.ToString();
					return true;
				}
			}
			reference = null;
			return false;
		}
		static bool ReadKey(StringReader stringReader2, out string reference)
		{
			StringReader stringReader = stringReader2.Split();
			StringBuilder stringBuilder2 = new StringBuilder();
			while (!stringReader.IsAtEnd())
			{
				if (stringReader.Peek() == '=')
				{
					stringReader.Read();
					stringReader2.Merge(stringReader);
					reference = stringBuilder2.ToString();
					return true;
				}
				stringBuilder2.Append(stringReader.Read());
			}
			reference = null;
			return false;
		}
		static bool ReadSection(StringReader stringReader, out string reference)
		{
			StringBuilder stringBuilder2 = new StringBuilder();
			if (stringReader.Peek() == '[')
			{
				stringReader.Read();
				while (stringReader.Peek() != ']')
				{
					stringBuilder2.Append(stringReader.Read());
				}
				reference = stringBuilder2.ToString();
				return true;
			}
			reference = null;
			return false;
		}
		static bool ReadValue(StringReader stringReader, out string reference2, out string reference)
		{
			reference = null;
			StringBuilder stringBuilder2 = new StringBuilder();
			while (!stringReader.IsAtEnd() && !ReadComment(stringReader, out reference))
			{
				stringBuilder2.Append(stringReader.Read());
			}
			reference2 = stringBuilder2.ToString();
			return reference2.Length > 0;
		}
	}

	public string Serialize()
	{
		StringBuilder iniTextBuilder = new StringBuilder();
		int whitelines = 0;
		data.sections.ForEach(delegate(KeyValuePair<string, IniDataSection> section)
		{
			AppendSection(section.Value);
		});
		return iniTextBuilder.ToString();
		void Append(string txt, bool clearWhitelines)
		{
			whitelines = ((!clearWhitelines) ? whitelines : 0);
			iniTextBuilder.Append(txt);
		}
		void AppendAftComment(string aftComment)
		{
			if (aftComment != null)
			{
				AppendComment(aftComment, canUseWhiteline: false);
			}
			else
			{
				AppendLine("", clearWhitelines: true);
			}
		}
		void AppendComment(string comment, bool canUseWhiteline)
		{
			if (comment != null)
			{
				if ((whitelines == 0) & canUseWhiteline)
				{
					EnsureWhitelines(1);
				}
				string[] array = comment.Split(new string[2] { "\n", "\r\n" }, StringSplitOptions.None);
				foreach (string text in array)
				{
					AppendLine("# " + text, clearWhitelines: false);
				}
			}
		}
		void AppendDataLine(string keyName, IniDataEnv.Value value, bool canUseInitialWhiteline)
		{
			if ((value.whitespacesBefore != 0) & canUseInitialWhiteline)
			{
				EnsureWhitelines(value.whitespacesBefore);
			}
			AppendComment(value.preLineComment, canUseInitialWhiteline);
			if (value.value.Contains("\n"))
			{
				EnsureWhitelines(1);
				string[] array = value.value.Split('\n', StringSplitOptions.None);
				for (int i = 0; i < array.Length; i++)
				{
					AppendDataValue(array[i] + ((i < array.Length - 1) ? "\n" : ""));
				}
				AppendAftComment(value.aftLineComment);
				AppendWhiteLine();
			}
			else
			{
				AppendDataValue(value.value);
				AppendAftComment(value.aftLineComment);
			}
		}
		void AppendDataValue(string val)
		{
			Append("    " + P_1.keyName + "=" + val, clearWhitelines: true);
		}
		void AppendLine(string txt, bool clearWhitelines)
		{
			Append(txt + "\n", clearWhitelines);
		}
		void AppendSection(IniDataSection section)
		{
			AppendComment(section.comment, canUseWhiteline: true);
			if (iniTextBuilder.Length > 0)
			{
				EnsureWhitelines(1);
			}
			AppendLine("[" + section.name + "]", clearWhitelines: true);
			KeyValuePair<string, IniDataEnv.Value>[] array = section.data.ToArray();
			for (int i = 0; i < section.data.Count; i++)
			{
				KeyValuePair<string, IniDataEnv.Value> keyValuePair = array[i];
				AppendDataLine(keyValuePair.Key, keyValuePair.Value, i > 0);
			}
		}
		void AppendWhiteLine()
		{
			whitelines++;
			iniTextBuilder.AppendLine();
		}
		void EnsureWhitelines(int amount)
		{
			int num = Math.Max(0, amount - whitelines);
			for (int i = 0; i < num; i++)
			{
				AppendWhiteLine();
			}
		}
	}

	public string[] GetSectionNames()
	{
		return new List<IniDataSection>(data.sections.Values).ConvertAll((IniDataSection data) => data.name).ToArray();
	}
}
