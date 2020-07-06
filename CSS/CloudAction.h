#pragma once
namespace CSS
{
	typedef HRESULT(*TransferData_CB)(CF_CONNECTION_KEY, CF_TRANSFER_KEY, LPCVOID, LARGE_INTEGER, LARGE_INTEGER, NTSTATUS);
	
	ref class CloudAction
	{
	public:
		static void Download(SyncRootViewModel^ srvm, CloudItem^ ci, 
			CONST CF_CALLBACK_INFO* callbackInfo, CONST CF_CALLBACK_PARAMETERS* callbackParameters, TransferData_CB TransferData);
	private:
	};
}
