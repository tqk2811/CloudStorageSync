#pragma once
#ifdef CSSWINRT_EXPORTS
#define DLL_EXPORTS __declspec(dllexport)
#include <Windows.h>
#include <Unknwn.h>//IClassFactory

#include <shlwapi.h>//check file path,BHID_ThumbnailHandler
#include <pathcch.h>//PathAllocCombine (thumbnail)
#include <ShlObj_core.h>

#include <bcrypt.h>//ntstatus
#include <ntstatus.h>//cfapi
#include <cfapi.h>//cloud filter
#include <sddl.h>//ConvertSidToStringSid (Utilities.h)
#include <ppltasks.h>//concurrency::task

#include <winrt/Windows.ApplicationModel.h>
#include <winrt/Windows.Storage.AccessCache.h>
#include <winrt/Windows.Storage.Pickers.h>
#include <winrt/Windows.Storage.Provider.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Security.Cryptography.h>
namespace winrt {
    using namespace Windows::Foundation;
    using namespace Windows::Foundation::Collections;
    using namespace Windows::Storage;
    using namespace Windows::Storage::Pickers;
    using namespace Windows::Storage::AccessCache;
    using namespace Windows::Storage::Streams;
    using namespace Windows::Storage::Provider;
    using namespace Windows::Security::Cryptography;
}
#else
#define DLL_EXPORTS __declspec(dllimport)
#endif
namespace CssWinrt
{
    enum class SyncRootRegisterStatus
    {
        Registed,
        Exist,
        Failed
    };
}
#include "LogWriter.h"

#ifdef CSSWINRT_EXPORTS
#include "ShellServices.h"
#include "Utilities.h"
#include "SyncRootRegistrar.h"
#endif

#include "ExportFunction.h"
