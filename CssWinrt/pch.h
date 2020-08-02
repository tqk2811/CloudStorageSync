#pragma once
#ifdef CSSWINRT_EXPORTS
#define DLL_EXPORTS __declspec(dllexport)
#include <Windows.h>
#include <Unknwn.h>//IClassFactory

#include <winrt/Windows.Storage.Provider.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Security.Cryptography.h>
namespace winrt {
    using namespace Windows::Foundation;
    using namespace Windows::Foundation::Collections;
    using namespace Windows::Storage;
    using namespace Windows::Storage::Streams;
    using namespace Windows::Storage::Provider;
    using namespace Windows::Security::Cryptography;
}
#else
#define DLL_EXPORTS __declspec(dllimport)
#endif
#include "LogWriter.h"
#include "ExportStruct.h"

#ifdef CSSWINRT_EXPORTS
#include "ShellServices.h"
#include "Utilities.h"
#include "SyncRootRegistrar.h"
#endif

#include "ExportFunction.h"
