using UnityEngine;

namespace SFS.Variables;

public class VariablesModule : MonoBehaviour
{
	public DoubleVariableList doubleVariables = new DoubleVariableList();

	public BoolVariableList boolVariables = new BoolVariableList();

	public StringVariableList stringVariables = new StringVariableList();
}
