﻿using System.Collections.Generic;

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
    public string ItemFullPath { get; set; }
    public string SRId { get; set; }
    public string CiId { get; set; }
    public LocalErrorType Type { get; set; }

    public static IList<LocalError> ListAll() => SqliteManager.LEListAll();
    public void Insert() => SqliteManager.LEInsert(ItemFullPath, SRId, Type, CiId);
    public static void Insert(string ItemFullPath, string SrId, LocalErrorType type, string CiId) => SqliteManager.LEInsert(ItemFullPath, SrId, type, CiId);
    public void Delete() => SqliteManager.LEDelete(SQLId);
    public static void Clear(string SrId) => SqliteManager.LEClear(SrId);
  }
}
