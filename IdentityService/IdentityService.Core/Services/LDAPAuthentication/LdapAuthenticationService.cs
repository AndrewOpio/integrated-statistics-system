using IdentityService.Core.Models;
using IdentityService.Core.Services.AdAuthentication;
using IdentityService.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
//using Novell.Directory.Ldap;
using System.Net;

namespace IdentityService.Core.Services.LDAPAuthentication
{
    public class LdapAuthenticationService : ILdapAuthenticationService
    {
        private readonly LdapSettings _settings;
        private readonly ILogger<LdapAuthenticationService> _logger;

        public LdapAuthenticationService(IOptions<LdapSettings> settings, ILogger<LdapAuthenticationService> logger)
        {
            _settings = settings.Value;
            _logger = logger;   
        }

        public async Task<bool> ValidateCredentials(string username, string password)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _settings.Server, _settings.SearchBase))
                {
                    return context.ValidateCredentials(username, password);
                }
                //return true;
            }
            catch (PrincipalServerDownException ex)
            {
                // Handle connection issues with LDAP server
                // Log the exception
                _logger.LogError(ex, "LDAP server is down or unreachable. Server: {LdapServer}", _settings.Server);
                return false;
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                // Log the exception
                _logger.LogError(ex, "An unexpected error occurred during LDAP authentication. Username: {Username}", username);
                return false;
            }
        }

        public async Task<bool> ValidateUserCredentials(string username, string password)
        {
            // using System.DirectoryServices.Protocols;
            try
            {
                using (var connection = new LdapConnection(
                    new LdapDirectoryIdentifier(_settings.Server, _settings.Port)))
                {
                    connection.SessionOptions.ProtocolVersion = 3;
                    connection.SessionOptions.SecureSocketLayer = _settings.UseSSL;

                    // First, bind with service account to search for user
                    connection.Bind(new NetworkCredential(_settings.BindDn, _settings.BindPassword));

                    string detailedLog = $"Server: {_settings.Server} \nBindDn: {_settings.BindDn}" +
                        $"\nPort: {_settings.Port} \nUseSSL: {_settings.UseSSL} \nBindPassword: {_settings.BindPassword}";
                    _logger.LogInformation(detailedLog);

                    // Search for the user
                    var searchFilter = string.Format(_settings.SearchFilter, username);
                    var searchRequest = new SearchRequest(
                        _settings.SearchBase,
                        searchFilter,
                        SearchScope.Subtree,
                        "dn"
                    );

                    var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

                    if (searchResponse.Entries.Count == 0)
                    {
                        return false; // User not found
                    }

                    var userDn = searchResponse.Entries[0].DistinguishedName;

                    string detailedLog2 = $"Username: {username} \nPassword: {password}" +
                        $"\nuserDn: {userDn} \nsearchFilter: {searchFilter}";
                    _logger.LogInformation(detailedLog2);

                    // Try to bind with user credentials
                    using (var userConnection = new LdapConnection(
                        new LdapDirectoryIdentifier(_settings.Server, _settings.Port)))
                    {
                        userConnection.SessionOptions.ProtocolVersion = 3;
                        userConnection.SessionOptions.SecureSocketLayer = _settings.UseSSL;
                        userConnection.Bind(new NetworkCredential(username, password));
                        return true;
                    }
                }
            }
            catch (LdapException e)
            {
                _logger.LogError(e.Message);
                return false;
            }


            //////// using Novell.Directory.Ldap;
            //if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            //{
            //    return false;
            //}

            //try
            //{
            //    using var connection = new LdapConnection();

            //    // Connect to LDAP server
            //    await connection.ConnectAsync(_settings.Server, _settings.Port);

            //    if (_settings.UseSSL)
            //    {
            //        connection.SecureSocketLayer = true;
            //    }

            //    // First, bind with service account to search for user
            //    await connection.BindAsync(_settings.BindDn, _settings.BindPassword);

            //    // Search for the user
            //    var searchFilter = string.Format(_settings.SearchFilter, EscapeLdapSearchFilter(username));
            //    var searchResults = await connection.SearchAsync(
            //        _settings.SearchBase,
            //        LdapConnection.ScopeSub,
            //        searchFilter,
            //        new[] { "dn" },
            //        false
            //    );

            //    if (!(await searchResults.HasMoreAsync()))
            //    {
            //        _logger.LogWarning("User not found: {Username}", username);
            //        return false;
            //    }

            //    var userEntry = await searchResults.NextAsync();
            //    var userDn = userEntry.Dn;

            //    // Disconnect the service account
            //    connection.Disconnect();

            //    // Try to bind with user credentials
            //    using var userConnection = new LdapConnection();
            //    await userConnection.ConnectAsync(_settings.Server, _settings.Port);

            //    if (_settings.UseSSL)
            //    {
            //        userConnection.SecureSocketLayer = true;
            //    }

            //    await userConnection.BindAsync(userDn, password);

            //    _logger.LogInformation("Successfully authenticated user: {Username}", username);
            //    return true;
            //}
            //catch (LdapException ex)
            //{
            //    _logger.LogWarning(ex, "LDAP authentication failed for user: {Username}", username);
            //    return false;
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Unexpected error during authentication for user: {Username}", username);
            //    return false;
            //}

        }

        public async Task<LdapUser> GetUserDetails(string username)
        {
            // using System.DirectoryServices.Protocols;
            try
            {
                using (var connection = new LdapConnection(
                    new LdapDirectoryIdentifier(_settings.Server, _settings.Port)))
                {
                    connection.SessionOptions.ProtocolVersion = 3;
                    connection.SessionOptions.SecureSocketLayer = _settings.UseSSL;
                    connection.Bind(new NetworkCredential(_settings.BindDn, _settings.BindPassword));

                    var searchFilter = string.Format(_settings.SearchFilter, username);
                    var searchRequest = new SearchRequest(
                        _settings.SearchBase,
                        searchFilter,
                        SearchScope.Subtree,
                        "cn", "mail", "displayName", "memberOf"
                    );

                    var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

                    if (searchResponse.Entries.Count == 0)
                    {
                        return null;
                    }

                    var entry = searchResponse.Entries[0];
                    var groups = new System.Collections.Generic.List<string>();

                    if (entry.Attributes.Contains("memberOf"))
                    {
                        foreach (string group in entry.Attributes["memberOf"].GetValues(typeof(string)))
                        {
                            groups.Add(group);
                        }
                    }

                    return new LdapUser
                    {
                        Username = username,
                        Email = entry.Attributes.Contains("mail")
                            ? entry.Attributes["mail"][0].ToString()
                            : null,
                        DisplayName = entry.Attributes.Contains("displayName")
                            ? entry.Attributes["displayName"][0].ToString()
                            : entry.Attributes.Contains("cn")
                                ? entry.Attributes["cn"][0].ToString()
                                : username,
                        Groups = groups.ToArray()
                    };
                }
            }
            catch (LdapException)
            {
                return null;
            }


            //////// using Novell.Directory.Ldap;
            //if (string.IsNullOrWhiteSpace(username))
            //{
            //    return null;
            //}

            //try
            //{
            //    using var connection = new LdapConnection();
            //    await connection.ConnectAsync(_settings.Server, _settings.Port);

            //    if (_settings.UseSSL)
            //    {
            //        connection.SecureSocketLayer = true;
            //    }

            //    await connection.BindAsync(_settings.BindDn, _settings.BindPassword);

            //    var searchFilter = string.Format(_settings.SearchFilter, EscapeLdapSearchFilter(username));
            //    var searchResults = await connection.SearchAsync(
            //        _settings.SearchBase,
            //        LdapConnection.ScopeSub,
            //        searchFilter,
            //        new[] { "cn", "mail", "displayName", "memberOf", "uid" },
            //        false
            //    );

            //    if (!(await searchResults.HasMoreAsync()))
            //    {
            //        _logger.LogWarning("User details not found: {Username}", username);
            //        return null;
            //    }

            //    var entry = await searchResults.NextAsync();
            //    var groups = new List<string>();

            //    // Get memberOf attribute
            //    var memberOfAttr = entry.GetAttributeSet("memberOf");
            //    if (memberOfAttr != null)
            //    {
            //        foreach (LdapAttribute attr in memberOfAttr)
            //        {
            //            groups.AddRange(attr.StringValueArray);
            //        }
            //        //groups.AddRange(memberOfAttr.StringValueArray);
            //    }

            //    var user = new LdapUser
            //    {
            //        Username = username,
            //        Email = GetAttributeValue(entry, "mail"),
            //        DisplayName = GetAttributeValue(entry, "displayName")
            //            ?? GetAttributeValue(entry, "cn")
            //            ?? username,
            //        Groups = groups.ToArray()
            //    };

            //    _logger.LogInformation("Retrieved details for user: {Username}", username);
            //    return user;
            //}
            //catch (LdapException ex)
            //{
            //    _logger.LogError(ex, "LDAP error retrieving user details: {Username}", username);
            //    return null;
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Unexpected error retrieving user details: {Username}", username);
            //    return null;
            //}
        }

        //private string GetAttributeValue(LdapEntry entry, string attributeName)
        //{
        //    var attrSet = entry.GetAttributeSet(attributeName);
        //    return attrSet.FirstOrDefault().Value.StringValue;
        //}

        private string EscapeLdapSearchFilter(string searchFilter)
        {
            if (string.IsNullOrEmpty(searchFilter))
                return searchFilter;

            return searchFilter
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");
        }

        public AdUserResult IsExistsOnAd(UserViewModel model)
        {
            try
            {
                // Create a PrincipalContext for the domain
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    // Search for the user by SAM account name
                    var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, model.UserName);

                    // If the user is found, return IsSuccess = true; otherwise, return IsSuccess = false
                    if (user != null)
                    {
                        User appUser = new User()
                        {
                            UserName = model.UserName,
                            EndDate = model.EndDate,
                            FirstName = user.GivenName,
                            MiddleName = user.MiddleName,
                            LastName = user.Surname,
                            Email = user.EmailAddress,
                            Title = model.Title,
                        };

                        return new AdUserResult
                        {
                            User = user,
                            AppUser = appUser,
                            IsSuccess = true,
                            ResultType = AdResultTypes.Success
                        };
                    }
                    else
                    {
                        // User not found
                        return new AdUserResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"User '{model.UserName}' not found in Active Directory",
                            ResultType = AdResultTypes.UserNotFound
                        };
                    }
                }
            }
            catch (PrincipalServerDownException ex)
            {
                //Console.WriteLine("Unable to connect to the domain controller.");
                //return null;
                return new AdUserResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Unable to connect to the Active Directory server",
                    Exception = ex,
                    ResultType = AdResultTypes.ServerUnavailable
                };
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error occurred: {ex.Message}");
                //return null;
                return new AdUserResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred while checking Active Directory: {ex.Message}",
                    Exception = ex,
                    ResultType = AdResultTypes.OtherError
                };
            }
        }
    }
}
