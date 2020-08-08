namespace CssCsData
{
  public enum CloudName:int
  {
    GoogleDrive = 0,
    OneDrive = 1,
    MegaNz = 2,
    Dropbox = 3,

    File = 250,
    Folder = 251,
    //Empty = 252,
    None = 255
  }

  //public CloudName CloudNameEnum
  //{
  //  get { return ((CloudName)CloudName); }
  //  set { CloudName = (long)value; }
  //}
}
