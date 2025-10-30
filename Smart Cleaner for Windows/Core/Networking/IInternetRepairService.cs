using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core.Networking;

public interface IInternetRepairService
{
    Task<InternetRepairResult> RunAsync(
        IEnumerable<InternetRepairAction> actions,
        IProgress<InternetRepairStepUpdate>? progress,
        CancellationToken cancellationToken);
}
