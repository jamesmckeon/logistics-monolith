namespace Throughline.Modules.Platform.Infrastructure.Common;

internal static class InfrastructureSettings
{
    public const string SchemaName = "platform";

    /// <summary>
    ///     The schema Wolverine's tables live in (<c>PersistMessagesWithPostgresql</c> in <c>Program.cs</c>)
    /// </summary>
    public const string WolverineSchemaName = "wolverine";
}
