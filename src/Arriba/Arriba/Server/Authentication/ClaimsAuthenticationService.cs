// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Principal;
using Arriba.Caching;
using Arriba.ParametersCheckers;

namespace Arriba.Server.Authentication
{
    /// <summary>
    /// Windows authentication utilities. 
    /// </summary>
    public class ClaimsAuthenticationService : IDisposable
    {
        private readonly RuntimeCache _cache;
        private readonly TimeSpan _defaultTimeToLive = TimeSpan.FromMinutes(15);

        public ClaimsAuthenticationService(IObjectCacheFactory factory)
        {
            _cache = new RuntimeCache(factory.CreateCache("Arriba.ClaimsAuthentication"));
        }

        /// <summary>
        /// Determines whether the specified user is within the specified security group. 
        /// </summary>
        /// <param name="principal">User principal to check.</param>
        /// <param name="roleName">Role to validate.</param>
        /// <returns>True if the user is in the specified role, otherwise false.</returns>
        public bool IsUserInGroup(IPrincipal principal, string roleName)
        {
            principal.ThrowIfNull(nameof(principal));
            roleName.ThrowIfNullOrWhiteSpaced(nameof(roleName));

            ClaimsPrincipal cPrincipal = principal as ClaimsPrincipal;

            if (cPrincipal.Identity == null || String.IsNullOrEmpty(cPrincipal.Identity.Name) || !principal.Identity.IsAuthenticated)
            {
                return false;
            }

            // Cachekey should be in the form of UserInGroup:{Identity}:{Role}
            string cacheKey = String.Concat("UserInGroup:", principal.Identity.Name, ":", roleName);

            Debug.Assert(cacheKey.Contains(principal.Identity.Name));
            Debug.Assert(cacheKey.Contains(roleName));

            return _cache.GetOrAdd<bool>(
                cacheKey,
                () => principal.IsInRole(roleName),
                timeToLive: _defaultTimeToLive);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}

