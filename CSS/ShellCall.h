#pragma once
namespace CSS
{
	class ShellCall
	{
	public:
		// Tell the Shell so File Explorer can display the progress bar in its view
		static void TransferProgressBar(_In_ PCWSTR fullPath,
			_In_ const CF_CONNECTION_KEY ConnectionKey,
			_In_ const CF_TRANSFER_KEY TransferKey,
			UINT64 total,
			UINT64 completed);

		// If the local (client) folder where the cloud file placeholders are created
		// is not under the User folder (i.e. Documents, Photos, etc), then it is required
		// to add the folder to the Search Indexer. This is because the properties for
		// the cloud file state/progress are cached in the indexer, and if the folder isn't
		// indexed, attempts to get the properties on items will not return the expected values.
		static void AddFolderToSearchIndexer(_In_ PCWSTR folder);
	};
}
