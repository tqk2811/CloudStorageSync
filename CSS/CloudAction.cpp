#include "pch.h"
#include "CloudAction.h"

namespace CSS
{
    struct READ_COMPLETION_CONTEXT
    {
        LARGE_INTEGER CallbackInfo_FileSize;
        CF_CONNECTION_KEY CallbackInfo_ConnectionKey;
        CF_TRANSFER_KEY CallbackInfo_TransferKey;
        CF_REQUEST_KEY CallbackInfo_RequestKey;
        LARGE_INTEGER StartOffset;
        LARGE_INTEGER RemainingLength;
        TransferData_CB TransferData;
        void Cancel()
        {
            LogWriter::WriteLog(L"DownloadItem cancel", 1);
            TransferData(
                CallbackInfo_ConnectionKey,
                CallbackInfo_TransferKey,
                NULL,
                StartOffset,
                RemainingLength,
                STATUS_UNSUCCESSFUL);
        }
    };
    ref class DownloadItem
    {
    private:
        int ReadStream(Stream^ stream)
        {
            int offset{ 0 };
            int byteread{ 0 };
            int byteneed = min(CHUNKSIZE, data->RemainingLength.QuadPart);
            do
            {
                byteread = stream->Read(buffer, offset, byteneed - offset);
                offset += byteread;
            } while (byteread != 0 && offset != byteneed);
            return offset;
        }
    public:
        READ_COMPLETION_CONTEXT* data;
        array<unsigned char>^ buffer;
        String^ FullPath;
        void Download(Task<Stream^>^ t, Object^ obj)
        {
            if (t->IsCanceled || t->IsFaulted || !t->Result)
            {
                if (t->IsFaulted)
                {
                    AggregateException^ ae = (AggregateException^)t->Exception;
                    WriteLog(String::Format(CultureInfo::InvariantCulture, "DownloadItem: Faulted, Exception Message:{0} ,StackTrace:{1}", 
                        ae->InnerException->Message, 
                        ae->InnerException->StackTrace), 0);
                }
                data->Cancel();
                return;
            }
            Stream^ stream = t->Result;
            LARGE_INTEGER byteread{ 0 };
            PinArr(buffer);
            PinStr(FullPath);

            try
            {
                do
                {
                    byteread.QuadPart = ReadStream(stream);
                    if (byteread.QuadPart)
                    {
                        CfReportProviderProgress(data->CallbackInfo_ConnectionKey,
                            data->CallbackInfo_TransferKey,
                            data->CallbackInfo_FileSize,
                            LongLongToLargeInteger(data->StartOffset.QuadPart + byteread.QuadPart));

                        HRESULT hr = data->TransferData(
                            data->CallbackInfo_ConnectionKey,
                            data->CallbackInfo_TransferKey,
                            pin_buffer,
                            data->StartOffset,
                            byteread,
                            0);

                        if (CheckHr(hr, L"DownloadItem ConnectSyncRoot::TransferData"))
                        {
                            data->StartOffset.QuadPart += byteread.QuadPart;
                            data->RemainingLength.QuadPart -= byteread.QuadPart;
                        }
                        else
                        {
                            data->Cancel();
                            break;
                        }
                    }
                } while (byteread.QuadPart != 0 && data->RemainingLength.QuadPart != 0);
            }
            catch (Exception^ ex)
            {
                data->Cancel();
                PinStr2(pin_message, ex->Message);
                LogWriter::WriteLog(std::wstring(L"Download exception message:").append(pin_message)
                    .append(L", Filepath:").append(pin_FullPath));
            }
            stream->Close();
            delete this;
        }
        DownloadItem()
        {
            buffer = gcnew array<unsigned char>(CHUNKSIZE);
        }
        ~DownloadItem()
        {
            //LogWriter::WriteLog(L"DownloadItem Deconstructor", 10);
            delete data;
        }
    };
	void CloudAction::Download(SyncRootViewModel^ srvm, CloudItem^ ci,
        CONST CF_CALLBACK_INFO* callbackInfo, CONST CF_CALLBACK_PARAMETERS* callbackParameters,TransferData_CB TransferData)
	{
        LONGLONG start = callbackParameters->FetchData.RequiredFileOffset.QuadPart;
        LONGLONG end = callbackParameters->FetchData.RequiredLength.QuadPart + start - 1;
        auto task_stream = srvm->SyncRootData->Account->AccountViewModel->Cloud->Download(ci, start, end);

        std::wstring fullClientPath(callbackInfo->VolumeDosName);
        fullClientPath.append(callbackInfo->NormalizedPath);
        DownloadItem^ di = gcnew DownloadItem();
        di->data = new READ_COMPLETION_CONTEXT();
        di->data->TransferData = TransferData;
        di->data->CallbackInfo_FileSize = callbackInfo->FileSize;
        di->data->CallbackInfo_ConnectionKey = callbackInfo->ConnectionKey;
        di->data->CallbackInfo_TransferKey = callbackInfo->TransferKey;
        di->data->StartOffset = callbackParameters->FetchData.RequiredFileOffset;
        di->data->RemainingLength = callbackParameters->FetchData.RequiredLength;
        di->data->CallbackInfo_RequestKey = callbackInfo->RequestKey;
        di->FullPath = gcnew String(fullClientPath.c_str());

        auto action = gcnew Action<Task<Stream^>^, Object^>(di, &DownloadItem::Download);
        task_stream->ContinueWith(action, di);
        return;
	}

}