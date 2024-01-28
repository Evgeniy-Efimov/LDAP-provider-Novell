using LDAP_Connector.Helpers;
using LDAP_Connector.Infrastructure;
using Novell.Directory.Ldap;
using System.Globalization;

namespace LDAP_Connector
{
	public class LDAPConnector : ILDAPConnector
	{
		#region Connection parameters

		private LdapConnection Connection { get; set; }
		private string UserName { get; set; }
		private string Password { get; set; }
		private string ConnectionHost { get; set; }
		private int ConnectionPort { get; set; }
		private bool IsSecureSocketLayer { get; set; }
		private bool IsReferralFollowing { get; set; }
		private TimeSpan ConnectionTimeout { get; set; }

		#endregion

		#region Constants

		private readonly string[] managedAttributes = new string[38]
		{
			"cn",
			"distinguishedName",
			"samAccountName",
			"sn",
			"objectClass",
			"userAccountControl",
			"givenName",
			"displayName",
			"userPrincipalName",
			"whenCreated",
			"whenChanged",
			"description",
			"targetAddress",
			"proxyAddresses",
			"l",
			"name",
			"mail",
			"mobile",
			"telephoneNumber",
			"objectSid",
			"manager",
			"company",
			"member",
			"lockoutTime",
			"accountExpires",
			"co",
			"st",
			"streetAddress",
			"postalCode",
			"pmiIMDLAttribute8",
			"extensionAttribute9",
			"employeeType",
			"memberOf",
			"title",
			"extensionAttribute1",
			"pmiIMDLAttribute1",
			"pmiIMDLExtensionAttribute8",
			"physicalDeliveryOfficeName"
		};

		private readonly string[] notUpdatedAttributes = new string[2]
		{
			"cn",
			"name"
		};

		public static readonly int UF_ACCOUNTDISABLE = 2;
		public static readonly int UF_PASSWD_CANT_CHANGE = 64;
		public static readonly int UF_NORMAL_ACCOUNT = 512;
		public static readonly int UF_DONT_EXPIRE_PASSWORD = 65536;

		private const int DateTimeToAdTicksOffsetInYears = 1600;

		#endregion

		public LDAPConnector(string userName, string password,
			string connectionHost, int connectionPort, TimeSpan connectionTimeout,
			bool isSecureSocketLayer = true, bool isReferralFollowing = true)
		{
			Connection = new LdapConnection();
			UserName = userName;
			Password = password;
			ConnectionHost = connectionHost;
			ConnectionPort = connectionPort;
			IsSecureSocketLayer = isSecureSocketLayer;
			IsReferralFollowing = isReferralFollowing;
			ConnectionTimeout = connectionTimeout;
		}

		public ILDAPConnector Connect()
		{
			if (Connection != null && Connection.Connected && Connection.Bound)
				return this;

			if (Connection != null)
			{
				try
				{
					Connection.Dispose();
				}
				catch { }
			}

			Connection = new LdapConnection();

			Connection.SecureSocketLayer = IsSecureSocketLayer;
			Connection.Connect(ConnectionHost, ConnectionPort);

			var connectionConstraints = new LdapSearchConstraints()
			{
				ReferralFollowing = IsReferralFollowing
			};

			Connection.Constraints = connectionConstraints;
			Connection.ConnectionTimeout = ConnectionTimeout.Milliseconds;
			Connection.Bind(UserName, Password);

			return this;
		}
		
		public void CreateAdGroup(string samAccountName, string displayName, string adGroupParentDn, string domainDn)
		{
			var groupDn = GetDn(displayName, adGroupParentDn, domainDn);

			var attributeSet = new LdapAttributeSet();

			attributeSet.Add(GetLdapAttribute("objectClass", new[] { "group" }));
			attributeSet.Add(GetLdapAttribute("displayName", displayName));
			attributeSet.Add(GetLdapAttribute("description", displayName));
			attributeSet.Add(GetLdapAttribute("cn", displayName));
			attributeSet.Add(GetLdapAttribute("samAccountName", samAccountName));

			Connection.Add(new LdapEntry(groupDn, attributeSet));
		}

