using LDAP_Connector.Helpers;
using System.ComponentModel;

namespace LDAP_Connector.Models
{
    public class AdGroup
    {
        [DisplayName("samAccountName")]
        public string TechnicalName { get; set; }

        [DisplayName("cn")]
        public string Cn { get; set; }

        [DisplayName("distinguishedName")]
        public string Dn { get; set; }

        [DisplayName("displayName")]
        public string DisplayName { get; set; }

        [DisplayName("description")]
        public string Description { get; set; }

        [DisplayName("managedBy")]
        public string ManagerDn { get; set; }

        [DisplayName("member")]
        public string[] Members { get; set; }

        [DisplayName("objectClass")]
        public string[] ObjectClass { get; set; }

        [DisplayName("objectSid")]
        public byte[] ObjectSid { get; set; }

        [DisplayName(AdAttributesHelper.AdditionalAttributesName)]
        public IDictionary<string, object> AdditionalAttributes { get; set; }
    }
}
