using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CssCsData.Data
{
  public enum PlacehoderResult : int
	{
		Success = 0,
		Failed = 1 << 0,
		OpenByOtherProcess = 1 << 1,
		CanNotOpen = 1 << 2,
		FileNotFound = 1 << 3,
	}
}
