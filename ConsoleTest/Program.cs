using LDAP_Connector;

ILDAPConnector LDAPConnector = new LDAPConnector("user", "password", "host", 123, new TimeSpan(0, 0, 5));

LDAPConnector.Connect().CreateAdGroup("Test_NewGroup", "NewTestGroup", "OU=Security groups,OU=Global", "DC=my,DC=domain,DC=test");

Console.WriteLine("New group has been created");
Console.ReadLine();