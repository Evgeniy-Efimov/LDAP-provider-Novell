using LDAP_Connector;
using LDAP_Connector.Configuration;
using LDAP_Connector.Helpers;
using LDAP_Connector.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

try
{
    var connectorSettings = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", true, true)
        .Build()
        .GetRequiredSection("LDAPConnector")
        .Get<ConnectorSettings>();

    IOptions<ConnectorSettings> connectorSettingsOptions = Options.Create(connectorSettings);

    var logger = LoggerFactory
        .Create(builder =>
        {
            builder.AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                .AddConsole();
        }).CreateLogger<LDAPConnector>();


    ILDAPConnector ldapConnector = new LDAPConnector(connectorSettingsOptions, logger);

    var userLogin = "testuser";
    var baseDn = connectorSettings.BaseDn;

    var adEntryDn = await ldapConnector.GetEntryDn(userLogin, baseDn);

    if (string.IsNullOrWhiteSpace(adEntryDn))
    {
        throw new Exception($"User {userLogin} not found");
    }

    var adAttributes = await ldapConnector.GetEntryAttributesBySamAccountName(userLogin, baseDn);

    var attributes = new AttributesCollection();

    foreach (var adAttribute in adAttributes)
    {
        attributes.Add(adAttribute.Name, AdAttributesHelper.GetAdAttributeObject(adAttribute));
    }

    Console.WriteLine($"User {userLogin} properties:" +
        $"\r\n    {string.Join("\r\n    ", attributes.GetValuesDictionary()
            .Select(p => $"{p.Key}: {p.Value}"))}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.Read();