		public void UpdateAdGroup(string samAccountName, AttributesDictionary<string, object> attributes, string currentAdGroupDn, string domainDn, string newAdGroupParentDn = null)
		{
			LdapModification[] attributesToUpdate = null;

			TryGetLdapAttributeAsString(GetEntryBySamAccountName(samAccountName, domainDn), "cn", 
				out var currentCn);

			UpdateCnDn(currentCn, newCn: attributes.GetValue("cn")?.ToString(), 
				ref currentAdGroupDn, newAdGroupParentDn, domainDn);

			foreach (var notUpdatedAttr in attributes.GetKeys().Intersect(notUpdatedAttributes).ToList())
			{
				attributes.Remove(notUpdatedAttr);
			}

			attributesToUpdate = attributes.GetAllAsCopy()
				.Select(attribute => new LdapModification(LdapModification.Replace, GetLdapAttribute(attribute.Key, attribute.Value)))
				.OrderByDescending(attribute => attribute.Attribute.StringValue).ToArray();

			foreach (var attributeToUpdate in attributesToUpdate)
			{
				Connection.Modify(currentAdGroupDn, attributeToUpdate);
			}
		}

		public void ArchiveAdGroup(string samAccountName, string currentAdGroupDn, string archivedParentDn, string domainDn)
		{
			LdapModification[] attributesToUpdate =
			{
				new (LdapModification.Delete, new LdapAttribute("member"))
			};

			Connection.Modify(currentAdGroupDn, attributesToUpdate);

			TryGetLdapAttributeAsString(GetEntryBySamAccountName(samAccountName, domainDn), "cn", 
				out var currentCn);

			UpdateCnDn(currentCn, newCn: currentCn, 
				ref currentAdGroupDn, archivedParentDn, domainDn);
		}

		public void CreateUser(string samAccountName,
			string displayName, int userAccountControl, string userParentDn, 
			string domainDn, AttributesDictionary<string, object> attributes = null)
		{
			var attributeSet = new LdapAttributeSet();

			var userDn = GetDn(displayName, userParentDn, domainDn);

			attributeSet.Add(GetLdapAttribute("objectClass", new[] { "user" }));
			attributeSet.Add(GetLdapAttribute("samAccountName", samAccountName));
			attributeSet.Add(GetLdapAttribute("userAccountControl", userAccountControl));
			attributeSet.Add(GetLdapAttribute("distinguishedName", userDn));

			if (attributes != null && attributes.Any())
			{
				foreach (var attribute in managedAttributes
					.Where(managedAttribute => attributes.ContainsKey(managedAttribute))
					.ToDictionary(k => k, v => attributes.GetValue(v))
					.Where(managedAttribute => !string.IsNullOrEmpty(managedAttribute.Value?.ToString())))
				{
					attributeSet.Add(GetLdapAttribute(attribute.Key, attribute.Value));
				}
			}

			Connection.Add(new LdapEntry(userDn, attributeSet));
		}

		public void UpdateUser(string samAccountName, AttributesDictionary<string, object> attributes, string currentUserDn, 
			string newUserParentDn, string smtpEmailDomain, string domainDn)
		{
			LdapModification[] attributesToUpdate = null;

			TryGetLdapAttributeAsString(GetEntryBySamAccountName(samAccountName, domainDn), "cn",
				out var currentCn);

			UpdateCnDn(currentCn, newCn: attributes.GetValue("cn")?.ToString(), 
				ref currentUserDn, newUserParentDn, domainDn);

			TryGetLdapAttributeAsStringArray(GetEntryBySamAccountName(samAccountName, domainDn), "proxyAddresses",
				out var currentProxyAddresses);

			UpdateProxyAddresses(samAccountName, currentProxyAddresses, smtpEmailDomain, attributes);

			foreach (var notUpdatedAttr in attributes.GetKeys().Intersect(notUpdatedAttributes).ToList())
			{
				attributes.Remove(notUpdatedAttr);
			}

			attributesToUpdate = attributes.GetAllAsCopy()
				.Select(attribute => new LdapModification(LdapModification.Replace, GetLdapAttribute(attribute.Key, attribute.Value)))
				.OrderByDescending(attribute => attribute.Attribute.StringValue).ToArray();

			foreach (var attributeToUpdate in attributesToUpdate)
			{
				Connection.Modify(currentUserDn, attributeToUpdate);
			}
		}

