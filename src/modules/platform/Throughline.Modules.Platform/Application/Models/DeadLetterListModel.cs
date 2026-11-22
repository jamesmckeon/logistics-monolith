namespace Throughline.Modules.Platform.Application.Models;

/// <summary>
///     The dead-letter list, wrapped in an object so that adding paging later doesn't change the response's shape
/// </summary>
internal sealed record DeadLetterListModel(IReadOnlyCollection<DeadLetterModel> DeadLetters);
