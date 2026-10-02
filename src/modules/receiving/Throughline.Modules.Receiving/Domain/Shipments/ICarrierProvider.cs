namespace Throughline.Modules.Receiving.Domain.Shipments;

internal interface ICarrierProvider
{
    Task<CarrierScac?> GetCarrierScacByScacCodeAync(ScacCode scacCode, CancellationToken token);
}