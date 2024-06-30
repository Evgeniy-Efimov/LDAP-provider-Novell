namespace LDAP_Connector.Models.Interfaces
{
    public interface IAttributesCollection
    {
        public IDictionary<string, object> GetValuesDictionary();

        public IDictionary<string, string> GetErrorsDictionary();

        public IEnumerable<AttributeInfo> All();

        public void Add(string ldapName, object value, string modelName = "");

        public void Add(AttributeInfo attributeInfo);

        public void Remove(string ldapName);

        public void RemoveEmpty();

        public AttributeInfo Get(string ldapName);

        public IEnumerable<string> GetLdapNames();

        public bool Contains(AttributeInfo attributeInfo);

        public bool Contains(string ldapName);

        public IAttributesCollection GetModifications(IAttributesCollection newValues);
    }
}
