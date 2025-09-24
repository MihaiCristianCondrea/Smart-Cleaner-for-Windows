using System;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core;

public interface IStaTaskScheduler
{
    Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken);
}
