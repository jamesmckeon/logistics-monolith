using Throughline.Modules.Receiving.Application.Common;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal interface IDefaultLocationsProvider
{
    Task<DefaultLocations> GetDefaultLocationsAsync(CancellationToken token);
}