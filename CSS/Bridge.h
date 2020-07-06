#pragma once

namespace CSS
{
	class Bridge
	{
	public:
		static void LoadCallback();
	};
	void WriteLog(System::String^ text, int loglevel = 10);
}
