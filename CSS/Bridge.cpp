#include "pch.h"
#include "Bridge.h"
namespace CSS
{
	void TestWatchCloud()
	{
		TrackChanges::OnElapsed(nullptr, nullptr);
	}

	bool ConvertToPlaceholder(SyncRootViewModel^ srvm, LocalItem^ li, String^ fileIdentity)
	{
		return Placeholders::Convert(srvm, li, fileIdentity);
	}

	bool UpdatePlaceholder(SyncRootViewModel^ srvm, LocalItem^ li, CloudItem^ ci)
	{
		return Placeholders::Update(srvm, li, ci);
	}

	void Bridge::LoadCallback()
	{
		CssCs::CPPCLR_Callback::OutPutDebugString = gcnew CssCs::_OutPutDebugString(WriteLog);
		CssCs::CPPCLR_Callback::TestWatchCloud = gcnew CssCs::_TestWatchCloud(TestWatchCloud);

		CssCs::CPPCLR_Callback::SRRegister = gcnew CssCs::_SRRegister(SRManaged::Register);
		CssCs::CPPCLR_Callback::SRUnRegister = gcnew CssCs::_SRUnRegister(SRManaged::UnRegister);

		CssCs::CPPCLR_Callback::ConvertToPlaceholder = gcnew CssCs::_ConvertToPlaceholder(ConvertToPlaceholder);
		CssCs::CPPCLR_Callback::UpdatePlaceholder = gcnew CssCs::_UpdatePlaceholder(UpdatePlaceholder);
	}

	void WriteLog(System::String^ text,int loglevel)
	{
		PinStr(text);
		LogWriter::WriteLog(pin_text,loglevel);
	}
}