		public void ArchiveUser(string samAccountName, string currentUserDn, string archivedParentDn, string domainDn, AttributesDictionary<string, object> modifications = null)
		{
			TryGetLdapAttributeAsStringArray(GetEntryBySamAccountName(samAccountName, domainDn), "memberOf",
				out var userGroupsDns);

			var userGroupsDnsList = (userGroupsDns ?? new string[] { }).ToList();

			foreach (var groupDn in userGroupsDnsList)
			{
				TryGetLdapAttributeAsString(GetEntryBySamAccountName(samAccountName, domainDn), "samAccountName",
					out var adGroupSamAccountName);

				RemoveUserFromAdGroup(adGroupSamAccountName, groupDn, currentUserDn, domainDn);
			}

			if (modifications != null && modifications.Any())
			{
				var ldapModifications = new List<LdapModification>();

				foreach (var modification in modifications.GetAllAsCopy())
				{
					ldapModifications.Add(new LdapModification(LdapModification.Replace, GetLdapAttribute(modification.Key, modification.Value)));
				}

				foreach (var modification in ldapModifications)
				{
					Connection.Modify(currentUserDn, modification);
				}
			}

			TryGetLdapAttributeAsString(GetEntryBySamAccountName(samAccountName, domainDn), "cn",
				out var currentCn);

			UpdateCnDn(currentCn, newCn: currentCn, ref currentUserDn, archivedParentDn, domainDn);
		}

		public void AddUserInAdGroup(string adGroupSamAccountName, string adGroupDn, string userDn, string domainDn)
		{
			TryGetLdapAttributeAsStringArray(GetEntryBySamAccountName(adGroupSamAccountName, domainDn), "member",
				out var membersDns);

			var membersDnsList = (membersDns ?? new string[] { }).ToList();

			if (!membersDnsList.Contains(userDn))
			{
				membersDnsList.Add(userDn);

				LdapModification[] modifications = {
					new (LdapModification.Replace, GetLdapAttribute("member", membersDnsList.ToArray())) 
				};

				Connection.Modify(adGroupDn, modifications);
			}
		}

		public void RemoveUserFromAdGroup(string adGroupSamAccountName, string adGroupDn, string userDn, string domainDn)
		{
			TryGetLdapAttributeAsStringArray(GetEntryBySamAccountName(adGroupSamAccountName, domainDn), "member",
				out var membersDns);

			var membersDnsList = (membersDns ?? new string[] { }).ToList();

			if (membersDnsList.Contains(userDn))
			{
				membersDnsList.Remove(userDn);

				LdapModification[] modifications = {
					new (LdapModification.Replace, GetLdapAttribute("member", membersDnsList.ToArray()))
				};

				Connection.Modify(adGroupDn, modifications);
			}
		}

		public void UpdateAdGroupMembers(List<string> membersDns, string adGroupDn)
		{
			LdapModification[] modifications = {
				new (LdapModification.Replace, GetLdapAttribute("member", membersDns.ToArray()))
			};

			Connection.Modify(adGroupDn, modifications);
		}

		#region Methods for LDAP attributes

		private LdapAttributeSet GetEntryBySamAccountName(string samAccountName, string domainDn)
		{
			var searchResults = Connection.Search(domainDn, LdapConnection.ScopeSub,
				$"samAccountName={samAccountName}", managedAttributes, false);

			foreach (var searchResult in searchResults)
			{
				var ldapAttributeSet = searchResult.GetAttributeSet();

				return ldapAttributeSet;
			}

			return null;
		}

		private LdapAttributeSet GetEntryByDn(string dn, string domainDn)
		{
			var searchResults = Connection.Search(domainDn, LdapConnection.ScopeSub,
				$"distinguishedName={dn}", managedAttributes, false);

			foreach (var searchResult in searchResults)
			{
				var ldapAttributeSet = searchResult.GetAttributeSet();

				return ldapAttributeSet;
			}

			return null;
		}

		private string GetParentDn(string parentDn, string domainDn)
		{
			return string.Join(",", new string[] { parentDn, domainDn });
		}

		private string GetDn(string cn, string parentDn, string domainDn)
		{
			return string.Join(",", new string[] { "CN=" + cn.FormatCn(), parentDn, domainDn });
		}

		private LdapAttribute GetLdapAttribute(string name, object value)
		{
			if (value.GetType().IsArray)
			{
				string[] arrayOfValues = ((object[])value).Cast<string>().ToArray();

				return new LdapAttribute(name, arrayOfValues);
			}
			else
			{
				return new LdapAttribute(name, value.ToString());
			}
		}

