#include "pch.h"
#include "Utilities.h"
namespace CSS
{
    bool GetFileInformation(LPCWSTR fullpath, BY_HANDLE_FILE_INFORMATION& info)
    {
        HANDLE hfile = CreateFile(fullpath, 0, FILE_READ_DATA, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, 0);
        bool result{ false };
        if (INVALID_HANDLE_VALUE != hfile) result = GetFileInformationByHandle(hfile, &info);
        CloseHandle(hfile);
        return result;
    }
    bool TwoItemIsHardLink(LPCWSTR fullpath0, LPCWSTR fullpath1)
    {
        BY_HANDLE_FILE_INFORMATION info0{};
        BY_HANDLE_FILE_INFORMATION info1{};
        if (GetFileInformation(fullpath0, info0) && GetFileInformation(fullpath1, info1) &&
            info0.dwVolumeSerialNumber == info1.dwVolumeSerialNumber &&
            info0.nFileIndexLow == info1.nFileIndexLow && info0.nFileIndexHigh == info1.nFileIndexHigh) return true;
        return false;
    }

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
    String^ FixLengthFileName(String^ filename, String^ extension)
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
    String^ FindNewFileName(SyncRootViewModel^ srvm, String^ ParentDirectory, String^ filename, String^ extension, CloudItem^ ci, bool hardlink)
    {
        int i = 0;
        bool isfolder = ci->Size == -1;
        do
        {
            i++;
            String^ newpath = String::Format(CultureInfo::InvariantCulture, L"{0}\\{1} ({2}){3}", ParentDirectory, filename, i, String::IsNullOrEmpty(extension) ? String::Empty : extension);
            PinStr(newpath);
            DWORD dw = GetFileAttributes(pin_newpath);
            if (INVALID_FILE_ATTRIBUTES == dw) break;//file not found -> new name
            else
            {
                if (((dw & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY) != isfolder) continue;//file != folder -> continue
                else//same file / folder
                {
                    CF_PLACEHOLDER_STATE state = Placeholders::GetPlaceholderState(pin_newpath);//
                    if (state == CF_PLACEHOLDER_STATE_INVALID) continue;//can't open file,... -> continue
                    if (!(state & CF_PLACEHOLDER_STATE_PLACEHOLDER) == CF_PLACEHOLDER_STATE_PLACEHOLDER)//file is not placeholder
                    {
                        //not placeholder
                        if (isfolder)
                        {
                            if (hardlink) continue;//can't convert, need create hardlink
                            else break;//is folder -> convert
                        }
                        else//file -> hash check
                        {
                            if (hardlink) continue;//can't convert, need create hardlink
                            else
                            {
                                if (srvm->EnumStatus.HasFlag(SyncRootStatus::CreatingPlaceholder)) srvm->Message = String::Format(CultureInfo::InvariantCulture, L"Checking hash file: {0}", filename);
                                if (srvm->SyncRootData->Account->AccountViewModel->Cloud->HashCheck(newpath, ci)) break;//same hash -> convert
                                else continue;//diff hash
                            }
                        }
                    }
                    //else file is placeholder
                }

            }
        } while (true);
        return String::Format(CultureInfo::InvariantCulture, L"{0} ({1}){2}", filename, i, String::IsNullOrEmpty(extension) ? String::Empty : extension);
    }
    String^ FindNewNameItem(SyncRootViewModel^ srvm, String^ parentFullPath, CloudItem^ ci, bool hardlink)
    {
        String^ Name = ci->Name->Trim();
        String^ extension = GetFileExtension(Name, ci->Size == -1);
        Name = FixLengthFileName(Name, extension);
        return FindNewFileName(srvm, parentFullPath, Name, extension, ci, hardlink);
    }
}
