using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.Common;

internal sealed record ReceivedLpn(UpperCaseString Lpn, NonFutureDateTime ReceivedOn);