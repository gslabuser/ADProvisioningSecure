using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace ADProvisioning
{
    [DataContract]
    public class UpdateInfo
    {
        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string Property { get; set; }

        [DataMember]
        public string Value { get; set; }
    }
}