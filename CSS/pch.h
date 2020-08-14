#pragma once
#include <Windows.h>
#include <cfapi.h>
#include <ntstatus.h>
#include <shellapi.h>//shell: tray icon
#include <string>//std::wstring
#include <gcroot.h>
#include <msclr\marshal.h>//PtrToStringChars
//#include <msclr\marshal_cppstd.h>
#include <shlwapi.h>//file/folder,...
#include "resource.h"

#using <CssCs.dll>
#using <CssCsCloud.dll>
#using <CssCsData.dll>
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
	using namespace CssCs::Queues;
	using namespace CssCs::UI;
	using namespace CssCs::UI::ViewModel;
	using namespace CssCsData;
	using namespace CssCsData::Data;
	using namespace CssCsData::Cloud;
	using namespace CssCsCloud;
	using namespace CssCsCloud::Cloud;
	using namespace CssCsCloud::CustomStream;

	using namespace System;
	using namespace System::Runtime::CompilerServices;
	using namespace System::Collections::Generic;
	using namespace System::IO;
	using namespace System::Threading;
	using namespace System::Threading::Tasks;
	using namespace System::Text;
	using namespace System::Text::RegularExpressions;
	using namespace System::Globalization;
	using namespace System::ComponentModel;	
	typedef ref class SyncRootViewModel;
}

//no include
#include "Utilities.h"
#include "ShellCall.h"
#include "Placeholders.h"
#include "UploadQueue.h"
#include "UiManaged.h"
#include "SyncRootViewModel.h"
#include "LocalAction.h"
#include "CloudAction.h"
#include "ConnectSyncRoot.h"//typedef
//include
