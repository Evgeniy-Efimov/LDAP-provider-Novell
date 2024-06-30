using LDAP_Connector.Helpers;
using LDAP_Connector.Infrastructure;
using LDAP_Connector.Models.Interfaces;

namespace LDAP_Connector.Models
{
    public class AttributesCollection : IAttributesCollection
    {
        private SafeDictionary<string, AttributeInfo> dictionary = new SafeDictionary<string, AttributeInfo>();

        public AttributesCollection() { }

        public AttributesCollection(IEnumerable<AttributeInfo> attributes)
        {
            foreach (var attribute in attributes)
            {
                dictionary.Add(attribute.LdapName, attribute);
            }
        }

        public IDictionary<string, object> GetValuesDictionary()
        {
            return dictionary.All().ToList()
                .GroupBy(a => string.IsNullOrWhiteSpace(a.Value?.ModelName)
                    ? $"{a.Key}" : $"{a.Key} ({a.Value.ModelName})")
                .ToDictionary(k => k.Key, v => v.First().Value.Value);
        }

        public IDictionary<string, string> GetErrorsDictionary()
        {
            return dictionary.All().Where(a => !string.IsNullOrWhiteSpace(a.Value.Error)).ToList()
                .GroupBy(a => string.IsNullOrWhiteSpace(a.Value?.ModelName)
                    ? $"{a.Key}" : $"{a.Key} ({a.Value.ModelName})")
                .ToDictionary(k => k.Key, v => v.First().Value.Error);
        }

        public IEnumerable<AttributeInfo> All()
        {
            return dictionary.All().Values.ToList();
        }

        public void Add(string ldapName, object value, string modelName = "")
        {
            dictionary.Add(ldapName, new AttributeInfo(ldapName, modelName, value));
        }

        public void Add(AttributeInfo attributeInfo)
        {
            dictionary.Add(attributeInfo.LdapName, attributeInfo);
        }

        public void Remove(string ldapName)
        {
            dictionary.Remove(ldapName);
        }

        public void RemoveEmpty()
        {
            foreach (var attribute in dictionary.All().Select(a => a.Value).ToList())
            {
                try
                {
                    if (attribute.Value == null
                        || (attribute.Value.GetType() == typeof(string) && string.IsNullOrWhiteSpace(attribute.Value.ToString()))
                        || (attribute.Value.GetType().IsArray && ((Array)attribute.Value).Length == 0))
                    {
                        Remove(attribute.LdapName);
                    }
                }
                catch { }
            }
        }

        public AttributeInfo Get(string ldapName)
        {
            return dictionary.Get(ldapName);
        }

        public IEnumerable<string> GetLdapNames()
        {
            return dictionary.GetKeys().ToList();
        }

        public bool Contains(AttributeInfo attributeInfo)
        {
            return dictionary.ContainsKey(attributeInfo.LdapName);
        }

        public bool Contains(string ldapName)
        {
            return dictionary.ContainsKey(ldapName);
        }

        public IAttributesCollection GetModifications(IAttributesCollection newValues)
        {
            var modifications = new AttributesCollection();
            var currentValues = dictionary.All();

            foreach (var currentValueAttribute in currentValues)
            {
                if (newValues.Contains(currentValueAttribute.Value))
                {
                    var currentValue = currentValueAttribute.Value.Value;
                    var newValue = newValues.Get(currentValueAttribute.Key).Value;

                    if (newValue != null)
                    {
                        if (newValue.GetType().IsArray)
                        {
                            try
                            {
                                var newValueArray = ((object[])newValue).Cast<string>().ToArray();
                                var currentValueArray = new string[] { };

                                if (currentValue != null && currentValue.GetType().IsArrayOf<string>())
                                {
                                    currentValueArray = ((object[])currentValue).Cast<string>().ToArray();
                                }

                                if (Enumerable.SequenceEqual(currentValueArray, newValueArray) == false)
                                {
                                    modifications.Add(newValues.Get(currentValueAttribute.Key));
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to compare old and new values ​​of array property " +
                                    $"\"{currentValueAttribute.Key}\": {ex.GetFullErrorMessage()}");
                            }
                        }
                        else if (newValue.GetType().IsDictionary())
                        {
                            continue;
                        }
                        else
                        {
                            if (currentValue == null)
                            {
                                currentValue = string.Empty;
                            }

                            if (newValue.Equals(currentValue) == false)
                            {
                                modifications.Add(newValues.Get(currentValueAttribute.Key));
                            }
                        }
                    }
                }
            }

            foreach (var missingLdapName in newValues.GetLdapNames().Where(n => !currentValues.ContainsKey(n)))
            {
                modifications.Add(newValues.Get(missingLdapName));
            }

            return modifications;
        }
    }
}
