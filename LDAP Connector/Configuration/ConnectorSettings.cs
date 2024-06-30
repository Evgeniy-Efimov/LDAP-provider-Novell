namespace LDAP_Connector.Configuration
{
    public class ConnectorSettings
    {
        public bool? IsValidateCertificate { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string BaseDn { get; set; }
        public AdGroupsSettings Groups { get; set; }
        public AdUserSettings Users { get; set; }
        public string[] ManagedAttributes { get; set; }
        public string[] NotUpdatableAttributes { get; set; }
    }

    public class AdGroupsSettings
    {
        public string[] ObjectClassAttributeValue { get; set; }
    }

    public class AdUserSettings
    {
        public string[] ObjectClassAttributeValue { get; set; }
    }
}
