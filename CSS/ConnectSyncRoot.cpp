#include "pch.h"
#include "ConnectSyncRoot.h"
namespace CSS
{
#define FIELD_SIZE( type, field ) ( sizeof( ( (type*)0 )->field ) )
#define CF_SIZE_OF_OP_PARAM( field )( FIELD_OFFSET( CF_OPERATION_PARAMETERS, field ) + FIELD_SIZE( CF_OPERATION_PARAMETERS, field ) )

    CF_CALLBACK_REGISTRATION ConnectSyncRoot::s_CallbackTable[] ={
        { CF_CALLBACK_TYPE_FETCH_DATA,                      ConnectSyncRoot::FETCH_DATA },
        //{ CF_CALLBACK_TYPE_CANCEL_FETCH_DATA,               ConnectSyncRoot::CANCEL_FETCH_DATA },

        //{ CF_CALLBACK_TYPE_FETCH_PLACEHOLDERS,              ConnectSyncRoot::FETCH_PLACEHOLDERS },//see StorageProviderPopulationPolicy::Full
        //{ CF_CALLBACK_TYPE_CANCEL_FETCH_PLACEHOLDERS,       ConnectSyncRoot::CANCEL_FETCH_PLACEHOLDERS },

        //{ CF_CALLBACK_TYPE_NOTIFY_FILE_OPEN_COMPLETION,     ConnectSyncRoot::NOTIFY_FILE_OPEN_COMPLETION },
        //{ CF_CALLBACK_TYPE_NOTIFY_FILE_CLOSE_COMPLETION,    ConnectSyncRoot::NOTIFY_FILE_CLOSE_COMPLETION },

        //{ CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE,                ConnectSyncRoot::NOTIFY_DEHYDRATE },
        //{ CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE_COMPLETION,     ConnectSyncRoot::NOTIFY_DEHYDRATE_COMPLETION },

        { CF_CALLBACK_TYPE_NOTIFY_DELETE,                   ConnectSyncRoot::NOTIFY_DELETE },
        { CF_CALLBACK_TYPE_NOTIFY_DELETE_COMPLETION,        ConnectSyncRoot::NOTIFY_DELETE_COMPLETION },

        { CF_CALLBACK_TYPE_NOTIFY_RENAME,                   ConnectSyncRoot::NOTIFY_RENAME },//when move file or rename
        { CF_CALLBACK_TYPE_NOTIFY_RENAME_COMPLETION,        ConnectSyncRoot::NOTIFY_RENAME_COMPLETION },

        //{ CF_CALLBACK_TYPE_VALIDATE_DATA,                   ConnectSyncRoot::VALIDATE_DATA },//see StorageProviderHydrationPolicyModifier::ValidationRequired
        CF_CALLBACK_REGISTRATION_END
    };
    LONGLONG ConnectSyncRoot::ConnectSyncRootTransferCallbacks(_In_ LPCWSTR LocalFolder)
    {
        CF_CONNECTION_KEY ck{ 0 };
        HRESULT hr = CfConnectSyncRoot(
            LocalFolder,
            s_CallbackTable,
            NULL,
            CF_CONNECT_FLAG_REQUIRE_PROCESS_INFO | CF_CONNECT_FLAG_REQUIRE_FULL_FILE_PATH | CF_CONNECT_FLAG_BLOCK_SELF_IMPLICIT_HYDRATION,
            &ck);
        CheckHr(hr, L"ConnectSyncRoot::SRConnectSyncRoot SRConnectSyncRoot", LocalFolder);
        return ck.Internal;
    }
    bool ConnectSyncRoot::DisconnectSyncRootTransferCallbacks(_In_ LONGLONG transferCallbackConnectionKey)
    {
        CF_CONNECTION_KEY ck;
        ck.Internal = transferCallbackConnectionKey;
        HRESULT hr = CfDisconnectSyncRoot(ck);
        if (CheckHr(hr, L"ConnectSyncRoot::SRConnectSyncRoot SRDisconnectSyncRoot")) return true;
        else return false;
    }

