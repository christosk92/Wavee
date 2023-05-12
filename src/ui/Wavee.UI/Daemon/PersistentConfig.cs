using Newtonsoft.Json;
using Wavee.UI.Bases;

namespace Wavee.UI.Daemon;


[JsonObject(MemberSerialization.OptIn)]
public class PersistentConfig : ConfigBase
{
    /// <summary>
    /// Constructor for config population using Newtonsoft.JSON.
    /// </summary>
    public PersistentConfig() : base()
    {
    }

    public PersistentConfig(string filePath) : base(filePath)
    {
    }
}