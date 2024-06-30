using LDAP_Connector.Helpers;
using System.ComponentModel;

namespace LDAP_Connector.Models
{
    public class AdUser
    {
        [DisplayName("samAccountName")]
        public string Login { get; set; }

        [DisplayName("cn")]
        public string Cn { get; set; }

        [DisplayName("distinguishedName")]
        public string Dn { get; set; }

        [DisplayName("sn")]
        public string LastName { get; set; }

        [DisplayName("givenName")]
        public string FirstName { get; set; }

        [DisplayName("displayName")]
        public string DisplayName { get; set; }

        [DisplayName("name")]
        public string Name { get; set; }

        [DisplayName("userAccountControl")]
        public string UserControlFlag { get; set; }

        [DisplayName("userPrincipalName")]
        public string UserPrincipalName { get; set; }

        [DisplayName("mailNickname")]
        public string MailNickname { get; set; }

        [DisplayName("title")]
        public string Title { get; set; }

        [DisplayName("description")]
        public string Description { get; set; }

        [DisplayName("accountExpires")]
        public string AccountExpires { get; set; }

        [DisplayName("mail")]
        public string Email { get; set; }

        [DisplayName("proxyAddresses")]
        public string[] ProxyAddresses { get; set; }

        [DisplayName("targetAddress")]
        public string TargetAddress { get; set; }

        [DisplayName("manager")]
        public string ManagerDn { get; set; }

        [DisplayName("c")]
        public string Region { get; set; }

        [DisplayName("company")]
        public string Company { get; set; }

        [DisplayName("department")]
        public string Department { get; set; }

        [DisplayName("telephoneNumber")]
        public string BusinessPhone { get; set; }

        [DisplayName("mobile")]
        public string MobilePhone { get; set; }

        [DisplayName("physicalDeliveryOfficeName")]
        public string Location { get; set; }

        [DisplayName("l")]
        public string City { get; set; }

        [DisplayName("postalCode")]
        public string ZipPostalCode { get; set; }

        [DisplayName("streetAddress")]
        public string Street { get; set; }

        [DisplayName("co")]
        public string Country { get; set; }

        [DisplayName("memberOf")]
        public string[] MemberOf { get; set; }

        [DisplayName("objectClass")]
        public string[] ObjectClass { get; set; }

        [DisplayName("objectSid")]
        public byte[] ObjectSid { get; set; }

        [DisplayName(AdAttributesHelper.AdditionalAttributesName)]
        public IDictionary<string, object> AdditionalAttributes { get; set; }

        public bool IsActive
        {
            get
            {
                try
                {
                    return !Convert.ToBoolean(int.Parse(UserControlFlag) & 0x0002);
                }
                catch { return false; }
            }
        }

        public DateTimeOffset? AccountExpiresDateTime
        {
            get
            {
                try
                {
                    return AdAttributesHelper.GetDateTimeFromTicks(long.Parse(AccountExpires));
                }
                catch { return null; }
            }
        }
    }
}
