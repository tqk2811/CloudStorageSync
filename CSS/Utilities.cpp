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
    String^ FindNewFileName(SyncRootViewModelBase^ srvm, String^ ParentDirectory, String^ filename, String^ extension, CloudItem^ ci)
    {
        int i = 0;
        bool isfolder = ci->Size == -1;
        do
        {
            i++;
            String^ newpath = String::Format(CultureInfo::InvariantCulture, L"{0}\\{1} ({2}){3}", ParentDirectory, filename, i, String::IsNullOrEmpty(extension) ? String::Empty : extension);
            PinStr(newpath);
            DWORD dw = GetFileAttributes(pin_newpath);
            if (INVALID_FILE_ATTRIBUTES == dw) break;//file not found
            else
            {
                if (((dw & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY) != isfolder) continue;//file != folder -> continue
                else
                {
                    CF_PLACEHOLDER_STATE state = Placeholders::GetPlaceholderState(pin_newpath);
                    if (state == CF_PLACEHOLDER_STATE_INVALID) continue;
                    if (!(state & CF_PLACEHOLDER_STATE_PLACEHOLDER) == CF_PLACEHOLDER_STATE_PLACEHOLDER)
                    {
                        //not placeholder
                        if (isfolder) break;//-> convert
                        else
                        {
                            if (srvm->Status.HasFlag(SyncRootStatus::CreatingPlaceholder)) srvm->Message = String::Format(CultureInfo::InvariantCulture, L"Checking hash file: {0}", filename);
                            if (srvm->CEVM->Cloud->HashCheck(newpath, ci)) break;//same hash -> convert
                            else continue;//diff hash
                        }
                    }
                }

            }
        } while (true);
        return String::Format(CultureInfo::InvariantCulture, L"{0} ({1}){2}", filename, i, String::IsNullOrEmpty(extension) ? String::Empty : extension);
    }
    String^ FindNewNameItem(SyncRootViewModel^ srvm, String^ parentFullPath, CloudItem^ ci)
    {
        String^ Name = ci->Name->Trim();
        String^ extension = GetFileExtension(Name, ci->Size == -1);
        Name = FixFileName(Name, extension);
        return FindNewFileName(srvm, parentFullPath, Name, extension, ci);
    }
}
