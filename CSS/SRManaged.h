#pragma once
namespace CSS
{
	class SRManaged
	{
	public:
		static void Init();
		static void UnInit();
		static void Register(SyncRootViewModel^ srvm);
		static void UnRegister(SyncRootViewModel^ srvm);
	};
}
