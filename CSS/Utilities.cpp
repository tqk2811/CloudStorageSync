#include "pch.h"
#include "Utilities.h"
namespace CSS
{
	gcroot<Regex^> rg_extension = gcnew Regex(gcnew String(L"\\.[^\\.]+$"));
	gcroot<Regex^> rg_subname = gcnew Regex(gcnew String(L"\\(\\d+\\)$"));
	String^ GetFileExtension(String^& filename, bool isfolder)
	{
		Match^ m = rg_extension->Match(filename);
		String^ extension{ nullptr };
		if (!isfolder && m->Success)
		{
			extension = m->Value;
			filename = filename->Substring(0, filename->Length - extension->Length)->Trim();
		}
		return extension;
	}
	String^ FixFileName(String^ filename, String^ extension)
	{
		Match^ m = rg_subname->Match(filename);
		if (m->Success)
		{
			filename = filename->Substring(0, filename->Length - m->Value->Length)->Trim();
		}

		if ((filename->Length + String::IsNullOrEmpty(extension) ? 0 : extension->Length) >= 250)
			filename = filename->Substring(0, 250 - extension->Length);

		return filename;
	}
	String^ FindNewFileName(String^ ParentDirectory, String^ filename, String^ extension)
	{
		int i = 0;
		do
		{
			i++;
			String^ newpath = String::Format(L"{0}\\{1} ({2}){3}", ParentDirectory, filename, i, String::IsNullOrEmpty(extension) ? String::Empty : extension);
			PinStr(newpath);
			if (!PathFileExists(pin_newpath)) break;//file not found
			else
			{
				//check if not placeholder
				CF_PLACEHOLDER_STATE state = Placeholders::GetPlaceholderState(pin_newpath);
				if (state != CF_PLACEHOLDER_STATE_INVALID && //not invalid
					!(state & CF_PLACEHOLDER_STATE_PLACEHOLDER) == CF_PLACEHOLDER_STATE_PLACEHOLDER)//not placeholder
				{
					break;
				}
			}
		} while (true);
		return String::Format(L"{0} ({1}){2}", filename, i, String::IsNullOrEmpty(extension) ? String::Empty : extension);
	}
	String^ FindNewNameItem(String^ parentFullPath, String^ Name,bool isfolder)
	{
		Name = Name->Trim();
		String^ extension = GetFileExtension(Name, isfolder);
		Name = FixFileName(Name, extension);
		return FindNewFileName(parentFullPath, Name, extension);
	}
}
