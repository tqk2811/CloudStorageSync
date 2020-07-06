#pragma once
namespace CssWinrt 
{
	class LogWriter
	{
	public:
		//level 0 = error
		//level 1 = main run
		DLL_EXPORTS static void Init(int loglevel = 10);		
		DLL_EXPORTS static void ShutDown();

		DLL_EXPORTS static void WriteLog(LPCSTR text, int writelevel = 10);
		DLL_EXPORTS static void WriteLog(std::string& text, int writelevel = 10);
		DLL_EXPORTS static void WriteLog(LPCWSTR text, int writelevel = 10);
		DLL_EXPORTS static void WriteLog(std::wstring &text, int writelevel = 10);

		
		DLL_EXPORTS static void WriteLogError(LPCSTR text, HRESULT hresult);
		DLL_EXPORTS static void WriteLogError(LPCSTR text, int errorcode);

		DLL_EXPORTS static void WriteLogError(std::string& text, HRESULT hresult);
		DLL_EXPORTS static void WriteLogError(std::string& text, int errorcode);
		//-------------------

		DLL_EXPORTS static void WriteLogError(LPCWSTR text, HRESULT hresult);
		DLL_EXPORTS static void WriteLogError(LPCWSTR text, int errorcode);

		DLL_EXPORTS static void WriteLogError(std::wstring& text, HRESULT hresult);
		DLL_EXPORTS static void WriteLogError(std::wstring& text, int errorcode);
	};
}
