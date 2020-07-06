using System.Collections.Generic;

namespace CssCs.DataClass
{
  public enum LocalErrorType : int
  {
    Revert,
    Update,
    Convert,
    Rename
  };
  public class LocalError
  {
    public long SQLId { get; set; } = -1;
    public long LI_Id { get; set; } = -1;
    public string SRId { get; set; }
    public string CIId { get; set; }
    public LocalErrorType Type { get; set; }

    public static IList<LocalError> ListAll() => SqliteManager.LEListAll();
    public void Insert() => SqliteManager.LEInsert(LI_Id, SRId, Type, CIId);
    public static void Insert(long LiId, string SrId, LocalErrorType type, string CiId) => SqliteManager.LEInsert(LiId, SrId, type, CiId);
    public void Delete() => SqliteManager.LEDelete(SQLId);
    public static void Clear(string SrId) => SqliteManager.LEClear(SrId);
  }
}
