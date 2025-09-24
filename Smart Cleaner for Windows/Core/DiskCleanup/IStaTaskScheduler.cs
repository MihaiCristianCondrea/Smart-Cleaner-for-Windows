using System;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core; // FIXME: Namespace does not correspond to file location, must be: 'Smart_Cleaner_for_Windows.Core.DiskCleanup'

public interface IStaTaskScheduler
{
    Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken);
}
