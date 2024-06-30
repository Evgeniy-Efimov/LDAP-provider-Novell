using LDAP_Connector.Models;
using LDAP_Connector.Models.Interfaces;
using Novell.Directory.Ldap;

namespace LDAP_Connector
{
    public interface ILDAPConnector
	{
        Task<string> GetEntryDn(string samAccountName, string baseDn);
        Task<LdapAttributeSet> GetEntryAttributesBySamAccountName(string samAccountName, string baseDn);
        Task<LdapAttributeSet> GetEntryAttributesByDn(string dn, string baseDn);
        Task<AdGroup> GetGroup(string samAccountName, string baseDn);
        Task<AdUser> GetUserBySamAccountName(string samAccountName, string baseDn);
        Task<AdUser> GetUserByDn(string dn, string baseDn);
        Task<IDictionary<string, string>> UpdateGroup(string dn, string samAccountName, IAttributesCollection attributes, string newParentDn, string baseDn);
        Task<IDictionary<string, string>> UpdateUser(string dn, string smtpEmailDomain, IAttributesCollection attributes, string newParentDn, string baseDn);
        Task<IDictionary<string, string>> ArchiveGroup(string dn, string samAccountName, string archivedParentDn, string baseDn);
        Task<IDictionary<string, string>> ArchiveUser(string dn, string samAccountName, IAttributesCollection attributes, string archivedParentDn, string baseDn);
        Task CreateGroup(string cn, string displayName, string samAccountName, string managerDn, string parentDn, string baseDn);
        Task<string> CreateUser(IAttributesCollection attributes, string defaultPassword, string parentDn, string baseDn);
        Task AddUserToGroup(string groupSamAcountName, string groupDn, string userSamAcountName, string userDn, string baseDn);
        Task RemoveUserFromGroup(string groupSamAcountName, string groupDn, string userSamAcountName, string userDn, string baseDn);
        Task<IDictionary<string, Tuple<string, bool>>> SyncUsersGroups(string dn, string userSamAccountName, List<string> targetGroupsDns, string baseDn);
    }
}
