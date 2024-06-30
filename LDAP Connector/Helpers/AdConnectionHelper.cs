using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using System.Diagnostics;

namespace LDAP_Connector.Helpers
{
    public static class AdConnectionHelper
    {
        private static LdapConnection Connection { get; set; }

        private static readonly object _locker = new object();

        static AdConnectionHelper()
        {
            Connection = new LdapConnection();
        }

        private static bool IsConnected()
        {
            return Connection != null && Connection.Connected && Connection.Bound;
        }

        public static LdapConnection GetConnection(ILogger<LDAPConnector> logger,
            string host, int port, string userName, string password, bool isValidateCertificate)
        {
            lock (_locker)
            {
                if (IsConnected()) return Connection;

                logger.LogInformation("No connection to LDAP provider. Reconnecting...");

                var timer = new Stopwatch();
                timer.Start();

                Connection = new LdapConnection();
                Connection.SecureSocketLayer = true;

                if (!isValidateCertificate)
                {
                    Connection.UserDefinedServerCertValidationDelegate += (sender, certificate, chain, errors) => true;
                }

                Connection.ConnectAsync(host, port).GetAwaiter().GetResult();

                Connection.Constraints = new LdapSearchConstraints() { ReferralFollowing = true };
                Connection.ConnectionTimeout = new TimeSpan(0, 0, 30).Milliseconds;
                Connection.BindAsync(userName, password).GetAwaiter().GetResult();

                logger.LogInformation($"Connected to AD. Time taken: {timer.Elapsed.ToString("g")}");
                timer.Reset();

                return Connection;
            }
        }
    }
}
