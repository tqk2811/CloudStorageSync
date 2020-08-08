namespace CssCsData
{
  public enum CloudItemFlag : long
  {
    None = 0,

    CanDownload = 1 << 0,
    CanEdit = 1 << 1,
    CanRename = 1 << 2,
    CanShare = 1 << 3,
    CanTrash = 1 << 4,
    CanUntrash = 1 << 5,

    CanAddChildren = 1 << 6,
    CanRemoveChildren = 1 << 7,


    OwnedByMe = 1 << 62,
    All = CanDownload | CanEdit | CanRename | CanShare | CanTrash | CanUntrash | CanAddChildren | CanRemoveChildren |
          OwnedByMe,
  }

  //public CloudCapabilitiesAndFlag CapabilitiesAndFlagEnum
  //{
  //  get { return ((CloudCapabilitiesAndFlag)CapabilitiesAndFlag); }
  //  set { CapabilitiesAndFlag = (long)value; }
  //}
}
