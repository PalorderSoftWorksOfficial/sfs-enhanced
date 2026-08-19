using System.Text;

namespace SFS;

public class MsgCollector : I_MsgLogger
{
	public StringBuilder msg = new StringBuilder();

	void I_MsgLogger.Log(string msg)
	{
		this.msg.AppendLine(msg);
	}
}
