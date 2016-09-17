#if DEBUG
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.XboxMusic.Abstractions.ProjectReferences
{
    public enum EnumReferences
    {
        [EnumMember]
        Value
    }
    [DataContract]
    public class References
    {
        [Description]
        [JsonIgnore]
        public string Value { get; set; }
    }
}
#endif