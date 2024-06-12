using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace Metu.MarketData.Client.Services;

public interface IMdService
{
    Task SubscribeAndListenAsync(IEnumerable<string> Symbols, CancellationToken cancellationToken);
}
