#pragma once
#include <Windows.h>
#include <cfapi.h>
#include <ntstatus.h>
#include <shellapi.h>//shell: tray icon
#include <string>//std::wstring
#include <gcroot.h>
#include <msclr\marshal.h>//PtrToStringChars
//#include <msclr\marshal_cppstd.h>
#include <shlwapi.h>//file/folder PathExists,...
#include "resource.h"

#using <CssCs.dll>
#include <CssWinrt/pch.h>
#define FILE_SHARE_ALL FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE
#define LengthFileIdentity 128
#define CHUNKSIZE 4096*4
#define HR_FileOpenningByOtherProcess 0x80070020
#define HR_OplockBrocken 0x80070322
#define HR_InUse 0x80070187
namespace CSS
{
	using namespace CssWinrt;
	using namespace CssCs;
	using namespace CssCs::DataClass;
	using namespace CssCs::UI;
	using namespace CssCs::UI::ViewModel;
	using namespace CssCs::Queues;
	using namespace CssCs::Cloud;
	using namespace System;
	using namespace System::Collections::Generic;
	using namespace System::Collections::ObjectModel;
	using namespace System::IO;
	using namespace System::Threading;
	using namespace System::Threading::Tasks;
	using namespace System::Text;
	using namespace System::Text::RegularExpressions;
	typedef enum class PlaceholderResult;
}

//no include
#include "Utilities.h"
#include "ShellCall.h"
#include "Placeholders.h"
#include "UploadQueue.h"
#include "UiManaged.h"
#include "ConnectSyncRoot.h"
#include "LocalAction.h"
#include "TrackChanges.h"
#include "SRManaged.h"
#include "CloudAction.h"

//include