    bool IsThisProcess(_In_ CONST CF_PROCESS_INFO* ProcessInfo)
    {
        if (ProcessInfo)
        {
            static DWORD currProcessId = GetCurrentProcessId();
            if (ProcessInfo->ProcessId == currProcessId) return true;
        }
        return false;
    }

    gcroot<Regex^> rg_system = gcnew Regex(L"\\\\Device\\\\HarddiskVolume\\d\\\\Windows\\\\");
    bool IsSystem(_In_ CONST CF_PROCESS_INFO* ProcessInfo)
    {
        return rg_system->Match(gcnew String(ProcessInfo->ImagePath))->Success;
    }


    HRESULT ConnectSyncRoot::TransferData(_In_ CF_CONNECTION_KEY connectionKey,_In_ CF_TRANSFER_KEY transferKey,
        _In_reads_bytes_opt_(length.QuadPart) LPCVOID transferData,_In_ LARGE_INTEGER startingOffset,_In_ LARGE_INTEGER length,_In_ NTSTATUS completionStatus)
    {
        CF_OPERATION_INFO opInfo = { 0 };
        CF_OPERATION_PARAMETERS opParams = { 0 };
        opInfo.StructSize = sizeof(opInfo);
        opInfo.Type = CF_OPERATION_TYPE::CF_OPERATION_TYPE_TRANSFER_DATA;
        opInfo.ConnectionKey = connectionKey;
        opInfo.TransferKey = transferKey;
        opParams.ParamSize = CF_SIZE_OF_OP_PARAM(CF_OPERATION_PARAMETERS::TransferData);
        opParams.TransferData.CompletionStatus = completionStatus;
        opParams.TransferData.Buffer = transferData;
        opParams.TransferData.Offset = startingOffset;
        opParams.TransferData.Length = length;
        return CfExecute(&opInfo, &opParams);
    }
    //when download file
    void CALLBACK ConnectSyncRoot::FETCH_DATA(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        LPCWSTR reason = L"";
        if (CssCs::Settings::Setting->HasInternet)
        {
            SyncRootViewModel^ srvm = SyncRootViewModel::FindWithConnectionKey(callbackInfo->ConnectionKey.Internal);
            if (srvm)
            {
                CloudItem^ ci = CloudItem::Select(gcnew String(GetFileIdentity(callbackInfo->FileIdentity)), srvm);
                if (ci)
                {
                    CloudAction::Download(srvm, ci, callbackInfo, callbackParameters, TransferData);
                    LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::FETCH_DATA Accept request")
                        .append(L", path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
                    return;
                }
                else reason = L"ci not found";
            }
            else reason = L"srvm not found";
        }
        else reason = L"No Internet";

        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::FETCH_DATA Cancel, reason:").append(reason)
            .append(L", path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
        //cancel
        TransferData(
            callbackInfo->ConnectionKey,
            callbackInfo->TransferKey,
            NULL,
            callbackParameters->FetchData.RequiredFileOffset,
            callbackParameters->FetchData.RequiredLength,
            STATUS_UNSUCCESSFUL);
    }
    void CALLBACK ConnectSyncRoot::CANCEL_FETCH_DATA(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::CANCEL_FETCH_DATA ,path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
    }



    HRESULT ConnectSyncRoot::TransferPlaceholders(_In_ CF_CONNECTION_KEY connectionKey, _In_ CF_TRANSFER_KEY transferKey,
        CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS Flags, NTSTATUS CompletionStatus, LARGE_INTEGER PlaceholderTotalCount,
        CF_PLACEHOLDER_CREATE_INFO* PlaceholderArray, DWORD PlaceholderCount, DWORD EntriesProcessed)
    {
        CF_OPERATION_INFO opInfo = { 0 };
        CF_OPERATION_PARAMETERS opParams = { 0 };
        opInfo.StructSize = sizeof(opInfo);
        opInfo.Type = CF_OPERATION_TYPE::CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS;
        opInfo.ConnectionKey = connectionKey;
        opInfo.TransferKey = transferKey;
        opParams.ParamSize = CF_SIZE_OF_OP_PARAM(CF_OPERATION_PARAMETERS::TransferPlaceholders);
        opParams.TransferPlaceholders.Flags = Flags;
        opParams.TransferPlaceholders.CompletionStatus = CompletionStatus;
        opParams.TransferPlaceholders.PlaceholderTotalCount = PlaceholderTotalCount;
        opParams.TransferPlaceholders.PlaceholderArray = PlaceholderArray;
        opParams.TransferPlaceholders.PlaceholderCount = PlaceholderCount;
        opParams.TransferPlaceholders.EntriesProcessed = EntriesProcessed;
        return CfExecute(&opInfo, &opParams);
    }
    void CALLBACK ConnectSyncRoot::FETCH_PLACEHOLDERS(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->FetchPlaceholders.Flags;//none
        //callbackParameters->FetchPlaceholders.Pattern;
        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::FETCH_PLACEHOLDERS ,path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
    }
    void CALLBACK ConnectSyncRoot::CANCEL_FETCH_PLACEHOLDERS(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->;
        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::CANCEL_FETCH_PLACEHOLDERS ,path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
    }

    //public value class Find_NOTIFYOpenClose
    //{
    //    LONGLONG localid;
    //public:
    //    Find_NOTIFYOpenClose(LONGLONG id)
    //    {
    //        localid = id;
    //    }
    //    bool FindLocalId(LONGLONG id)
    //    {
    //        return localid == id;
    //    }
    //};
    //gcroot<List<LONGLONG>^> LocalIdsOpen = gcnew List<LONGLONG>();
    void CALLBACK ConnectSyncRoot::NOTIFY_FILE_OPEN_COMPLETION(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->OpenCompletion.Flags;
        //if (IsThisProcess(callbackInfo->ProcessInfo) || IsSystem(callbackInfo->ProcessInfo)) return;
        //String^ fullpath = gcnew String(callbackInfo->VolumeDosName) + gcnew String(callbackInfo->NormalizedPath);        
        //SyncRootViewModel^ srvm = FindSR(callbackInfo->ConnectionKey);
        //LocalItem^ li = FindLi(srvm, fullpath);
        //if(li) LocalIdsOpen->Add(li->LocalId);
        //PinStr(fullpath);
        //LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::NOTIFY_FILE_OPEN_COMPLETION, Path:").append(pin_fullpath)
        //    .append(L", CommandLine:").append(callbackInfo->ProcessInfo->CommandLine), 1);
    }
    void CALLBACK ConnectSyncRoot::NOTIFY_FILE_CLOSE_COMPLETION(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->CloseCompletion.Flags;
        //if (IsThisProcess(callbackInfo->ProcessInfo) || IsSystem(callbackInfo->ProcessInfo)) return;
        //String^ fullpath = gcnew String(callbackInfo->VolumeDosName) + gcnew String(callbackInfo->NormalizedPath);
        //PinStr(fullpath);
        //SyncRootViewModel^ srvm = FindSR(callbackInfo->ConnectionKey);
        //LocalItem^ li = FindLi(srvm, fullpath);
        //if (li)
        //{
        //    LocalIdsOpen->Remove(li->LocalId);
        //    if (!LocalIdsOpen->Exists(gcnew Predicate<LONGLONG>(gcnew Find_NOTIFYOpenClose(li->LocalId), &Find_NOTIFYOpenClose::FindLocalId)))
        //    {
        //        HANDLE hfile = CreateFile(pin_fullpath, FILE_READ_ATTRIBUTES, 0, nullptr, OPEN_EXISTING, 0, nullptr);
        //        if (hfile != INVALID_HANDLE_VALUE)
        //        {
        //            LARGE_INTEGER start{ 0 };
        //            LARGE_INTEGER length;
        //            length.QuadPart = MAXINT64;
        //            BYTE buffer[1024];
        //            DWORD returnlength{ 0 };
        //            HRESULT hr = CfGetPlaceholderRangeInfo(hfile, CF_PLACEHOLDER_RANGE_INFO_MODIFIED, start, length, &buffer, 1024, &returnlength);
        //            CloseHandle(hfile);
        //            if (SUCCEEDED(hr) && returnlength > 0)
        //            {
        //                srvm->UploadQueues->Add(gcnew UploadQueue(li));
        //            }
        //        }                
        //    }
        //}
        //LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::NOTIFY_FILE_CLOSE_COMPLETION, Path:").append(pin_fullpath)
        //    .append(L", CommandLine:").append(callbackInfo->ProcessInfo->CommandLine), 1);
    }



    HRESULT ConnectSyncRoot::AckDehydrate(_In_ CF_CONNECTION_KEY connectionKey, _In_ CF_TRANSFER_KEY transferKey,
        CF_OPERATION_ACK_DEHYDRATE_FLAGS Flags, NTSTATUS CompletionStatus, _Field_size_bytes_(FileIdentityLength) LPCVOID FileIdentity, DWORD FileIdentityLength)
    {
        CF_OPERATION_INFO opInfo = { 0 };
        CF_OPERATION_PARAMETERS opParams = { 0 };
        opInfo.StructSize = sizeof(opInfo);
        opInfo.Type = CF_OPERATION_TYPE::CF_OPERATION_TYPE_ACK_DEHYDRATE;
        opInfo.ConnectionKey = connectionKey;
        opInfo.TransferKey = transferKey;
        opParams.ParamSize = CF_SIZE_OF_OP_PARAM(CF_OPERATION_PARAMETERS::AckDehydrate);
        opParams.AckDehydrate.CompletionStatus = CompletionStatus;
        opParams.AckDehydrate.Flags = Flags;
        opParams.AckDehydrate.FileIdentity = FileIdentity;
        opParams.AckDehydrate.FileIdentityLength = FileIdentityLength;
        return CfExecute(&opInfo, &opParams);
    }
    void CALLBACK ConnectSyncRoot::NOTIFY_DEHYDRATE(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->Dehydrate.Flags;
        //callbackParameters->Dehydrate.Reason;
        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::NOTIFY_DEHYDRATE ,path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
    }
    void CALLBACK ConnectSyncRoot::NOTIFY_DEHYDRATE_COMPLETION(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->DehydrateCompletion.Flags;
        //callbackParameters->DehydrateCompletion.Reason;
        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::NOTIFY_DEHYDRATE_COMPLETION ,path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
    }



    HRESULT ConnectSyncRoot::AckDelete(_In_ CF_CONNECTION_KEY connectionKey,_In_ CF_TRANSFER_KEY transferKey,
        CF_OPERATION_ACK_DELETE_FLAGS Flags, _In_ NTSTATUS completionStatus)
    {
        CF_OPERATION_INFO opInfo = { 0 };
        CF_OPERATION_PARAMETERS opParams = { 0 };
        opInfo.StructSize = sizeof(opInfo);
        opInfo.Type = CF_OPERATION_TYPE::CF_OPERATION_TYPE_ACK_DELETE;
        opInfo.ConnectionKey = connectionKey;
        opInfo.TransferKey = transferKey;
        opParams.ParamSize = CF_SIZE_OF_OP_PARAM(CF_OPERATION_PARAMETERS::AckDelete);
        opParams.AckDelete.CompletionStatus = completionStatus;
        opParams.AckDelete.Flags = Flags;
        return CfExecute(&opInfo, &opParams);
    }
    //trigger 2 time when delete a placeholder (not hydrate, and not shift delete)
    //when delete or move file different disk (copy and delete)
    void CALLBACK ConnectSyncRoot::NOTIFY_DELETE(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->Delete.Flags;
        bool SUCCESS{ false };
        LPCWSTR reason = L"No Error";
        if (CssCs::Settings::Setting->HasInternet)
        {
            if (IsThisProcess(callbackInfo->ProcessInfo)) SUCCESS = true;
            else
            {
                String^ fullpath = gcnew String(callbackInfo->VolumeDosName) + gcnew String(callbackInfo->NormalizedPath);
                PinStr(fullpath);
                if (PathExists(pin_fullpath))
                {
                    SyncRootViewModel^ srvm = SyncRootViewModel::FindWithConnectionKey(callbackInfo->ConnectionKey.Internal);
                    if (srvm)
                    {
                        CloudItem^ ci = CloudItem::Select(gcnew String(GetFileIdentity(callbackInfo->FileIdentity)), srvm);
                        LocalItem^ li = LocalItem::FindFromPath(srvm, fullpath, 0);
                        if (li && ci)
                        {
                            LocalItem^ li_parent = LocalItem::Find(li->LocalParentId);
                            CloudItem^ ci_parent = li_parent ? CloudItem::Select(li_parent->CloudId, srvm) : nullptr;
                            if (li_parent && ci_parent)
                            {
                                if (ci->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::OwnedByMe) &&
                                    ci->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::CanTrash))
                                {
                                    Task^ t = srvm->CEVM->Cloud->TrashItem(ci->Id);
                                    CssCs::Extensions::WriteLogIfError(t, gcnew String(L"ConnectSyncRoot::NOTIFY_DELETE Cloud->TrashItem"));
                                    SUCCESS = true;
                                }
                                else if (li_parent && !ci->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::OwnedByMe) &&
                                    ci_parent->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::CanRemoveChildren))
                                {
                                    UpdateCloudItem^ uci = gcnew UpdateCloudItem();
                                    uci->Id = ci->Id;
                                    uci->ParentIdsRemove->Add(ci_parent->Id);

                                    Task^ t = srvm->CEVM->Cloud->UpdateMetadata(uci);
                                    CssCs::Extensions::WriteLogIfError(t, gcnew String(L"ConnectSyncRoot::NOTIFY_DELETE Cloud->UpdateMetadata(remove parent)"));
                                    SUCCESS = true;
                                }
                                else reason = L"user not has permision on cloud";
                                //false
                            }
                            else
                            {
                                SUCCESS = true;
                                reason = L"li_parent/ci_parent not found";
                            }
                        }
                        else
                        {
                            SUCCESS = true;//ignore when li not found (because trigger 2 times)
                            reason = L"ci/li not found";
                        }
                    }
                    else reason = L"srvm not found";
                }
                else
                {
                    SUCCESS = true;
                    reason = L"file not found (trigger 2 times by delete placeholder(not hydrate))";
                }
            }
        }
        else reason = L"No Internet";
        AckDelete(callbackInfo->ConnectionKey, callbackInfo->TransferKey, CF_OPERATION_ACK_DELETE_FLAG_NONE, SUCCESS ? STATUS_SUCCESS : STATUS_UNSUCCESSFUL);
        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::NOTIFY_DELETE:").append(SUCCESS ? L"STATUS_SUCCESS" : L"STATUS_UNSUCCESSFUL")
            .append(L", error:").append(reason)
            .append(L", path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
    }
    void CALLBACK ConnectSyncRoot::NOTIFY_DELETE_COMPLETION(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->DeleteCompletion.Flags;
        //if (IsThisProcess(callbackInfo->ProcessInfo)) 
        //   return;

        bool success{ false };
        LPCWSTR reason = L"No Error";
        String^ fullpath = gcnew String(callbackInfo->VolumeDosName) + gcnew String(callbackInfo->NormalizedPath);
        PinStr(fullpath);
        SyncRootViewModel^ srvm = SyncRootViewModel::FindWithConnectionKey(callbackInfo->ConnectionKey.Internal);
        //CloudItem^ ci = FindCi(srvm, callbackInfo->FileIdentity);
        if (srvm)
        {
            LocalItem^ li = LocalItem::FindFromPath(srvm, fullpath, 0);
            if (li)
            {
                if (PathExists(pin_fullpath)) LocalAction::DeleteLocal(srvm, li);
                li->Delete(true);
                success = true;
            }
            else reason = L"li not found";
        }
        else reason = L"srvm not found";
        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::NOTIFY_DELETE_COMPLETION, error:").append(reason)
            .append(L", path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath).append(L", LocalItemDelete:").append(success ? L"Success" : L"Failed"), 1);
    }



    HRESULT ConnectSyncRoot::AckRename(_In_ CF_CONNECTION_KEY connectionKey, _In_ CF_TRANSFER_KEY transferKey,
        CF_OPERATION_ACK_RENAME_FLAGS Flags, _In_ NTSTATUS completionStatus)
    {
        CF_OPERATION_INFO opInfo = { 0 };
        CF_OPERATION_PARAMETERS opParams = { 0 };
        opInfo.StructSize = sizeof(opInfo);
        opInfo.Type = CF_OPERATION_TYPE::CF_OPERATION_TYPE_ACK_RENAME;
        opInfo.ConnectionKey = connectionKey;
        opInfo.TransferKey = transferKey;
        opParams.ParamSize = CF_SIZE_OF_OP_PARAM(CF_OPERATION_PARAMETERS::AckRename);
        opParams.AckRename.CompletionStatus = completionStatus;
        opParams.AckRename.Flags = Flags;
        return CfExecute(&opInfo, &opParams);
    }
    //when move file in current disk, rename
    void CALLBACK ConnectSyncRoot::NOTIFY_RENAME(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->Rename.Flags;
        //callbackParameters->Rename.TargetPath;
        bool SUCCESS{ false };
        LPCWSTR reason = L"";
        if (CssCs::Settings::Setting->HasInternet)
        {
            if (IsThisProcess(callbackInfo->ProcessInfo)) SUCCESS = true;
            else
            {
                //(fullpath_new->Length > srvm->LocalPath->Length) && fullpath_new->Substring(0, srvm->LocalPath->Length)->Equals(srvm->LocalPath,StringComparison::OrdinalIgnoreCase);
                bool newpathIsInSyncroot = callbackParameters->Rename.Flags & CF_CALLBACK_RENAME_FLAG_TARGET_IN_SCOPE;
                String^ fullpath = gcnew String(callbackInfo->VolumeDosName) + gcnew String(callbackInfo->NormalizedPath);
                String^ fullpath_new = gcnew String(callbackInfo->VolumeDosName) + gcnew String(callbackParameters->Rename.TargetPath);
                SyncRootViewModel^ srvm = SyncRootViewModel::FindWithConnectionKey(callbackInfo->ConnectionKey.Internal);
                if (srvm)
                {
                    CloudItem^ ci = CloudItem::Select(gcnew String(GetFileIdentity(callbackInfo->FileIdentity)), srvm);
                    LocalItem^ li = LocalItem::FindFromPath(srvm, fullpath, 0);
                    if (ci && li && !li->Flag.HasFlag(LocalItemFlag::LockWaitUpdateFromCloudWatch))
                    {
                        LocalItem^ li_parent = LocalItem::Find(li->LocalParentId);
                        CloudItem^ ci_parent = li_parent ? CloudItem::Select(li_parent->CloudId, srvm->CEVM->EmailSqlId) : nullptr;
                        if (ci_parent)
                        {
                            if (newpathIsInSyncroot)
                            {
                                LocalItem^ li_parentnew = LocalItem::FindFromPath(srvm, fullpath_new, 1);
                                CloudItem^ ci_parentnew = li_parentnew ? CloudItem::Select(li_parentnew->CloudId, srvm->CEVM->EmailSqlId) : nullptr;
                                if (ci_parentnew)
                                {
                                    List<String^>^ ParentsIdAdd{ nullptr };
                                    List<String^>^ ParentsIdRemove{ nullptr };
                                    bool isChangeParent{ false };
                                    bool isrename{ false };
                                    if (!li_parentnew->Equals(li_parent))//change parent
                                    {
                                        ParentsIdAdd = gcnew List<String^>();
                                        ParentsIdAdd->Add(li_parentnew->CloudId);
                                        ParentsIdRemove = gcnew List<String^>();
                                        ParentsIdRemove->Add(li_parent->CloudId);
                                        isChangeParent = true;
                                    }
                                    String^ newname = fullpath_new->Substring(li_parentnew->GetFullPath()->Length + 1);
                                    isrename = !newname->Equals(li->Name, StringComparison::OrdinalIgnoreCase);

                                    if ((isChangeParent && (ci_parent->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::CanRemoveChildren) &&
                                        ci_parentnew->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::CanAddChildren)))
                                        || (isrename && ci->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::CanRename)))
                                    {
                                        UpdateCloudItem^ uci = gcnew UpdateCloudItem();
                                        uci->Id = ci->Id;
                                        uci->NewName = isrename ? newname : nullptr;
                                        uci->ParentIdsAdd->AddRange(ParentsIdAdd);
                                        uci->ParentIdsRemove->AddRange(ParentsIdRemove);

                                        Task^ t = srvm->CEVM->Cloud->UpdateMetadata(uci);
                                        CssCs::Extensions::WriteLogIfError(t,
                                            gcnew String(L"ConnectSyncRoot::NOTIFY_RENAME Cloud->UpdateMetadata(CanAddChildren,CanRemoveChildren,CanRename)"));

                                        SUCCESS = true;
                                        if (isrename) li->Name = newname;
                                        if (isChangeParent) li->LocalParentId = li_parentnew->LocalId;
                                        li->AddFlagWithLock(LocalItemFlag::LockWaitUpdateFromCloudWatch);
                                        li->Update();
                                    }
                                }
                            }
                            else if (ci->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::OwnedByMe) &&
                                ci->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::CanTrash))//move out syncroot FETCH_DATA -> and move, cloud delete
                            {
                                Task^ t = srvm->CEVM->Cloud->TrashItem(ci->Id);
                                CssCs::Extensions::WriteLogIfError(t,
                                    gcnew String(L"ConnectSyncRoot::NOTIFY_RENAME Cloud->TrashItem"));

                                li->Delete(true);
                                SUCCESS = true;
                            }
                            else if (!ci->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::OwnedByMe) &&
                                ci_parent->CapabilitiesAndFlag.HasFlag(CloudCapabilitiesAndFlag::CanRemoveChildren))
                            {
                                UpdateCloudItem^ uci = gcnew UpdateCloudItem();
                                uci->Id = ci->Id;
                                uci->ParentIdsRemove->Add(ci_parent->Id);

                                Task^ t = srvm->CEVM->Cloud->UpdateMetadata(uci);
                                CssCs::Extensions::WriteLogIfError(t,
                                    gcnew String(L"ConnectSyncRoot::NOTIFY_RENAME Cloud->UpdateMetadata(!OwnedByMe,CanRemoveChildren)"));

                                li->Delete(true);
                                SUCCESS = true;
                            }
                            else reason = L"user not have permision on cloud";
                        }
                        else reason = L"li_parent & ci_parent not found";
                    }
                    else reason = L"ci/li not found or li.Flag has LockWaitUpdateFromCloudWatch";
                }
                else reason = L"srvm not found";
            }
        }
        else reason = L"No Internet";
        AckRename(callbackInfo->ConnectionKey, callbackInfo->TransferKey, CF_OPERATION_ACK_RENAME_FLAG_NONE, SUCCESS ? STATUS_SUCCESS : STATUS_UNSUCCESSFUL);
        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::NOTIFY_RENAME:").append(SUCCESS ? L"STATUS_SUCCESS" : reason)
            .append(L", path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
    }
    //when rename success
    void CALLBACK ConnectSyncRoot::NOTIFY_RENAME_COMPLETION(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->RenameCompletion.Flags;
        //callbackParameters->RenameCompletion.SourcePath;
        LPCWSTR reason = L"No error";
        String^ fullpath = gcnew String(callbackInfo->VolumeDosName) + gcnew String(callbackInfo->NormalizedPath);
        PinStr(fullpath);
        SyncRootViewModel^ srvm = SyncRootViewModel::FindWithConnectionKey(callbackInfo->ConnectionKey.Internal);
        if (srvm)
        {
            CloudItem^ ci = CloudItem::Select(gcnew String(GetFileIdentity(callbackInfo->FileIdentity)), srvm);
            LocalItem^ li = LocalItem::FindFromPath(srvm, fullpath, 0);
            if (ci && li)
            {
                bool newpathIsInSyncroot = (fullpath->Length > srvm->LocalPath->Length) && fullpath->Substring(0, srvm->LocalPath->Length)
                    ->Equals(srvm->LocalPath, StringComparison::OrdinalIgnoreCase);
                if (newpathIsInSyncroot) Placeholders::Update(srvm, li, ci);//remove insync state (spin icon)
            }
            else reason = L"ci/li not found";
        }
        else reason = L"srvm not found";
        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::NOTIFY_RENAME_COMPLETION, error:").append(reason)
            .append(L", path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
    }
    


    HRESULT ConnectSyncRoot::AckData(CF_CONNECTION_KEY connectionKey, CF_TRANSFER_KEY transferKey,
        CF_OPERATION_ACK_DATA_FLAGS Flags, NTSTATUS CompletionStatus, LARGE_INTEGER Offset, LARGE_INTEGER Length)
    {
        CF_OPERATION_INFO opInfo = { 0 };
        CF_OPERATION_PARAMETERS opParams = { 0 };
        opInfo.StructSize = sizeof(opInfo);
        opInfo.Type = CF_OPERATION_TYPE::CF_OPERATION_TYPE_ACK_DATA;
        opInfo.ConnectionKey = connectionKey;
        opInfo.TransferKey = transferKey;
        opParams.ParamSize = CF_SIZE_OF_OP_PARAM(CF_OPERATION_PARAMETERS::AckData);
        opParams.AckData.Flags = Flags;
        opParams.AckData.CompletionStatus = CompletionStatus;
        opParams.AckData.Offset = Offset;
        opParams.AckData.Length = Length;
        return CfExecute(&opInfo, &opParams);
    }
    void CALLBACK ConnectSyncRoot::VALIDATE_DATA(_In_ CONST CF_CALLBACK_INFO* callbackInfo, _In_ CONST CF_CALLBACK_PARAMETERS* callbackParameters)
    {
        //callbackParameters->ValidateData.Flags;
        //callbackParameters->ValidateData.RequiredFileOffset;
        //callbackParameters->ValidateData.RequiredLength;
        LogWriter::WriteLog(std::wstring(L"ConnectSyncRoot::VALIDATE_DATA ,path:").append(callbackInfo->VolumeDosName).append(callbackInfo->NormalizedPath), 1);
    }



    HRESULT ConnectSyncRoot::RetrieveData(CF_CONNECTION_KEY connectionKey, CF_TRANSFER_KEY transferKey, CF_OPERATION_RETRIEVE_DATA_FLAGS Flags,
        _Field_size_bytes_(Length.QuadPart) LPVOID Buffer, LARGE_INTEGER Offset, LARGE_INTEGER Length, LARGE_INTEGER ReturnedLength)
    {
        CF_OPERATION_INFO opInfo = { 0 };
        CF_OPERATION_PARAMETERS opParams = { 0 };
        opInfo.StructSize = sizeof(opInfo);
        opInfo.Type = CF_OPERATION_TYPE::CF_OPERATION_TYPE_RETRIEVE_DATA;
        opInfo.ConnectionKey = connectionKey;
        opInfo.TransferKey = transferKey;
        opParams.ParamSize = CF_SIZE_OF_OP_PARAM(CF_OPERATION_PARAMETERS::RetrieveData);
        opParams.RetrieveData.Flags = Flags;
        opParams.RetrieveData.Buffer = Buffer;
        opParams.RetrieveData.Offset = Offset;
        opParams.RetrieveData.Length = Length;
        opParams.RetrieveData.ReturnedLength = ReturnedLength;
        return CfExecute(&opInfo, &opParams);
    }

   
    HRESULT ConnectSyncRoot::RestartHydration(CF_CONNECTION_KEY connectionKey, CF_TRANSFER_KEY transferKey,
        CF_OPERATION_RESTART_HYDRATION_FLAGS Flags, CONST CF_FS_METADATA* FsMetadata,
        _Field_size_bytes_(FileIdentityLength) LPCVOID FileIdentity, DWORD FileIdentityLength)
    {
        CF_OPERATION_INFO opInfo = { 0 };
        CF_OPERATION_PARAMETERS opParams = { 0 };
        opInfo.StructSize = sizeof(opInfo);
        opInfo.Type = CF_OPERATION_TYPE::CF_OPERATION_TYPE_RESTART_HYDRATION;
        opInfo.ConnectionKey = connectionKey;
        opInfo.TransferKey = transferKey;
        opParams.ParamSize = CF_SIZE_OF_OP_PARAM(CF_OPERATION_PARAMETERS::RestartHydration);
        opParams.RestartHydration.Flags = Flags;
        opParams.RestartHydration.FsMetadata = FsMetadata;
        opParams.RestartHydration.FileIdentity = FileIdentity;
        opParams.RestartHydration.FileIdentityLength = FileIdentityLength;
        return CfExecute(&opInfo, &opParams);
    }
}