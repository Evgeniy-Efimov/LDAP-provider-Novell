using LDAP_Connector.Configuration;
using LDAP_Connector.Helpers;
using LDAP_Connector.Models;
using LDAP_Connector.Models.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;
using System.Text;

namespace LDAP_Connector
{
    public class LDAPConnector : ILDAPConnector
    {
        private readonly ConnectorSettings _connectorSettings;
        private readonly ILogger<LDAPConnector> _logger;

        private string[] ManagedAttributes { get { return _connectorSettings.ManagedAttributes; } }
        private string[] NotUpdatableAttributes { get { return _connectorSettings.NotUpdatableAttributes; } }
        private string[] UserObjectClass { get { return _connectorSettings.Users.ObjectClassAttributeValue; } }
        private string[] GroupObjectClass { get { return _connectorSettings.Groups.ObjectClassAttributeValue; } }

        private const string UnwillingToPerformError = "Unwilling To Perform";
        private const string EntryAlreadyExistsError = "Entry Already Exists";
        private const string InvalidAttributeValueError = "Invalid Attribute Syntax";
        private const string AttributeAlreadyRemovedError = "No Such Attribute";

        public LDAPConnector(IOptions<ConnectorSettings> connectorSettings, ILogger<LDAPConnector> logger)
        {
            _connectorSettings = connectorSettings.Value;
            _logger = logger;
        }

