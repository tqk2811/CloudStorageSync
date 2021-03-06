#pragma once
namespace CSS
{
	ref class SyncRootViewModel : CssCsData::SyncRootViewModelBase
	{
	private:
        bool _IsEditingDisplayName = false;
        INT64 ConnectionKey = 0;
        Watcher^ watcher;
        String^ _Status;
        String^ _Message;
        LocalItemRoot^ _Root;
        PropertyChangedEventHandler^ _PropertyChanged;
        Object^ eventLock;
        SyncRootStatus _EnumStatus = SyncRootStatus::NotWorking;
        void NotifyPropertyChange(String^ name)
        {
            PropertyChanged(this, gcnew PropertyChangedEventArgs(name));
        }

        bool CheckBeforeRegister()
        {
            if (String::IsNullOrEmpty(CloudFolderName) || String::IsNullOrEmpty(LocalPath)) return false;
            return true;
        }

        bool IsWorkChange(bool value)
        {
            if (value)//turn on
            {
                if (CheckBeforeRegister())
                {
                    this->SyncRootData->Account->AccountViewModel->Cloud->ListAllItemsToDb(this->SyncRootData, this->SyncRootData->CloudFolderId)
                        ->ContinueWith(gcnew Action<Task^>(this, &SyncRootViewModel::Register),
                            this->TokenSource->Token, TaskContinuationOptions::None, TaskScheduler::Default);
                    return true;
                }
                else return false;
            }
            else//turn off
            {
                System::Windows::Forms::DialogResult result = System::Windows::Forms::MessageBox::Show(gcnew String("Are you sure to unregister this syncroot"),
                    gcnew String("Confirm"), System::Windows::Forms::MessageBoxButtons::YesNo, System::Windows::Forms::MessageBoxIcon::Question);
                if (result == System::Windows::Forms::DialogResult::Yes)
                {
                    this->TokenSource->Cancel();
                    delete this->TokenSource;
                    this->TokenSource = gcnew CancellationTokenSource();
                    UnRegister();
                    return true;
                }
                else return false;
            }
        }

        void Register(Task^ task)
        {
            if (task->IsCanceled || task->IsFaulted) 
                return;
            this->SyncRootData->Flag = this->SyncRootData->Flag | SyncRootFlag::IsListed;
            this->SyncRootData->Update();
            Register();
        }
        void Register();
        void UnRegister();
        void LocalOnChanged(CustomFileSystemEventArgs^ e);


        void FindNonPlaceholderAndUpload()
        {
            PinStr(LocalPath);
            FindNonPlaceholderAndUpload(pin_LocalPath);
        }
        void FindNonPlaceholderAndUpload(LPCWSTR FullPath);

        
	public:
        SyncRootViewModel(SyncRoot^ syncRootData) : CssCsData::SyncRootViewModelBase(syncRootData)
        {
            if (nullptr == syncRootData) throw gcnew ArgumentNullException("syncRootData");
            syncRootData->SyncRootViewModel = this;
            eventLock = gcnew Object();
            Status = _EnumStatus.ToString();
            watcher = gcnew Watcher();
        }

        bool UpdateChange(ICloudItemAction^ change) override;

        //on start app, UI not run
        Task^ Run()
        {
            if (IsWork)
            {
                if (IsListed)
                {
                    if (CheckBeforeRegister()) return Task::Factory->StartNew(gcnew Action(this, &SyncRootViewModel::Register),
                                                      this->TokenSource->Token, TaskCreationOptions::LongRunning, TaskScheduler::Default);
                }
                else
                {
                    //set IsWork to false
                    this->SyncRootData->Flag = this->SyncRootData->Flag ^ SyncRootFlag::IsWork;
                    this->SyncRootData->Update();
                }
            }
            return Task::FromResult(0);
        }



        bool CheckConnectionKey(LONGLONG key) override
        {
            return this->ConnectionKey == key;
        }

        property SyncRootStatus EnumStatus
        {
            SyncRootStatus get() override { return _EnumStatus; }
            void set(CssCsData::SyncRootStatus value) override
            {
                if (value.HasFlag(SyncRootStatus::Error)) Status = "Error";
                else
                {
                    switch (value)
                    {
                    case SyncRootStatus::NotWorking: Status = "Not Working"; break;
                    case SyncRootStatus::ScanningCloud: Status = "Scanning Cloud"; break;
                    case SyncRootStatus::ScanningLocal: Status = "Scanning Local"; break;
                    case SyncRootStatus::CreatingPlaceholder: Status = "Creating Placeholder"; break;
                    case SyncRootStatus::RegisteringSyncRoot: Status = "Registering SyncRoot"; break;
                    case SyncRootStatus::Working: Status = "Working"; break;
                    default: return;
                    }
                }
                _EnumStatus = value;
            }
        }

        property LocalItemRoot^ Root
        {
            LocalItemRoot^ get() override { return _Root; }
            void set(LocalItemRoot^ value) override { _Root = value; }
        }

#pragma region MVVM
        event PropertyChangedEventHandler^ PropertyChanged
        {
            void add(PropertyChangedEventHandler^ handler) override
            {
                Monitor::Enter(eventLock);
                if (_PropertyChanged == nullptr)
                {
                    _PropertyChanged = static_cast<PropertyChangedEventHandler^> (Delegate::Combine(_PropertyChanged, handler));
                }
                else _PropertyChanged += handler;
                Monitor::Exit(eventLock);
            }

            void remove(PropertyChangedEventHandler^ handler) override
            {
                Monitor::Enter(eventLock);
                if (_PropertyChanged != nullptr) _PropertyChanged -= handler;
                Monitor::Exit(eventLock);
            }

            void raise(Object^ sender, PropertyChangedEventArgs^ e) override 
            {
                Monitor::Enter(eventLock);
                if (_PropertyChanged != nullptr) _PropertyChanged->Invoke(sender, e);
                Monitor::Exit(eventLock);
            }
        }

        property bool IsEditingDisplayName 
        {
            bool get() override { return _IsEditingDisplayName; }
            void set(bool value) override { _IsEditingDisplayName = value; NotifyPropertyChange("IsEditingDisplayName"); }
        }

        property bool IsWork
        {
            bool get() override { return this->SyncRootData->Flag.HasFlag(SyncRootFlag::IsWork); }
            void set(bool value) override 
            {
                if (IsWorkChange(value))//-> on accept & value = true -> List all item in cloud -> Flag = IsListed
                {
                    if (value) this->SyncRootData->Flag = SyncRootFlag::IsWork;
                    else this->SyncRootData->Flag =  SyncRootFlag::None;
                    this->SyncRootData->Update();
                    NotifyPropertyChange("IsWork");
                }                
            }
        }

        property bool IsListed
        {
            bool get() override { return this->SyncRootData->Flag.HasFlag(SyncRootFlag::IsListed); }
            void set(bool value) override
            {
                if(value) this->SyncRootData->Flag = this->SyncRootData->Flag | SyncRootFlag::IsListed;
                else this->SyncRootData->Flag = this->SyncRootData->Flag ^ SyncRootFlag::IsListed;
                this->SyncRootData->Update();
            }
        }

        property String^ CloudFolderName
        {
            String^ get() override { return this->SyncRootData->CloudFolderName; }
            void set(String^ value) override
            {
                this->SyncRootData->CloudFolderName = value;
                this->SyncRootData->Update();
                NotifyPropertyChange("CloudFolderName");
            }
        }        

        property String^ LocalPath
        {
            String^ get() override { return this->SyncRootData->LocalPath; }
            void set(String^ value) override
            {
                this->SyncRootData->LocalPath = value;
                this->SyncRootData->Update();
                NotifyPropertyChange("LocalPath");
            }
        }

        property String^ Status
        {
            String^ get() override { return _Status; }
            void set(String^ value) override { _Status = value; NotifyPropertyChange("Status"); }
        }

        property String^ Message
        {
            String^ get() override { return _Message; }
            void set(String^ value) override { _Message = value; NotifyPropertyChange("Message"); }
        }

        property String^ DisplayName
        {
            String^ get() override { return this->SyncRootData->DisplayName; }
            void set(String^ value) override
            {
                this->SyncRootData->DisplayName = value;
                this->SyncRootData->Update();
                NotifyPropertyChange("DisplayName");
            }
        }
#pragma endregion
	};
}


