using System.ComponentModel;
using System.Reflection;

namespace LDAP_Connector.Helpers
{
    public static class ReflectionHelper
    {
        public static string GetPropertyDisplayName(this PropertyInfo property)
        {
            return (property.GetCustomAttributes(typeof(DisplayNameAttribute), false).FirstOrDefault() as DisplayNameAttribute)?.DisplayName ?? property.Name;
        }

        public static string GetPropertyName(this PropertyInfo property)
        {
            return property.Name;
        }

        public static object GetPropertyValue(this object model, string propertyName)
        {
            return model.GetType().GetProperties().FirstOrDefault(p => p.GetPropertyDisplayName() == propertyName)?.GetValue(model);
        }

        public static void SetPropertyValue<TModel>(this PropertyInfo property, TModel model, object value)
        {
            property.SetValue(model, value);
        }

        public static bool IsArrayOf<T>(this Type type)
        {
            return type == typeof(T[]);
        }

        public static bool IsDictionary(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }
    }
}