        public async Task CreateGroup(string cn, string displayName, string samAccountName,
            string managerDn, string parentDn, string baseDn)
        {
            try
            {
                var groupDn = GetDn(cn, parentDn, baseDn);

                var attributeSet = new LdapAttributeSet
                {
                    GetLdapAttribute("objectClass", GroupObjectClass),
                    GetLdapAttribute("displayName", displayName),
                    GetLdapAttribute("description", cn),
                    GetLdapAttribute("cn", cn),
                    GetLdapAttribute("samAccountName", samAccountName),
                    GetLdapAttribute("managedBy", managerDn)
                };

                var groupEntry = new LdapEntry(groupDn, attributeSet);

                await GetConnection().AddAsync(groupEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while create group\r\n");

                throw;
            }
        }

        public async Task<string> CreateUser(IAttributesCollection attributes, string defaultPassword, string parentDn, string baseDn)
        {
            var attributeSet = new LdapAttributeSet();

            try
            {
                var displayName = attributes.Get("displayName")?.Value?.ToString();
                var samAccountName = attributes.Get("samAccountName")?.Value?.ToString();
                var userAccountControl = attributes.Get("userAccountControl")?.Value?.ToString();
                var userPrincipalName = attributes.Get("userPrincipalName")?.Value?.ToString();
                var userDn = GetDn(cn: displayName, parentDn, baseDn);

                attributeSet.Add(GetLdapAttribute("objectClass", UserObjectClass));
                attributeSet.Add(GetLdapAttribute("samAccountName", samAccountName));
                attributeSet.Add(GetLdapAttribute("userPrincipalName", userPrincipalName));
                attributeSet.Add(GetLdapAttribute("userAccountControl", userAccountControl));
                attributeSet.Add(new LdapAttribute("unicodePwd",
                    Encoding.Unicode.GetBytes($"\"{defaultPassword}\"")));
                attributeSet.Add(GetLdapAttribute("distinguishedName", userDn));

                var userEntry = new LdapEntry(userDn, attributeSet);

                await GetConnection().AddAsync(userEntry);

                return userDn;
            }
            catch (Exception ex)
            {
                var attributesLog = attributeSet.Select(x => $"{x.Key}: {x.Value}");
                _logger.LogError(ex, $"Error while creating user with attributes:\r\n" +
                    $"{string.Join(Environment.NewLine + "   ", attributesLog)}");

                throw;
            }
        }

        public async Task<IDictionary<string, string>> UpdateUser(string dn, string smtpEmailDomain,
            IAttributesCollection attributes, string newParentDn, string baseDn)
        {
            try
            {
                var samAccountName = attributes.Get("samAccountName")?.Value?.ToString();

                AdAttributesHelper.TryGetAdAttributeString(
                    await WithCheckConnection().GetEntryAttributesBySamAccountName(samAccountName, baseDn), "cn",
                    out var currentCnAttribute);

                UpdateCnDn(currentCnAttribute, newCn: attributes.Get("cn")?.Value?.ToString(), ref dn, newParentDn, baseDn);

                AdAttributesHelper.TryGetAdAttributeStringArray(
                    await WithCheckConnection().GetEntryAttributesBySamAccountName(samAccountName, baseDn), "proxyAddresses",
                    out var currentProxyAddresses);

                UpdateUserProxyAddresses(samAccountName, currentProxyAddresses, smtpEmailDomain, attributes);

                foreach (var notUpdatableAttribute in attributes.GetLdapNames().Intersect(NotUpdatableAttributes).ToList())
                {
                    attributes.Remove(notUpdatableAttribute);
                }

                foreach (var notManagedAttribute in attributes.GetLdapNames().Where(k => !ManagedAttributes.Contains(k)))
                {
                    attributes.Remove(notManagedAttribute);
                }

                foreach (var modification in attributes.All())
                {
                    var errorMessage = await ModifyEntryAttribute(dn,
                        new LdapModification(LdapModification.Replace, GetLdapAttribute(modification.LdapName, modification.Value)));

                    modification.Error = errorMessage;
                }

                return attributes.GetErrorsDictionary();
            }
            catch (Exception generalEx)
            {
                var attributesLog = attributes?.GetValuesDictionary()?.Select(x => $"{x.Key}: {x.Value}");
                _logger.LogError(generalEx, $"Error while updating user with attributes:\r\n" +
                    $"{string.Join(Environment.NewLine + "   ", attributesLog ?? new string[] { })}");

                throw;
            }
        }

        public async Task<IDictionary<string, string>> UpdateGroup(string dn, string samAccountName,
            IAttributesCollection attributes, string newParentDn, string baseDn)
        {
            try
            {
                AdAttributesHelper.TryGetAdAttributeString(
                    await WithCheckConnection().GetEntryAttributesBySamAccountName(samAccountName, baseDn), "cn",
                    out var currentCnAttribute);

                UpdateCnDn(currentCnAttribute, newCn: attributes.Get("cn")?.Value?.ToString(), ref dn, newParentDn, baseDn);

                foreach (var notUpdatableAttribute in attributes.GetLdapNames().Intersect(NotUpdatableAttributes).ToList())
                {
                    attributes.Remove(notUpdatableAttribute);
                }

                foreach (var notManagedAttribute in attributes.GetLdapNames().Where(k => !ManagedAttributes.Contains(k)))
                {
                    attributes.Remove(notManagedAttribute);
                }

                foreach (var modification in attributes.All())
                {
                    var errorMessage = await ModifyEntryAttribute(dn,
                        new LdapModification(LdapModification.Replace, GetLdapAttribute(modification.LdapName, modification.Value)));

                    modification.Error = errorMessage;
                }

                return attributes.GetErrorsDictionary();
            }
            catch (Exception generalEx)
            {
                var attributesLog = attributes?.GetValuesDictionary()?.Select(x => $"{x.Key}: {x.Value}");
                _logger.LogError(generalEx, $"Error while updating group with attributes:\r\n" +
                    $"{string.Join(Environment.NewLine + "   ", attributesLog ?? new string[] { })}");

                throw;
            }
        }

        public async Task<IDictionary<string, string>> ArchiveUser(string dn, string samAccountName,
            IAttributesCollection attributes, string archivedParentDn, string baseDn)
        {
            try
            {
                AdAttributesHelper.TryGetAdAttributeStringArray(
                    await WithCheckConnection().GetEntryAttributesBySamAccountName(samAccountName, baseDn), "memberOf",
                    out var userGroupsDns);

                foreach (var userGroupDn in (userGroupsDns ?? new string[] { }).ToList())
                {
                    AdAttributesHelper.TryGetAdAttributeString(
                        await WithCheckConnection().GetEntryAttributesByDn(userGroupDn, baseDn), "samAccountName",
                        out var groupSamAccountName);

                    await RemoveUserFromGroup(groupSamAccountName, userGroupDn, samAccountName, dn, baseDn);
                }

                foreach (var notUpdatableAttribute in attributes.GetLdapNames().Intersect(NotUpdatableAttributes).ToList())
                {
                    attributes.Remove(notUpdatableAttribute);
                }

                foreach (var notManagedAttribute in attributes.GetLdapNames().Where(k => !ManagedAttributes.Contains(k)))
                {
                    attributes.Remove(notManagedAttribute);
                }

                foreach (var modification in attributes.All())
                {
                    var errorMessage = await ModifyEntryAttribute(dn,
                        new LdapModification(LdapModification.Replace, GetLdapAttribute(modification.LdapName, modification.Value)));

                    modification.Error = errorMessage;
                }

                AdAttributesHelper.TryGetAdAttributeString(
                    await WithCheckConnection().GetEntryAttributesBySamAccountName(samAccountName, baseDn), "cn",
                    out var currentCnAttribute);

                UpdateCnDn(currentCnAttribute, newCn: currentCnAttribute, ref dn, archivedParentDn, baseDn);

                return attributes.GetErrorsDictionary();
            }
            catch (Exception generalEx)
            {
                _logger.LogError(generalEx, $"Error while archiving user {dn}\r\n");

                throw;
            }
        }

        public async Task<IDictionary<string, string>> ArchiveGroup(string dn, string samAccountName,
            string archivedParentDn, string baseDn)
        {
            try
            {
                LdapModification[] modifications = { new(LdapModification.Delete, new LdapAttribute("member")) };

                await GetConnection().ModifyAsync(dn, modifications);

                AdAttributesHelper.TryGetAdAttributeString(
                    await WithCheckConnection().GetEntryAttributesBySamAccountName(samAccountName, baseDn), "cn",
                    out var currentCnAttribute);

                UpdateCnDn(currentCnAttribute, newCn: currentCnAttribute, ref dn, archivedParentDn, baseDn);

                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while archiving group {dn}\r\n");

                throw;
            }
        }

        public async Task RemoveUserFromGroup(string groupSamAcountName, string groupDn, string userSamAcountName,
            string userDn, string baseDn)
        {
            try
            {
                LdapModification[] modifications = { new(LdapModification.Delete, GetLdapAttribute("member", userDn)) };

                await GetConnection().ModifyAsync(groupDn, modifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while removing user from group\r\n");

                if (ex.Message.NormalizeString().Contains(UnwillingToPerformError.NormalizeString()))
                {
                    throw new Exception($"User {userSamAcountName} is not member of {groupSamAcountName}");
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<IDictionary<string, Tuple<string, bool>>> SyncUsersGroups(string dn, string userSamAccountName,
            List<string> targetGroupsDns, string baseDn)
        {
            try
            {
                var result = new List<Tuple<string, string, bool>>();

                AdAttributesHelper.TryGetAdAttributeStringArray(
                    await WithCheckConnection().GetEntryAttributesByDn(dn, baseDn), "memberOf",
                    out var usersGroupsDns);

                var groupName = string.Empty;
                var message = string.Empty;
                var isSuccess = false;

                foreach (var groupDn in usersGroupsDns.Union(targetGroupsDns ?? new List<string>()).Distinct())
                {
                    groupName = groupDn.GetSubstring(new string[] { "CN=" }, new string[] { ",OU=", ",CN=", ",DC=" });
                    message = string.Empty;
                    isSuccess = false;

                    try
                    {
                        if (targetGroupsDns.Contains(groupDn) && usersGroupsDns.Contains(groupDn))
                        {
                            message = "Already synced";
                            isSuccess = true;
                        }
                        else if (!targetGroupsDns.Contains(groupDn))
                        {
                            await RemoveUserFromGroup(groupName, groupDn, userSamAccountName, dn, baseDn);
                            message = "Has been removed";
                            isSuccess = true;
                        }
                        else if (!usersGroupsDns.Contains(groupDn))
                        {
                            await AddUserToGroup(groupName, groupDn, userSamAccountName, dn, baseDn);
                            message = "Has been added";
                            isSuccess = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        message = $"Error: {ex.GetFullErrorMessage()}";
                        isSuccess = false;
                    }

                    result.Add(new Tuple<string, string, bool>(groupName, message, isSuccess));
                }

                return result.GroupBy(g => g.Item1).ToDictionary(
                    k => k.Key,
                    v => new Tuple<string, bool>(
                        v.FirstOrDefault()?.Item2 ?? string.Empty,
                        v.FirstOrDefault()?.Item3 ?? false));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while synchronizing user groups\r\n");

                throw;
            }
        }

        public async Task AddUserToGroup(string groupSamAcountName, string groupDn, string userSamAcountName,
            string userDn, string baseDn)
        {
            try
            {
                LdapModification[] modifications = { new(LdapModification.Add, GetLdapAttribute("member", userDn)) };

                await GetConnection().ModifyAsync(groupDn, modifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding user to group\r\n");

                if (ex.Message.NormalizeString().Contains(EntryAlreadyExistsError.NormalizeString()))
                {
                    throw new Exception($"User {userSamAcountName} already has been added in {groupSamAcountName}");
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<LdapAttributeSet> GetEntryAttributesBySamAccountName(string samAccountName, string baseDn)
        {
            try
            {
                var searchResults = await GetConnection().SearchAsync(baseDn, LdapConnection.ScopeSub,
                    $"samAccountName={samAccountName}", ManagedAttributes, false);

                await foreach (var result in searchResults)
                {
                    try
                    {
                        var attributes = result.GetAttributeSet();

                        return attributes;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error while parsing data from AD: {ex.Message}\r\n");

                        throw;
                    }
                }

                return null;

            }
            catch (Exception generalEx)
            {
                _logger.LogError($"Error while parsing data from AD: {generalEx.Message}\r\n");

                throw;
            }
        }

        public async Task<LdapAttributeSet> GetEntryAttributesByDn(string dn, string baseDn)
        {
            try
            {
                var searchResults = await GetConnection().SearchAsync(baseDn, LdapConnection.ScopeSub,
                    $"distinguishedName={dn.FormatDnToRead()}", ManagedAttributes, false);

                await foreach (var result in searchResults)
                {
                    try
                    {
                        var attributes = result.GetAttributeSet();

                        return attributes;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error while parsing data from AD: {ex.Message}\r\n");

                        throw;
                    }
                }

                return null;

            }
            catch (Exception generalEx)
            {
                _logger.LogError($"Error while parsing data from AD: {generalEx.Message}\r\n");

                throw;
            }
        }

        public async Task<AdGroup> GetGroup(string samAccountName, string baseDn)
        {
            try
            {
                return AdAttributesHelper.GetModelFromAdAttributes<AdGroup>(
                    await WithCheckConnection().GetEntryAttributesBySamAccountName(samAccountName, baseDn));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while get group\r\n");

                throw;
            }
        }

        public async Task<AdUser> GetUserBySamAccountName(string samAccountName, string baseDn)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(samAccountName))
                {
                    return AdAttributesHelper.GetModelFromAdAttributes<AdUser>(
                        await WithCheckConnection().GetEntryAttributesBySamAccountName(samAccountName, baseDn));
                }

                throw new Exception("SamAccountName is empty");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while get user\r\n");

                throw;
            }
        }

        public async Task<AdUser> GetUserByDn(string dn, string baseDn)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dn))
                {
                    return AdAttributesHelper.GetModelFromAdAttributes<AdUser>(
                        await WithCheckConnection().GetEntryAttributesByDn(dn, baseDn));
                }

                throw new Exception("Dn is empty");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while get user\r\n");

                throw;
            }
        }

        public async Task<string> GetEntryDn(string samAccountName, string baseDn)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(samAccountName))
                {
                    return AdAttributesHelper.TryGetAdAttributeString(
                        await WithCheckConnection().GetEntryAttributesBySamAccountName(samAccountName, baseDn),
                            "distinguishedName", out var entryDn)
                        ? entryDn : string.Empty;
                }

                throw new Exception("SamAccountName is empty");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while get entry Dn\r\n");

                throw;
            }
        }

