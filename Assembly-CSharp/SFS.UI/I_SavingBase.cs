using SFS.IO;

namespace SFS.UI;

public interface I_SavingBase
{
	string Title { get; }

	string LoadButtonText { get; }

	ImportAvailability GetImportAvailability();

	bool CanSave(I_MsgLogger logger);

	void Save(string name);

	void Load(string name);

	void Delete(string name);

	void Import(string name);

	void Rename(string nameOld, string nameNew);

	OrderedPathList GetOrderer();
}
