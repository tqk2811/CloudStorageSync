#pragma once
namespace CSS
{
	class ConnectSyncRoot
	{
        static CF_CALLBACK_REGISTRATION s_CallbackTable[];
	public:
		static LONGLONG ConnectSyncRootTransferCallbacks(_In_ LPCWSTR LocalFolder);
		static bool DisconnectSyncRootTransferCallbacks(_In_ LONGLONG transferCallbackConnectionKey);

	private:

        static HRESULT TransferData(_In_ CF_CONNECTION_KEY connectionKey, _In_ CF_TRANSFER_KEY transferKey,
            _In_reads_bytes_opt_(length.QuadPart) LPCVOID transferData, _In_ LARGE_INTEGER startingOffset, _In_ LARGE_INTEGER length, _In_ NTSTATUS completionStatus);
        //---------------FETCH DATA---------------
        //Callback to satisfy an I/O request, or a placeholder hydration request.
        static void CALLBACK FETCH_DATA(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);
        //Callback to cancel an ongoing placeholder hydration.
        static void CALLBACK CANCEL_FETCH_DATA(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);
        //-------------END FETCH DATA-------------


        //---------------FETCH_PLACEHOLDERS---------------
        static HRESULT TransferPlaceholders(_In_ CF_CONNECTION_KEY connectionKey,_In_ CF_TRANSFER_KEY transferKey,
            CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS Flags, NTSTATUS CompletionStatus, LARGE_INTEGER PlaceholderTotalCount, 
            CF_PLACEHOLDER_CREATE_INFO* PlaceholderArray, DWORD PlaceholderCount, DWORD EntriesProcessed);
        //Callback to request information about the contents of placeholder files.
        static void CALLBACK FETCH_PLACEHOLDERS(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);
        //Callback to cancel a request for the contents of placeholder files.
        static void CALLBACK CANCEL_FETCH_PLACEHOLDERS(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);
        //-------------END FETCH_PLACEHOLDERS-------------


        
        //Callback to inform the sync provider that a placeholder under one of its sync roots has been successfully opened for read/write/delete access.
        static void CALLBACK NOTIFY_FILE_OPEN_COMPLETION(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);
        //Callback to inform the sync provider that a placeholder under one of its sync roots that has been previously opened for read/write/delete access is now closed.
        static void CALLBACK NOTIFY_FILE_CLOSE_COMPLETION(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);



        static HRESULT AckDehydrate(_In_ CF_CONNECTION_KEY connectionKey, _In_ CF_TRANSFER_KEY transferKey,
            CF_OPERATION_ACK_DEHYDRATE_FLAGS Flags, NTSTATUS CompletionStatus, _Field_size_bytes_(FileIdentityLength) LPCVOID FileIdentity, DWORD FileIdentityLength);
        //Callback to inform the sync provider that a placeholder under one of its sync roots is about to be dehydrated.
        static void CALLBACK NOTIFY_DEHYDRATE(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);
        //Callback to inform the sync provider that a placeholder under one of its sync roots has been successfully dehydrated.
        static void CALLBACK NOTIFY_DEHYDRATE_COMPLETION(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);



        static HRESULT AckDelete(_In_ CF_CONNECTION_KEY connectionKey, _In_ CF_TRANSFER_KEY transferKey, _In_ NTSTATUS completionStatus);
        //Callback to inform the sync provider that a placeholder under one of its sync roots is about to be deleted.
        static void CALLBACK NOTIFY_DELETE(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);
        //Callback to inform the sync provider that a placeholder under one of its sync roots has been successfully deleted.
        static void CALLBACK NOTIFY_DELETE_COMPLETION(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);



        static HRESULT AckRename(_In_ CF_CONNECTION_KEY connectionKey, _In_ CF_TRANSFER_KEY transferKey, _In_ NTSTATUS completionStatus);
        //Callback to inform the sync provider that a placeholder under one of its sync roots is about to be renamed or moved.
        static void CALLBACK NOTIFY_RENAME(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);
        //Callback to inform the sync provider that a placeholder under one of its sync roots has been successfully renamed or moved.
        static void CALLBACK NOTIFY_RENAME_COMPLETION(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);


        static HRESULT AckData(CF_CONNECTION_KEY connectionKey, CF_TRANSFER_KEY transferKey,
            NTSTATUS CompletionStatus, LARGE_INTEGER Offset, LARGE_INTEGER Length);
        //Callback to validate placeholder data.
        static void CALLBACK VALIDATE_DATA(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters);




        static HRESULT RetrieveData(CF_CONNECTION_KEY connectionKey, CF_TRANSFER_KEY transferKey,
            _Field_size_bytes_(Length.QuadPart) LPVOID Buffer, LARGE_INTEGER Offset, LARGE_INTEGER Length, LARGE_INTEGER ReturnedLength);        

        static HRESULT RestartHydration(CF_CONNECTION_KEY connectionKey, CF_TRANSFER_KEY transferKey,
            CF_OPERATION_RESTART_HYDRATION_FLAGS Flags,CONST CF_FS_METADATA* FsMetadata, 
            _Field_size_bytes_(FileIdentityLength) LPCVOID FileIdentity,DWORD FileIdentityLength);
	};
}
