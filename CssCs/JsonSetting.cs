using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CssCs
{
  public sealed class JsonSetting
  {
    public static JsonSerializerSettings jsonSerializerSettings { get; } = new JsonSerializerSettings
    {
      NullValueHandling = NullValueHandling.Ignore
    };
  }
}
