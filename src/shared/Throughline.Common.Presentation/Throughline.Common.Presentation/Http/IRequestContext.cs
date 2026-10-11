namespace Throughline.Common.Presentation.Http;

public interface IRequestContext
{
    string? GetHeaderValue(string value);
}