namespace Throughline.Common.Presentation.Auth;

public static class ThroughlineRoles
{
    /// <summary>
    ///     Name of the Entra role that EDI 940 integrators require membership in
    /// </summary>
    public const string OrderIntakeIntegrator = "OrderIntakeIntegrator";

    public const string Receiving = "Receiving";
    public const string Inventory = "Inventory";

    /// <summary>
    ///     Name of the Entra role held by outbound planners, who release orders for allocation in waves
    /// </summary>
    public const string OutboundPlanning = "OutboundPlanning";

    /// <summary>
    ///     Membership in this role, in combination with function specific roles, grants
    ///     access to endpoints for a specific owner
    /// </summary>
    /// <param name="ownerId"></param>
    /// <returns></returns>
    public static string OwnerRole(int ownerId) => $"Owner.{ownerId}";
}