#include "pch.h"
#include "LogWriter.h"
#include <ctime>
#include <iomanip>
#include <iostream>
#include <fstream>
#include <codecvt>
#include <sstream>
#include <comdef.h>
namespace CssWinrt
{
	std::fstream log_file;
	int _loglevel = 10;
	std::string wstring_to_utf8(const std::wstring& str) {
		std::wstring_convert<std::codecvt_utf8<wchar_t>> myconv;
		return myconv.to_bytes(str);
	}

	void LogWriter::Init(int loglevel)
	{
		_loglevel = loglevel;
		auto log_path = GetLocalStateUWPFolder();
		log_path.append(L"\\Log");
		if (!PathFileExists(log_path.c_str())) CreateDirectory(log_path.c_str(), NULL);

		time_t rawtime;
		struct tm* timeinfo;
		WCHAR buffer[40];
		time(&rawtime);
		timeinfo = localtime(&rawtime);
		wcsftime(buffer, sizeof(buffer), L"%Y-%m-%d.log", timeinfo);//strftime(buffer, sizeof(buffer), "%d-%m-%Y %H:%M:%S", timeinfo);
		log_path.append(L"\\").append(buffer);// %H:%M:%S

		if (!PathFileExists(log_path.c_str())) CloseHandle(CreateFile(	log_path.c_str(),
																		GENERIC_ALL,
																		FILE_SHARE_READ | FILE_SHARE_DELETE,
																		NULL,
																		CREATE_NEW,
																		FILE_ATTRIBUTE_NORMAL,
																		NULL));
		log_file.open(log_path, std::fstream::in | std::fstream::out | std::fstream::ate);
		log_file << "\nApp start\n";
	}
	void LogWriter::ShutDown()
	{
		log_file << "App shutdown\n";
		log_file.close();
	}

	//0
	void LogWriter::WriteLog(LPCSTR text, int writelevel)
	{
		if (_loglevel < writelevel) return;
		time_t rawtime;
		struct tm* timeinfo;
		CHAR buffer[60];
		time(&rawtime);
		timeinfo = localtime(&rawtime);
		strftime(buffer, sizeof(buffer), "%Y/%m/%d %H:%M:%S ", timeinfo);

		std::stringstream ss;
		ss << buffer << "(0x" << std::hex << GetCurrentProcessId() << ":0x" << std::hex << GetCurrentThreadId() << "):\t" << text << "\n";		
#if _DEBUG
		OutputDebugStringA(ss.str().c_str());
#endif
		log_file << ss.str();
	}
	//1
	void LogWriter::WriteLog(std::string& text, int writelevel)
	{
		WriteLog(text.c_str(), writelevel);
	}

	//1
	void LogWriter::WriteLog(LPCWSTR text, int writelevel)
	{
		std::wstring wstr(text);
		std::string str = wstring_to_utf8(text);
		WriteLog(str.c_str(), writelevel);
	}
	//1
	void LogWriter::WriteLog(std::wstring& text, int writelevel)
	{
		std::string str = wstring_to_utf8(text);
		WriteLog(str.c_str(), writelevel);
	}

	

	//1
	void LogWriter::WriteLogError(LPCSTR text, HRESULT hresult) 
	{
		_com_error err(hresult);
		std::string errormsg = wstring_to_utf8(std::wstring(err.ErrorMessage()));
		std::stringstream ss;
		ss << text << ", HRESULT: 0x" << std::hex << hresult << L" " << errormsg.c_str();
		WriteLog(ss.str().c_str(), 0);
	}
	//1
	void LogWriter::WriteLogError(LPCSTR text, int errorcode)
	{
		std::stringstream ss;
		ss << text << ", error code: " << std::dec << errorcode;
		WriteLog(ss.str().c_str(), 0);
	}

	//2
	void LogWriter::WriteLogError(std::string& text, HRESULT hresult)
	{
		WriteLogError(text.c_str(), hresult);
	}
	//2
	void LogWriter::WriteLogError(std::string& text, int errorcode)
	{
		WriteLogError(text.c_str(), errorcode);
	}

	

	//2
	void LogWriter::WriteLogError(LPCWSTR text, HRESULT hresult)
	{
		_com_error err(hresult);
		std::wstringstream ss;
		ss << text << L", HRESULT: 0x" << std::hex << hresult << L" " << err.ErrorMessage();
		WriteLog(ss.str().c_str(), 0);
	}
	//2
	void LogWriter::WriteLogError(LPCWSTR text, int errorcode)
	{
		std::wstringstream ss;
		ss << text << L", error code: " << std::dec << errorcode;
		WriteLog(ss.str().c_str(), 0);
	}

	//3
	void LogWriter::WriteLogError(std::wstring& text, HRESULT hresult)
	{
		WriteLogError(text.c_str(), hresult);
	}
	//3
	void LogWriter::WriteLogError(std::wstring& text, int errorcode)
	{
		WriteLogError(text.c_str(), errorcode);
	}
}