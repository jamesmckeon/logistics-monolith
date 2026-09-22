namespace Throughline.Modules.Inventory.Domain.Common;

internal interface ISpecification<T>
{
    bool IsSatisfiedBy(T obj);
}