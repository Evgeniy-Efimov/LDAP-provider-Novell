using LDAP_Connector.Infrastructure;

namespace LDAP_Connector
{
	public interface ILDAPConnector
	{
		ILDAPConnector Connect();
		void CreateAdGroup(string samAccountName, string displayName, string adGroupParentDn, string domainDn);
		void UpdateAdGroup(string samAccountName, AttributesDictionary<string, object> attributes, string currentAdGroupDn, string domainDn, string newAdGroupParentDn);
		void ArchiveAdGroup(string samAccountName, string currentAdGroupDn, string archivedParentDn, string domainDn);
		void CreateUser(string samAccountName, string displayName, int userAccountControl, string userParentDn, string domainDn, AttributesDictionary<string, object> attributes = null);
		void UpdateUser(string samAccountName, AttributesDictionary<string, object> attributes, string currentUserDn, string newUserParentDn, string smtpEmailDomain, string domainDn);
		void ArchiveUser(string samAccountName, string currentUserDn, string archivedParentDn, string domainDn, AttributesDictionary<string, object> modifications = null);
		void AddUserInAdGroup(string adGroupSamAccountName, string adGroupDn, string userDn, string domainDn);
		void RemoveUserFromAdGroup(string adGroupSamAccountName, string adGroupDn, string userDn, string domainDn);
		void UpdateAdGroupMembers(List<string> membersDns, string adGroupDn);
	}
}