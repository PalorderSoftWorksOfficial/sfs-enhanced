namespace SFS.Career;

public interface I_TechTreeData
{
	bool IsComplete { get; }

	bool GrayOut { get; }

	string Name_ID { get; }

	int Value { get; }
}
