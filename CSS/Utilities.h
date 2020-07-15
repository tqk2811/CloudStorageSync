#pragma once
#include <codecvt>
namespace CSS
{
#define PinStr3(name,SS)(name) = PtrToStringChars((SS))
#define PinStr2(name,SS)pin_ptr<const wchar_t> (name) = PtrToStringChars((SS))
#define PinStr(SS)pin_ptr<const wchar_t> pin_##SS = PtrToStringChars((SS))
#define PinArr(cliarr)pin_ptr<unsigned char> pin_##cliarr = &cliarr[0]
#define GetFileIdentity(FileIdentity)(const char*)FileIdentity
	typedef struct MY_CF_PLACEHOLDER_STANDARD_INFO {
		LARGE_INTEGER OnDiskDataSize;
		LARGE_INTEGER ValidatedDataSize;
		LARGE_INTEGER ModifiedDataSize;
		LARGE_INTEGER PropertiesSize;
		CF_PIN_STATE PinState;
		CF_IN_SYNC_STATE InSyncState;
		LARGE_INTEGER FileId;
		LARGE_INTEGER SyncRootFileId;
		ULONG FileIdentityLength;
		char FileIdentity[LengthFileIdentity]{ 0 };

		void LoadFileIdentity(String^ FileId)
		{
			array<unsigned char>^ fileid = Encoding::ASCII->GetBytes(FileId);
			PinArr(fileid);
			memcpy(this->FileIdentity, pin_fileid, FileId->Length);
		}
	} MY_CF_PLACEHOLDER_STANDARD_INFO;

	inline static void FillFileIdentity(char* FileIdentity,String^ FileId)
	{
		array<unsigned char>^ fileid = Encoding::ASCII->GetBytes(FileId);
		PinArr(fileid);
		memcpy(FileIdentity, pin_fileid, FileId->Length);
	}

	inline static LARGE_INTEGER LongLongToLargeInteger(_In_ const LONGLONG longlong)
	{
		LARGE_INTEGER largeInteger;
		largeInteger.QuadPart = longlong;
		return largeInteger;
	}

	inline static HICON LoadIconFromResource(int iDC, UINT fuLoad = LR_DEFAULTCOLOR, int cx = 32, int cy = 32)
	{
		return static_cast<HICON>(::LoadImage(/*(HINSTANCE)*/GetModuleHandle(NULL), MAKEINTRESOURCE(iDC), IMAGE_ICON, cx, cy, fuLoad));
	}

	//need delete
	inline static WCHAR* WStringToWCHARP(std::wstring& s)
	{
		WCHAR* c_p = new WCHAR[s.size() + 1];
		memcpy(c_p, s.c_str(), sizeof(WCHAR) * s.size());
		c_p[s.size()] = 0;
		return c_p;
	}

	//convert wstring to UTF-8 string
	inline static std::string wstring_to_utf8(const std::wstring& wstr)
	{
		if (wstr.empty()) return std::string();
		int size_needed = WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), NULL, 0, NULL, NULL);
		std::string strTo(size_needed, 0);
		WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), &strTo[0], size_needed, NULL, NULL);
		return strTo;
	}

	inline static String^ ToBase64String(String^ s)
	{
		return System::Convert::ToBase64String(System::Text::Encoding::UTF8->GetBytes(s));
	}
	inline static String^ FromBase64String(String^ base64)
	{
		return System::Text::Encoding::UTF8->GetString(System::Convert::FromBase64String(base64));
	}

	inline static String^ MakeSplitString(IList<String^>^ input)
	{
		if (input == nullptr || input->Count == 0) return String::Empty;
		String^ result = input[0];
		for (int i = 1; i < input->Count; i++) result += (L"|" + input[i]);
		return result;
	}
	inline static List<String^>^ StringSplit(String^ input)
	{
		List<String^>^ lists = gcnew List<String^>();
		if (String::IsNullOrEmpty(input)) return lists;
		array<wchar_t>^ arr = { L'|' };
		lists->AddRange(input->Split(arr));
		return lists;
	}

	inline static bool MoveToRecycleBin(std::wstring& fullpath)
	{
		WCHAR* p = new WCHAR[fullpath.size() + 2]{ 0 };
		wcscpy(p, fullpath.c_str());
		SHFILEOPSTRUCT shfile{ 0 };
		shfile.wFunc = FO_DELETE;
		shfile.pFrom = p;
		shfile.fFlags = FOF_ALLOWUNDO | FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI;
		int result = SHFileOperation(&shfile);
		delete p;
		return result == 0;
	}

	inline static bool FindId(List<String^>^ Ids, String^ IdFind)
	{
		for (int i = 0; i < Ids->Count; i++) if (Ids[i]->Equals(IdFind)) return true;
		return false;
	}

	String^ FindNewNameItem(String^ parentFullPath, String^ Name, bool isfolder);

	inline static bool CheckHr(HRESULT hr,LPCWSTR info, LPCWSTR info2 = nullptr,bool WriteLogSucceeded = false)
	{
		std::wstring log(info);		
		if (SUCCEEDED(hr))
		{
			if (WriteLogSucceeded)
			{
				log.append(L" ").append(L"Succeeded");
				if (info2) log.append(L", ").append(info2);
				LogWriter::WriteLog(log, 1);
			}
			return true;
		}
		else
		{
			if (info2) log.append(L" ").append(info2);
			LogWriter::WriteLogError(log, hr);
			return false;
		}		
	}

	inline static void WriteLogTaskIfError(Task^ t, LPCWSTR info)
	{
		TaskContinueWriteLogIfError^ check = gcnew TaskContinueWriteLogIfError(gcnew String(info));
		auto action = gcnew Action<Task^>(check, &TaskContinueWriteLogIfError::Check);
		t->ContinueWith(action);
	}
}
