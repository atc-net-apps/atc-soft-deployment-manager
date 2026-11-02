namespace AtcSoft.Deployment.Manager.Windows.Tests;

/// <summary>
/// The tests in this project change the PC (firewall rules, processes) and need admin rights.
/// They run only when <see cref="Variable"/> is "1" or "true", e.g. on a test machine or VM.
/// </summary>
public static class MachineTests
{
    public const string Variable = "ATCSOFT_DEPLOYMENTMANAGER_MACHINE_TESTS";

    public const string SkipReason = "Changes the PC; set " + Variable + "=1 to run it.";

    public static bool IsEnabled
        => Environment.GetEnvironmentVariable(Variable) is { } value &&
           (value.Trim() == "1" || (bool.TryParse(value, out var enabled) && enabled));
}