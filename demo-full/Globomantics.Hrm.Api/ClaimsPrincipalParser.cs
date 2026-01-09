using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.IdentityModel.JsonWebTokens;

public static class ClaimsPrincipalParser
{
    private static JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private class ClientPrincipalClaim
    {
        [JsonPropertyName("typ")]
        public string Type { get; set; }
        [JsonPropertyName("val")]
        public string Value { get; set; }
    }

    private class ClientPrincipal
    {
        [JsonPropertyName("auth_typ")]
        public string IdentityProvider { get; set; }
        [JsonPropertyName("name_typ")]
        public string NameClaimType { get; set; }
        [JsonPropertyName("role_typ")]
        public string RoleClaimType { get; set; }
        [JsonPropertyName("claims")]
        public IEnumerable<ClientPrincipalClaim> Claims { get; set; }
    }

    public static ClaimsPrincipal Parse(HttpRequestData req)
    {
        ClientPrincipal? principal = null;

        if (req.Headers.TryGetValues("x-ms-client-principal", out var header))
        {
            var data = header.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(data))
            {
                var decoded = Convert.FromBase64String(data);
                var json = Encoding.UTF8.GetString(decoded);
                
                principal = JsonSerializer.Deserialize<ClientPrincipal>(json, SerializerOptions);
            }
        }

        principal ??= new ClientPrincipal();
        
        var identity = new ClaimsIdentity(principal.IdentityProvider, principal.NameClaimType, principal.RoleClaimType);
        if (principal.Claims != null) {
            identity.AddClaims(principal.Claims.Select(c => new Claim(c.Type, c.Value)));
        }
        
        return new ClaimsPrincipal(identity);
    }

    public static string ToClientPrincipalJson(AccessToken accessToken)
    {
        var jwtToken = new JsonWebToken(accessToken.Token);
        var identity = new ClaimsIdentity(jwtToken.Claims, "Bearer", "name", "role");

        var clientPrincipal = new ClientPrincipal()
        {            
            IdentityProvider = jwtToken.GetPayloadValue<string>("idp") ?? "live.com",
            NameClaimType = "name",
            RoleClaimType = "roles",
            Claims = identity.Claims.Select(c => new ClientPrincipalClaim()
            {
                Type = c.Type,
                Value = c.Value
            }).ToList()
        };

        return JsonSerializer.Serialize(clientPrincipal, SerializerOptions);
    }
}