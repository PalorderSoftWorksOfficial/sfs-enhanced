using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SFS.Variables;

namespace SFS.Parsers.Constructed;

public static class Compute
{
	private class Add : Operator
	{
		public override float Value => A.Value + B.Value;
	}

	private class Subtract : Operator
	{
		public override float Value => A.Value - B.Value;
	}

	private class Multiply : Operator
	{
		public override float Value => A.Value * B.Value;
	}

	private class Divide : Operator
	{
		public override float Value => A.Value / B.Value;
	}

	private abstract class Operator : I_Node
	{
		public I_Node A;

		public I_Node B;

		public abstract float Value { get; }
	}

	private class Number : I_Node
	{
		public float Value { get; set; }
	}

	private class Variable : I_Node
	{
		public double modifier;

		public VariableList<double>.Variable variable;

		public float Value => (float)variable.Value * (float)modifier;
	}

	public interface I_Node
	{
		float Value { get; }
	}

	public static I_Node Compile(string code, VariablesModule variables, out List<string> usedVariables)
	{
		int i = 0;
		return Compile(ref i, ref code, variables, out usedVariables);
	}

	private static I_Node Compile(ref int i, ref string input, VariablesModule variables, out List<string> usedVariables)
	{
		usedVariables = new List<string>();
		List<int> elements = new List<int>();
		List<I_Node> nodes = new List<I_Node>();
		if (string.IsNullOrWhiteSpace(input))
		{
			return new Number();
		}
		input = input.Replace(" ", "");
		while (i < input.Length)
		{
			if (input[i] == '(')
			{
				i++;
				nodes.Add(Compile(ref i, ref input, variables, out var usedVariables2));
				foreach (string item in usedVariables2)
				{
					if (!usedVariables.Contains(item))
					{
						usedVariables.Add(item);
					}
				}
				elements.Add(nodes.Count - 1);
				continue;
			}
			if (input[i] == ')')
			{
				i++;
				break;
			}
			bool flag = input[i] == '-' && (elements.Count == 0 || elements.Last() < 0);
			if (flag)
			{
				i++;
			}
			string text = "";
			while (i < input.Length && (char.IsLetter(input[i]) || input[i] == '_'))
			{
				text += input[i];
				i++;
			}
			if (text.Length > 0)
			{
				nodes.Add(new Variable
				{
					variable = variables.doubleVariables.GetVariable(text),
					modifier = ((!flag) ? 1 : (-1))
				});
				elements.Add(nodes.Count - 1);
				usedVariables.Add(text);
				continue;
			}
			string text2 = "";
			while (i < input.Length && (elements.Count == 0 || elements.Last() < 0) && (char.IsNumber(input[i]) || input[i] == '.'))
			{
				text2 += input[i];
				i++;
			}
			if (text2.Length > 0)
			{
				float num = float.Parse(text2, CultureInfo.InvariantCulture);
				nodes.Add(new Number
				{
					Value = (flag ? (0f - num) : num)
				});
				elements.Add(nodes.Count - 1);
				continue;
			}
			switch (input[i])
			{
			case '+':
				elements.Add(-1);
				break;
			case '-':
				elements.Add(-2);
				break;
			case '*':
				elements.Add(-3);
				break;
			case '/':
				elements.Add(-4);
				break;
			}
			i++;
		}
		int num2 = 0;
		while (elements.Contains(-3) || elements.Contains(-4))
		{
			if (num2 < 1000)
			{
				num2++;
				for (int j = 0; j < elements.Count; j++)
				{
					if (elements[j] == -3)
					{
						Operator(new Multiply(), j);
						break;
					}
					if (elements[j] == -4)
					{
						Operator(new Divide(), j);
						break;
					}
				}
				continue;
			}
			throw new Exception();
		}
		while (elements.Contains(-1) || elements.Contains(-2))
		{
			if (num2 < 1000)
			{
				num2++;
				for (int k = 0; k < elements.Count; k++)
				{
					if (elements[k] == -1)
					{
						Operator(new Add(), k);
						break;
					}
					if (elements[k] == -2)
					{
						Operator(new Subtract(), k);
						break;
					}
				}
				continue;
			}
			throw new Exception();
		}
		if (elements.Count != 1)
		{
			throw new Exception("Failed to compile: " + input);
		}
		List<I_Node> list = nodes;
		return list[list.Count - 1];
		void Operator(Operator _operator, int elementIndex)
		{
			_operator.A = nodes[elements[elementIndex - 1]];
			_operator.B = nodes[elements[elementIndex + 1]];
			nodes.Add(_operator);
			elements.RemoveRange(elementIndex - 1, 3);
			elements.Insert(elementIndex - 1, nodes.Count - 1);
		}
	}

	public static List<string> GetVariablesUsed(string valueString)
	{
		valueString = valueString.Replace(" ", "");
		List<string> list = new List<string> { "" };
		string text = valueString;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (char.IsLetter(c) || c == "_"[0])
			{
				list[list.Count - 1] += c;
			}
			else if (list[list.Count - 1].Length > 0)
			{
				list.Add("");
			}
		}
		if (list[list.Count - 1] == "")
		{
			list.RemoveAt(list.Count - 1);
		}
		return list;
	}
}
