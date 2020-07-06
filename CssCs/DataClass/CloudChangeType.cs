using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CssCs.DataClass
{
  public enum CloudChangeFlag : byte
  {
    None = 0,
    IsChangeTimeAndSize = 1,
    IsDeleted = 1 << 1,
    IsChangedId = 1 << 2,
    IsRename = 1 << 3,
    IsNewItem = 1 << 4
  }
  public class CloudChangeType
  {
    public CloudChangeType() { }
    internal CloudChangeType(string Id, IList<string> parent_old, IList<string> parent_new)
    {
      this.Id = Id;
      if (parent_old != null) ParentsRemove.AddRange(parent_old);
      else Flag |= CloudChangeFlag.IsNewItem;
      if (parent_new != null) ParentsNew.AddRange(parent_new);

      if (parent_old != null && parent_new != null) CheckParent();
    }
    public CloudChangeFlag Flag { get; set; } = CloudChangeFlag.None;
    public bool IsChangeParent
    {
      get
      {
        if (ParentsCurrent == null || (ParentsRemove.Count == 0 && ParentsNew.Count == 0)) return false;
        return true;
      }
    }

    public long SQLId { get; set; } = -1;
    public string CEId { get; set; }
    public string Id { get; set; }
    public string IdNew { get; set; }
    public List<string> ParentsRemove { get; } = new List<string>();
    public List<string> ParentsNew { get; } = new List<string>();
    public List<string> ParentsCurrent { get; } = new List<string>();
    public CloudItem CiNew { get; set; }
    void CheckParent()
    {
      for (int i = 0; i < ParentsRemove.Count; i++)
      {
        for (int j = 0; j < ParentsNew.Count; j++)
        {
          if (ParentsNew[j].Equals(ParentsRemove[i]))
          {
            ParentsCurrent.Add(ParentsRemove[i]);
            ParentsRemove.RemoveAt(i);
            ParentsNew.RemoveAt(j);
            i--;
            j--;
          }
        }
      }
    }
  }
}
