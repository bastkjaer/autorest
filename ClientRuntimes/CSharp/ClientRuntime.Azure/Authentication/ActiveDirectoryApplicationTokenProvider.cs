﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace Microsoft.Azure.Authentication
{
    /// <summary>
    /// Provides tokens for Azure Active Directory applications. 
    /// </summary>
    internal class ActiveDirectoryApplicationTokenProvider : ITokenProvider
    {
        private AuthenticationContext _authenticationContext;
        private string _tokenAudience;
        private string _clientId;
        private ClientCredential _credential;
        private string _type;

        /// <summary>
        /// Initializes a token provider for application credentials.
        /// </summary>
        /// <param name="domain">The domain or tenant id for the application.</param>
        /// <param name="clientId">The client Id of the application in Active Directory.</param>
        /// <param name="secret">The application secret, used for authentication.</param>
        /// <param name="environment">The Azure environment to manage resources in.</param>
        public ActiveDirectoryApplicationTokenProvider(string domain, string clientId, string secret, AzureEnvironment environment) 
            : this(domain, clientId, secret, environment, null)
        {
        }

        /// <summary>
        /// Initializes a token provider for application credentials.
        /// </summary>
        /// <param name="domain">The domain or tenant id for the application.</param>
        /// <param name="clientId">The client Id of the application in Active Directory.</param>
        /// <param name="secret">The application secret, used for authentication.</param>
        /// <param name="environment">The Azure environment to manage resources in.</param>
        /// <param name="cache">The TokenCache to use during authentication.</param>
        internal ActiveDirectoryApplicationTokenProvider(string domain, string clientId, string secret, AzureEnvironment environment, TokenCache cache)
        {
             if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentOutOfRangeException("secret");
            }

            InitializeAuthenticationContext(domain, clientId, environment, cache);
            this._credential = new ClientCredential(clientId, secret);
            ValidateAuthenticationResult(Authenticate().Result);
       }

        /// <summary>
        /// Returns the token type of the returned token.
        /// </summary>
        public string TokenType
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets an access token from the token cache or from AD authentication endpoint. 
        /// Attempts to refresh the access token if it has expired.
        /// </summary>
        public virtual async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            var result = await this.Authenticate();
            ValidateAuthenticationResult(result);
            return result.AccessToken;
        }

        /// <summary>
        /// Initialize private fields in the token provider.
        /// </summary>
        /// <param name="domain">The domain or tenant id for the application.</param>
        /// <param name="clientId">The client Id of the application in Active Directory.</param>
        /// <param name="environment">The Azure environment to manage resources in.</param>
        /// <param name="cache">The TokenCache to use while authenticating.</param>
        protected void InitializeAuthenticationContext(string domain, string clientId,
            AzureEnvironment environment, TokenCache cache = null)
        {
            ValidateCommonParameters(clientId, domain, environment);
            this._clientId = clientId;
            this._tokenAudience = environment.TokenAudience.ToString();
            this._authenticationContext = (cache == null)
                ? new AuthenticationContext(environment.AuthenticationEndpoint + domain, environment.ValidateAuthority)
                : new AuthenticationContext(environment.AuthenticationEndpoint + domain, environment.ValidateAuthority,
                    cache);
        }
        /// <summary>
        /// Set the ActiveDirectory authentication properties for this user
        /// </summary>
        /// <param name="authenticationResult">The authentication result</param>
        protected void ValidateAuthenticationResult(AuthenticationResult authenticationResult)
        {
            if (authenticationResult == null || authenticationResult.AccessToken == null )
            {
                throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture,
                    "Authentication with Azure Active Directory Failed using clientId; {0}",
                    this._clientId));
            }

            this._type = authenticationResult.AccessTokenType;
        }

        private async Task<AuthenticationResult> Authenticate()
        {
            return await this._authenticationContext.AcquireTokenAsync(this._tokenAudience, this._credential);
        }

        /// <summary>
        /// Validate the parameters used by every constructor.
        /// </summary>
        /// <param name="clientId">The client Id of the application in Active Directory.</param>
        /// <param name="domain">The domain or tenant id for the service principal.</param>
        /// <param name="environment">The Azure environment to manage resources in.</param>
        private static void ValidateCommonParameters(string clientId, string domain, AzureEnvironment environment)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentOutOfRangeException("clientId");
            }
            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentOutOfRangeException("domain");
            }
            if (environment == null || environment.AuthenticationEndpoint == null || environment.TokenAudience == null)
            {
                throw new ArgumentOutOfRangeException("environment");
            }
        }
    }
}
