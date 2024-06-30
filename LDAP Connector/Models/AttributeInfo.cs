namespace LDAP_Connector.Models
{
    public class AttributeInfo
    {
        public string LdapName { get; set; }
        public string ModelName { get; set; }
        public object Value { get; set; }
        public string Error { get; set; }

        public AttributeInfo() { }

        public AttributeInfo(string ldapName, string modelName, object value)
        {
            LdapName = ldapName;
            ModelName = modelName;
            Value = value;
        }
    }
}
