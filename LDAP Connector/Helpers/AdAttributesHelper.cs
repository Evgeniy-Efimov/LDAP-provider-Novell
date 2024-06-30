using LDAP_Connector.Models;
using Novell.Directory.Ldap;
using System.ComponentModel.DataAnnotations.Schema;

namespace LDAP_Connector.Helpers
{
    public static class AdAttributesHelper
    {
        public const string AdditionalAttributesName = "additionalProperties";

        public static bool TryGetAdAttributeString(LdapAttributeSet result, string propertyName, out string value)
        {
            value = string.Empty;

            if (result == null)
            {
                return false;
            }

            if (!result.TryGetValue(propertyName, out var attribute)) return false;

            value = attribute.StringValue;
            return true;
        }

        public static bool TryGetAdAttributeStringArray(LdapAttributeSet result, string propertyName, out string[] value)
        {
            value = Array.Empty<string>();

            if (result == null)
            {
                return false;
            }

            if (!result.TryGetValue(propertyName, out var attribute)) return false;

            value = attribute.StringValueArray;

            return true;
        }

        public static bool TryGetAdAttributeByteArray(LdapAttributeSet result, string propertyName, out byte[] value)
        {
            value = Array.Empty<byte>();

            if (result == null)
            {
                return false;
            }

            if (!result.TryGetValue(propertyName, out var attribute)) return false;

            value = attribute.ByteValue;

            return true;
        }

        public static object GetAdAttributeObject(LdapAttribute ldapAttribute)
        {
            if (ldapAttribute == null)
                return null;

            var arrayValue = ldapAttribute.StringValueArray;
            var byteArrayValue = ldapAttribute.ByteValue;

            if (byteArrayValue != null && ldapAttribute.Name == "objectSid")
            {
                return byteArrayValue.ToSidString();
            }
            else if (arrayValue != null && arrayValue.Any())
            {
                return arrayValue.Count() == 1
                    ? arrayValue.First()
                    : arrayValue;
            }

            return ldapAttribute.StringValue;
        }

        public static DateTimeOffset GetDateTimeFromTicks(long ticks)
        {
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        public static TModel GetModelFromAdAttributes<TModel>(LdapAttributeSet ldapAttributes) where TModel : new()
        {
            if (ldapAttributes == null)
                return default(TModel);

            var model = new TModel();
            var processedProperties = new List<string>();

            foreach (var property in typeof(TModel).GetProperties())
            {
                if (property.GetCustomAttributes(typeof(NotMappedAttribute), true).Count() > 0)
                    continue;

                var propertyName = property.GetPropertyDisplayName();

                if (!string.IsNullOrWhiteSpace(propertyName))
                {
                    if (property.PropertyType.IsArrayOf<byte>())
                    {
                        if (TryGetAdAttributeByteArray(ldapAttributes, propertyName, out var value))
                        {
                            property.SetPropertyValue(model, value);
                        }
                    }
                    else if (property.PropertyType.IsArray)
                    {
                        if (TryGetAdAttributeStringArray(ldapAttributes, propertyName, out var value))
                        {
                            property.SetPropertyValue(model, value);
                        }
                    }
                    else
                    {
                        if (TryGetAdAttributeString(ldapAttributes, propertyName, out var value))
                        {
                            property.SetPropertyValue(model, value);
                        }
                    }

                    processedProperties.Add(propertyName);
                }
            }

            var additionalAttributesProperty = typeof(TModel).GetProperties()
                .FirstOrDefault(p => p.GetPropertyDisplayName() == AdditionalAttributesName);

            if (additionalAttributesProperty != null)
            {
                var additionalAttributes = new Dictionary<string, object>();

                foreach (var additionalAttribute in ldapAttributes.Where(a => !processedProperties.Contains(a.Value.Name)))
                {
                    additionalAttributes.Add(additionalAttribute.Value.Name, GetAdAttributeObject(additionalAttribute.Value));
                }

                additionalAttributesProperty.SetPropertyValue(model, additionalAttributes);
            }

            return model;
        }

        public static AttributesCollection GetAdAttributesFromModel<TModel>(TModel model) where TModel : new()
        {
            if (model == null) model = new TModel();

            var adAttributes = new AttributesCollection();
            var processedProperties = new List<string>();

            foreach (var property in typeof(TModel).GetProperties())
            {
                if (property.GetCustomAttributes(typeof(NotMappedAttribute), true).Count() > 0)
                    continue;

                var propertyDisplayName = property.GetPropertyDisplayName();
                var propertyName = property.GetPropertyName();

                if (!string.IsNullOrWhiteSpace(propertyDisplayName))
                {
                    adAttributes.Add(propertyDisplayName, model.GetPropertyValue(propertyDisplayName), propertyName);
                    processedProperties.Add(propertyDisplayName);
                }
            }

            var additionalAttributesProperty = typeof(TModel).GetProperties()
                .FirstOrDefault(p => p.GetPropertyDisplayName() == AdditionalAttributesName);

            if (additionalAttributesProperty != null)
            {
                var additionalAttributes = (IDictionary<string, object>)additionalAttributesProperty.GetValue(model);

                if (additionalAttributes != null)
                {
                    foreach (var additionalAttribute in additionalAttributes)
                    {
                        if (!processedProperties.Contains(additionalAttribute.Key))
                        {
                            adAttributes.Add(additionalAttribute.Key, additionalAttribute.Value);
                        }
                    }
                }
            }

            return adAttributes;
        }
    }
}
