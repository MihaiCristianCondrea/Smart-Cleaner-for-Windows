using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartCleanerForWindows.Core.DiskCleanup;

public interface IStaTaskScheduler
{
    Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken);
}
