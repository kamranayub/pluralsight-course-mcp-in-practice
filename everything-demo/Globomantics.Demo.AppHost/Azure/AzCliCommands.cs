using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Hosting.Pipelines;

namespace Globomantics.Demo.AppHost.Azure;

internal static class AzCliCommands
{
    public static async Task EnableContainerAppCorsPolicyForMcpInspector(string containerAppName, string resourceGroupName, CancellationToken ct)
    {
        await RunAzCliCommand(ct,
            "containerapp", "ingress", "cors", "enable",
            "--name", containerAppName,
            "--resource-group", resourceGroupName,
            "--allowed-origins", "http://localhost:6274"
        ).ConfigureAwait(false);
    }

    public static async Task ConfigureContainerAppAuthWithMicrosoft(string containerAppName, string resourceGroupName, string tenantId, string clientId, string[] allowedAudiences, CancellationToken ct)
    {
        await RunAzCliCommand(ct,
            "containerapp", "auth", "microsoft", "update",
            "--name", containerAppName,
            "--resource-group", resourceGroupName,
            "--client-id", clientId!,
            "--client-secret-name", "microsoft-provider-authentication-secret", // Matches the environment variable set earlier but as a container app secret
            "--tenant-id", tenantId!,
            "--allowed-audiences", string.Join(",", allowedAudiences),
            "--yes"
        ).ConfigureAwait(false);
    }

    public static async Task<Uri?> GetContainerAppEndpoint(string containerAppName, string resourceGroupName, string clientId, CancellationToken ct)
    {
        var fqdn = await RunAzCliCommand(ct,
            "containerapp", "show",
            "--name", containerAppName,
            "--resource-group", resourceGroupName,
            "--query", "properties.configuration.ingress.fqdn",
            "--output", "tsv"
        ).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(fqdn))
        {
            throw new InvalidOperationException("Failed to get Container App FQDN from az CLI");
        }

        if (!Uri.TryCreate($"https://{fqdn}", UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Failed to create Container App ingress URI from FQDN: {fqdn}");
        }

        return uri;
    }

    public static async Task ConfigureContainerAppAuthRedirectUri(Uri containerEndpoint, string clientId, CancellationToken ct)
    {
        await RunAzCliCommand(ct,
            "ad", "app", "update",
            "--id", clientId,
            "--web-redirect-uris", new Uri(containerEndpoint, ".auth/login/aad/callback").ToString()
        ).ConfigureAwait(false);
    }

    public static async Task<string[]> GetAspireResourceGroups(CancellationToken ct)
    {
        var resourceGroups = await RunAzCliCommand(ct,
            "group", "list",
            "--query", "[?tags.aspire=='true'].{name: name}",
            "-o", "tsv"
        ).ConfigureAwait(false);
        
        return [.. resourceGroups.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim())];
    }

    public static async Task DeleteResourceGroup(string resourceGroupName, CancellationToken ct)
    {
        await RunAzCliCommand(ct,
            "group", "delete",
            "--name", resourceGroupName,
            "--yes"
        ).ConfigureAwait(false);
    }

    public static async Task<string> GetSoftDeletedFoundryAccount(string foundryResourceName, CancellationToken ct)
    {
        var deletedFoundryAccount =  await RunAzCliCommand(ct,
            "cognitiveservices", "account", "list-deleted",
            "--query", $"[?tags.\"aspire-resource-name\"=='{foundryResourceName}'].id",
            "-o", "tsv"
        ).ConfigureAwait(false);

        return deletedFoundryAccount;
    }

    public static async Task DeleteAzResourceById(string resourceId, CancellationToken ct)
    {
        await RunAzCliCommand(ct,
            "resource", "delete",
            "--ids", resourceId,
            "--no-wait"
        ).ConfigureAwait(false);
    }

    public static async Task<Guid?> GetSignedInUserPrincipalId(CancellationToken ct)
    {
        var signedInUserId = await RunAzCliCommand(ct,
            "ad", "signed-in-user", "show",
            "--query", "id",
            "--output", "tsv"
        ).ConfigureAwait(false);

        return Guid.TryParse(signedInUserId, out var userId) ? userId : null;
    }

    static async Task<string> RunAzCliCommand(CancellationToken cancellationToken = default, params string[] args)
    {
        using var azProcess = Process.Start(CreateAzStartInfo(args)) ?? throw new InvalidOperationException("Failed to start az CLI process");

        var stdoutTask = azProcess.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = azProcess.StandardError.ReadToEndAsync(cancellationToken);

        await azProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (azProcess.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"az CLI process exited with code {azProcess.ExitCode}\nSTDOUT: {stdout}\nSTDERR: {stderr}");
        }    

        return stdout.Trim();
    }

    static ProcessStartInfo CreateAzStartInfo(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "az",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.ArgumentList.Add("/d");
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add("az.cmd");
        }

        foreach (var a in args)
            psi.ArgumentList.Add(a);

        return psi;
    }
}