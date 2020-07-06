#include "pch.h"
#include "DirectoryWatcher.h"
namespace CssWinrt
{
    const size_t c_bufferSize = sizeof(FILE_NOTIFY_INFORMATION) * 100;

    concurrency::task<void> DirectoryWatcher::Initalize(const CFP* cfp)
    {
        _cfp = cfp;
        _path = cfp->LocalPath;
        _notify.reset(reinterpret_cast<FILE_NOTIFY_INFORMATION*>(new char[c_bufferSize]));

        _dir.attach(CreateFile(cfp->LocalPath,
            FILE_LIST_DIRECTORY,
            FILE_SHARE_READ,
            nullptr,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OVERLAPPED,
            nullptr));
        if (_dir.get() == INVALID_HANDLE_VALUE)
        {
            throw winrt::hresult_error(HRESULT_FROM_WIN32(GetLastError()));
        }
        //----------------------------
        auto token = _cancellationTokenSource.get_token();
        return concurrency::create_task([this, token] {
            LogWriter::WriteLog(std::wstring(L"Watcher starting, path:").append(_path).c_str(), 1);
            while (true)
            {
                DWORD returned;
                //winrt::check_bool(
                if (ReadDirectoryChangesW(_dir.get(), _notify.get(), c_bufferSize, TRUE, FILE_NOTIFY_CHANGE_ATTRIBUTES, &returned, &_overlapped, nullptr))
                {
                    DWORD transferred;
                    if (GetOverlappedResultEx(_dir.get(), &_overlapped, &transferred, 1000, FALSE))
                    {
                        std::list<std::wstring> result;
                        FILE_NOTIFY_INFORMATION* next = _notify.get();
                        while (next != nullptr)
                        {
                            std::wstring fullPath(_path);
                            fullPath.append(L"\\");
                            fullPath.append(std::wstring_view(next->FileName, next->FileNameLength / sizeof(wchar_t)));
                            result.push_back(fullPath);

                            if (next->NextEntryOffset) next = reinterpret_cast<FILE_NOTIFY_INFORMATION*>(reinterpret_cast<char*>(next) + next->NextEntryOffset);
                            else next = nullptr;
                        }
                        OnSyncRootFileChanges(result);
                    }
                    else if (GetLastError() != WAIT_TIMEOUT)
                    {
                        DWORD err = GetLastError();
                        LogWriter::WriteLog(std::wstring(L"DirectoryWatcher::ReadChangesAsync GetOverlappedResultEx error hr:")
                            .append(std::to_wstring(err)).c_str());
                        throw winrt::hresult_error(HRESULT_FROM_WIN32(err));
                    }
                    else if (token.is_canceled())
                    {
                        LogWriter::WriteLog(std::wstring(L"Watcher cancel received, path:").append(_path).c_str());
                        concurrency::cancel_current_task();
                        return;
                    }
                }
                else
                {
                    LogWriter::WriteLog(std::wstring(L"DirectoryWatcher::ReadChangesAsync ReadDirectoryChangesW error hr:")
                        .append(std::to_wstring(GetLastError())).c_str());
                }
            }
        }, token);
    }

    void DirectoryWatcher::Cancel()
    {
        LogWriter::WriteLog(std::wstring(L"Canceling watcher, path:").append(_path).c_str());
        _cancellationTokenSource.cancel();
    }


    void DirectoryWatcher::OnSyncRootFileChanges(std::list<std::wstring>& changes)
    {
        for (auto path : changes)
        {
            //LogWriter::WriteLog(std::wstring(L"Processing change for path:").append(path).c_str());
            DWORD attrib = GetFileAttributes(path.c_str());
            if (!(attrib & FILE_ATTRIBUTE_DIRECTORY))
            {
                winrt::handle placeholder(CreateFile(path.c_str(), 0, FILE_READ_DATA, nullptr, OPEN_EXISTING, 0, nullptr));

                LARGE_INTEGER offset = {};
                LARGE_INTEGER length;
                length.QuadPart = MAXLONGLONG;

                if (attrib & FILE_ATTRIBUTE_PINNED)
                {
                    LogWriter::WriteLog(std::wstring(L"Hydrating file:").append(path).c_str(),1);
                    CfHydratePlaceholder(placeholder.get(), offset, length, CF_HYDRATE_FLAG_NONE, NULL);
                }
                else if (attrib & FILE_ATTRIBUTE_UNPINNED)
                {
                    LogWriter::WriteLog(std::wstring(L"Dehydrating file:").append(path).c_str(),1);
                    CfDehydratePlaceholder(placeholder.get(), offset, length, CF_DEHYDRATE_FLAG_NONE, NULL);
                }
            }
        }
    }
}