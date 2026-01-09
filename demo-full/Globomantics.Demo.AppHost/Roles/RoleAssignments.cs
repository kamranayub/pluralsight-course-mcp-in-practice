using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;

namespace Globomantics.Demo.AppHost.Roles;

internal static class RoleAssignments
{
    internal static async Task EnsureRoleAssignmentAsync(
        TokenCredential credential,
    string scopeResourceId,
    Guid principalId,
    Guid roleDefinitionGuid,
    CancellationToken ct)
    {
        var arm = new ArmClient(credential);

        // RBAC scope can be any ARM scope string (resource, RG, subscription, etc.)
        var scope = new ResourceIdentifier(scopeResourceId);

        // This collection is scoped to: {scope}/providers/Microsoft.Authorization/roleAssignments/*
        RoleAssignmentCollection roleAssignments = arm.GetRoleAssignments(scope);

        // RoleAssignment "name" must be a GUID and must be unique per assignment.
        // Use a deterministic GUID if you want idempotency (same principal+role+scope => same assignment id).
        var assignmentName = DeterministicGuid(scopeResourceId, principalId, roleDefinitionGuid);

        // Role definition id must be a full ARM id
        // /subscriptions/{subId}/providers/Microsoft.Authorization/roleDefinitions/{roleGuid}
        // Note: role definitions are at subscription scope even if you assign at resource scope.
        var subId = scope.SubscriptionId
            ?? throw new InvalidOperationException("Scope must include a subscription.");
        var roleDefinitionId = new ResourceIdentifier($"/subscriptions/{subId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionGuid:D}");

        var content = new RoleAssignmentCreateOrUpdateContent(roleDefinitionId, principalId)
        {
            PrincipalType = RoleManagementPrincipalType.ServicePrincipal
        };

        await roleAssignments.CreateOrUpdateAsync(
            waitUntil: Azure.WaitUntil.Completed,
            roleAssignmentName: assignmentName.ToString(),
            content: content,
            cancellationToken: ct);
    }

    static Guid DeterministicGuid(string scope, Guid principalId, Guid roleDefId)
    {
        // Any deterministic scheme is fine; this one is stable across runs.
        // (If you’d rather allow multiple assignments over time, just use Guid.NewGuid().)
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes($"{scope}|{principalId:D}|{roleDefId:D}");
        var hash = sha.ComputeHash(bytes);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}