		private string UpdateCnDn(string currentCn, string newCn, ref string currentDn, string newParentDn, string domainDn)
		{
			if (!string.IsNullOrWhiteSpace(newCn))
			{
				try
				{
					newCn = newCn.FormatCn();

					if (string.IsNullOrWhiteSpace(newCn))
						return currentCn;

					if (currentDn != GetDn(newCn, newParentDn, domainDn))
					{
						Connection.Rename(
							dn: currentDn,
							newRdn: $"CN={newCn}",
							newParentdn: GetParentDn(newParentDn, domainDn),
							deleteOldRdn: true);

						currentDn = GetDn(newCn, newParentDn, domainDn);
					}
				}
				catch (Exception ex)
				{
					throw new Exception($"Error while updating user CN: {ex.Message}");
				}
			}

			return currentCn;
		}

		private void UpdateProxyAddresses(string samAccountName, string[] currentProxyAddresses, string smtpEmailDomain, AttributesDictionary<string, object> attributes)
		{
			try
			{
				var newEmailAddress = attributes.GetValue("mail")?.ToString().NormalizeString();

				if (string.IsNullOrWhiteSpace(newEmailAddress))
					return;

				var allCurrentAddresses = (currentProxyAddresses?.ToList() ?? new List<string>())
					.Where(pa => !string.IsNullOrWhiteSpace(pa))
					.Select(pa => string.Concat(pa.Where(ch => !char.IsWhiteSpace(ch))));
				var smtpCurrentAddresses = allCurrentAddresses.Where(pa => pa.StartsWith("smtp:"))
					.Select(pa => pa.Replace("smtp:", string.Empty).NormalizeString()).ToList();
				var otherCurrentAddresses = allCurrentAddresses.Where(pa => pa.StartsWith("SMTP:") || pa.StartsWith("sip:"))
					.Select(pa => pa.Replace("SMTP:", string.Empty).Replace("sip:", string.Empty).NormalizeString()).ToList();

				smtpCurrentAddresses.Add($"{samAccountName.GetSubstring('@')}@{smtpEmailDomain}");
				otherCurrentAddresses.Add(newEmailAddress);
				smtpCurrentAddresses = smtpCurrentAddresses.Distinct().ToList();
				otherCurrentAddresses = otherCurrentAddresses.Distinct().ToList();

				attributes.SetValue("proxyAddresses",
					otherCurrentAddresses.Select(pa => $"sip:{pa}").Union(
					otherCurrentAddresses.Select(pa => $"SMTP:{pa}")).Union(
					smtpCurrentAddresses.Select(pa => $"smtp:{pa}")).ToArray());
			}
			catch (Exception ex)
			{
				throw new Exception($"Error while updating user proxyAddresses: {ex.Message}");
			}
		}

		private bool TryGetLdapAttributeAsString(LdapAttributeSet attributeSet, string propertyName, out string value)
		{
			value = string.Empty;

			if (attributeSet == null)
			{
				return false;
			}

			if (!attributeSet.TryGetValue(propertyName, out var attribute)) 
				return false;

			value = attribute.StringValue;

			return true;
		}

		private int TryGetLdapAttributeAsInt(LdapAttributeSet attributeSet, string propertyName)
		{
			return !attributeSet.TryGetValue(propertyName, out var attribute) ? default : int.Parse(attribute.StringValue);
		}

		private long TryGetLdapAttributeAsLong(LdapAttributeSet attributeSet, string propertyName)
		{
			return !attributeSet.TryGetValue(propertyName, out var attribute) ? default : long.Parse(attribute.StringValue);
		}

		private DateTime TryGetLdapAttributeAsDateTime(LdapAttributeSet attributeSet, string propertyName)
		{
			return !attributeSet.TryGetValue(propertyName, out var attribute) ? default : DateTime.ParseExact(attribute.StringValue, "yyyyMMddHHmmss.f'Z'", CultureInfo.InvariantCulture);
		}

		private byte[] TryGetLdapAttributeAsByteArray(LdapAttributeSet attributeSet, string propertyName)
		{
			return !attributeSet.TryGetValue(propertyName, out var attribute) ? Array.Empty<byte>() : attribute.ByteValue;
		}

		private bool TryGetLdapAttributeAsStringArray(LdapAttributeSet attributeSet, string propertyName, out string[] value)
		{
			value = Array.Empty<string>();

			if (attributeSet == null)
			{
				return false;
			}

			if (!attributeSet.TryGetValue(propertyName, out var attribute)) 
				return false;

			value = attribute.StringValueArray;

			return true;
		}

		private long GetAdTicks(DateTimeOffset dateTimeOffset)
		{
			return dateTimeOffset.AddYears(-DateTimeToAdTicksOffsetInYears).Ticks;
		}

		#endregion
	}
}
