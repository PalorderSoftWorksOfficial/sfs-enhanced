using System;
using SFS.Input;
using UnityEngine.Events;

namespace SFS.UI;

[Serializable]
public class HoldUnityEvent : UnityEvent<OnInputStayData>
{
}
