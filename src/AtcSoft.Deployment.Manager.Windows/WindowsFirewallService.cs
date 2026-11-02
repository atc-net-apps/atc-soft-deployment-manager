namespace AtcSoft.Deployment.Manager.Windows;

/// <summary>
/// Windows Firewall rules through the firewall COM API (HNetCfg.FwPolicy2), late bound.
/// </summary>
/// <remarks>
/// Late binding (dynamic) instead of a COMReference to NetFwTypeLib, because the .NET SDK's MSBuild
/// can't resolve COM references (MSB4803), so `dotnet build` would fail.
/// </remarks>
[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "OK.")]
[SupportedOSPlatform("Windows")]
public class WindowsFirewallService : IWindowsFirewallService
{
    private readonly dynamic? firewallPolicy;

    public WindowsFirewallService()
    {
        var policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2")!;
        firewallPolicy = Activator.CreateInstance(policyType)!;
    }

    private enum FirewallAction
    {
        Allow = 1, // NET_FW_ACTION_ALLOW
    }

    private enum FirewallDirection
    {
        In = 1, // NET_FW_RULE_DIR_IN
        Out = 2, // NET_FW_RULE_DIR_OUT
    }

    private enum FirewallProtocol
    {
        Tcp = 6, // NET_FW_IP_PROTOCOL_TCP
        Udp = 17, // NET_FW_IP_PROTOCOL_UDP
        Any = 256, // NET_FW_IP_PROTOCOL_ANY
    }

    public bool DoesRuleExist(string ruleName)
        => FindRule(ruleName) is not null;

    public bool IsRuleEnabled(string ruleName)
    {
        var rule = FindRule(ruleName);
        return rule is not null && (bool)rule.Enabled;
    }

    public (bool IsSucceeded, string? ErrorMessage) AddInboundRuleForAllowTcp(
        string ruleName,
        string description,
        int port)
        => AddRule(ruleName, description, port, FirewallAction.Allow, FirewallDirection.In, FirewallProtocol.Tcp);

    public (bool IsSucceeded, string? ErrorMessage) AddInboundRuleForAllowUdp(
        string ruleName,
        string description,
        int port)
        => AddRule(ruleName, description, port, FirewallAction.Allow, FirewallDirection.In, FirewallProtocol.Udp);

    public (bool IsSucceeded, string? ErrorMessage) AddInboundRuleForAllowAny(
        string ruleName,
        string description,
        int port)
        => AddRule(ruleName, description, port, FirewallAction.Allow, FirewallDirection.In, FirewallProtocol.Any);

    public (bool IsSucceeded, string? ErrorMessage) AddOutboundRuleForAllowTcp(
        string ruleName,
        string description,
        int port)
        => AddRule(ruleName, description, port, FirewallAction.Allow, FirewallDirection.Out, FirewallProtocol.Tcp);

    public (bool IsSucceeded, string? ErrorMessage) AddOutboundRuleForAllowUdp(
        string ruleName,
        string description,
        int port)
        => AddRule(ruleName, description, port, FirewallAction.Allow, FirewallDirection.Out, FirewallProtocol.Udp);

    public (bool IsSucceeded, string? ErrorMessage) AddOutboundRuleForAllowAny(
        string ruleName,
        string description,
        int port)
        => AddRule(ruleName, description, port, FirewallAction.Allow, FirewallDirection.Out, FirewallProtocol.Any);

    public (bool IsSucceeded, string? ErrorMessage) EnableRule(string ruleName)
        => SetRuleEnabled(ruleName, enabled: true);

    public (bool IsSucceeded, string? ErrorMessage) DisableRule(string ruleName)
        => SetRuleEnabled(ruleName, enabled: false);

    public (bool IsSucceeded, string? ErrorMessage) RemoveRule(string ruleName)
    {
        var policy = GetPolicy();

        if (!DoesRuleExist(ruleName))
        {
            return (false, "Rule do not exist");
        }

        try
        {
            policy.Rules.Remove(ruleName);

            return (true, null);
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Unauthorized Access");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private dynamic GetPolicy()
        => firewallPolicy ?? throw new COMException("FirewallPolicy is not initialized");

    private dynamic? FindRule(string ruleName)
    {
        foreach (var rule in GetPolicy().Rules)
        {
            if (((string)rule.Name).Equals(ruleName, StringComparison.OrdinalIgnoreCase))
            {
                return rule;
            }
        }

        return null;
    }

    private (bool IsSucceeded, string? ErrorMessage) SetRuleEnabled(
        string ruleName,
        bool enabled)
    {
        var rule = FindRule(ruleName);
        if (rule is null)
        {
            return (false, "Rule do not exist");
        }

        if ((bool)rule.Enabled == enabled)
        {
            return (false, enabled ? "Rule is already enabled" : "Rule is already disabled");
        }

        try
        {
            rule.Enabled = enabled;

            return (true, null);
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Unauthorized Access");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private (bool IsSucceeded, string? ErrorMessage) AddRule(
        string ruleName,
        string description,
        int port,
        FirewallAction action,
        FirewallDirection direction,
        FirewallProtocol protocol)
    {
        var policy = GetPolicy();

        if (DoesRuleExist(ruleName))
        {
            return (false, "Rule already exist");
        }

        try
        {
            var ruleType = Type.GetTypeFromProgID("HNetCfg.FWRule")!;
            dynamic rule = Activator.CreateInstance(ruleType)!;

            rule.Name = ruleName;
            rule.Description = description;
            rule.Action = (int)action;
            rule.Direction = (int)direction;
            rule.InterfaceTypes = "All";
            rule.Protocol = (int)protocol;
            rule.Enabled = true;
            rule.LocalPorts = port.ToString(GlobalizationConstants.EnglishCultureInfo);

            policy.Rules.Add(rule);

            return (true, null);
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Unauthorized Access");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}