        private ILDAPConnector WithCheckConnection()
        {
            GetConnection();

            return this;
        }

        private LdapConnection GetConnection()
        {
            return AdConnectionHelper.GetConnection(_logger,
                _connectorSettings.Host,
                _connectorSettings.Port,
                _connectorSettings.Login,
                _connectorSettings.Password,
                _connectorSettings.IsValidateCertificate ?? true);
        }

        private string GetParentDn(string parentDn, string baseDn)
        {
            return string.Join(",", new string[] { parentDn, baseDn });
        }

        private string GetDn(string cn, string parentDn, string baseDn)
        {
            return string.Join(",", new string[] { "CN=" + cn.FormatCnToWrite(), parentDn, baseDn });
        }

        private LdapAttribute GetLdapAttribute(string name, object value)
        {
            if (value != null && value.GetType().IsArray)
            {
                string[] arrayOfValues = ((object[])value).Cast<string>().ToArray();
                return new LdapAttribute(name, arrayOfValues);
            }
            else
            {
                return new LdapAttribute(name, value?.ToString() ?? string.Empty);
            }
        }

        private string UpdateCnDn(string currentCn, string newCn, ref string currentDn, string newParentDn, string baseDn)
        {
            if (!string.IsNullOrWhiteSpace(newCn))
            {
                try
                {
                    newCn = newCn.FormatCnToWrite();

                    if (string.IsNullOrWhiteSpace(newCn))
                        return currentCn;

                    var newEntryDn = GetDn(newCn, newParentDn, baseDn);

                    if (currentDn.NormalizeString() != newEntryDn.NormalizeString())
                    {
                        GetConnection().RenameAsync(
                            dn: currentDn,
                            newRdn: $"CN={newCn}",
                            newParentdn: GetParentDn(newParentDn, baseDn),
                            deleteOldRdn: true
                            ).GetAwaiter().GetResult();

                        currentDn = newEntryDn;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while updating user CN: {ex.Message}");
                }
            }

            return currentCn;
        }

        private void UpdateUserProxyAddresses(string samAccountName, string[] currentProxyAddresses,
            string smtpEmailDomain, IAttributesCollection attributes)
        {
            try
            {
                var emailAddress = attributes.Get("mail")?.Value?.ToString().NormalizeString();

                if (string.IsNullOrWhiteSpace(emailAddress))
                    return;

                var proxyAddresses = currentProxyAddresses?.ToList() ?? new List<string>();
                
                //TODO: update list

                attributes.Add("proxyAddresses", proxyAddresses.Distinct().OrderBy(pa => pa).ToArray());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while updating user proxyAddresses: {ex.Message}");
            }
        }

        private async Task<string> ModifyEntryAttribute(string entryDn, LdapModification modification)
        {
            var errorMessage = string.Empty;

            try
            {
                await GetConnection().ModifyAsync(entryDn, modification);
            }
            catch (Exception ex)
            {
                if (modification.Attribute == null)
                {
                    errorMessage = "Error while updating attribute, attribute is null";
                    _logger.LogError(ex, $"{errorMessage}\r\n");
                }
                //Try delete empty value if empty string is not allowed
                else if (ex.Message.NormalizeString().Contains(InvalidAttributeValueError.NormalizeString())
                    && string.IsNullOrWhiteSpace(modification.Attribute.StringValue))
                {
                    try
                    {
                        await GetConnection().ModifyAsync(entryDn,
                            new LdapModification(LdapModification.Delete, new LdapAttribute(modification.Attribute.Name)));
                    }
                    catch (Exception deleteEx)
                    {
                        //Check attribute has been already removed
                        if (!deleteEx.Message.NormalizeString().Contains(AttributeAlreadyRemovedError.NormalizeString()))
                        {
                            errorMessage = deleteEx.GetFullErrorMessage();
                            _logger.LogError(deleteEx, $"Error while removing attribute value, " +
                                $"{modification.Attribute.Name ?? string.Empty}: {modification.Attribute.StringValue ?? string.Empty}\r\n");
                        }
                    }
                }
                else
                {
                    errorMessage = ex.GetFullErrorMessage();
                    _logger.LogError(ex, $"Error while updating attribute " +
                        $"{modification.Attribute.Name ?? string.Empty}: {modification.Attribute.StringValue ?? string.Empty}\r\n");
                }
            }

            return errorMessage;
        }
    }
}
