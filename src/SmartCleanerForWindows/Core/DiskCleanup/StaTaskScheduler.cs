using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SmartCleanerForWindows.Core.DiskCleanup;

public sealed class StaTaskScheduler : IStaTaskScheduler
{
    public Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.ThrowIfCancellationRequested();

        var thread = new Thread(() =>
        {
            try
            {
                var hr = NativeMethods.CoInitializeEx(IntPtr.Zero, CoInit.Apartmentthreaded);
                var initialized = hr >= 0;

                if (hr == HResults.RpcEChangedMode)
                {
                    hr = NativeMethods.CoInitializeEx(IntPtr.Zero, CoInit.Multithreaded);
                    initialized = hr >= 0;
                }

                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = action(cancellationToken);
                    tcs.TrySetResult(result);
                }
                finally
                {
                    if (initialized)
                    {
                        NativeMethods.CoUninitialize();
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                tcs.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "DiskCleanupWorker"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        }

        return tcs.Task;
    }

    private static class NativeMethods
    {
        [DllImport("ole32.dll")]
        public static extern int CoInitializeEx(IntPtr pvReserved, CoInit dwCoInit);

        [DllImport("ole32.dll")]
        public static extern void CoUninitialize();
    }

    private static class HResults
    {
        public const int RpcEChangedMode = unchecked((int)0x80010106);
    }

    [Flags]
    private enum CoInit : uint
    {
        Multithreaded = 0x0,
        Apartmentthreaded = 0x2,
    }
}
