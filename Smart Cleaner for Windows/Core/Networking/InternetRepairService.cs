using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Smart_Cleaner_for_Windows.Core.Networking;

public sealed class InternetRepairService : IInternetRepairService
{
    public async Task<InternetRepairResult> RunAsync(
        IEnumerable<InternetRepairAction> actions,
        IProgress<InternetRepairStepUpdate>? progress,
        CancellationToken cancellationToken)
    {
        if (actions is null)
        {
            throw new ArgumentNullException(nameof(actions));
        }

        var steps = new List<InternetRepairStepResult>();

        foreach (var action in actions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (action is null)
            {
                continue;
            }

            progress?.Report(new InternetRepairStepUpdate(action, InternetRepairStepState.Starting, null));

            try
            {
                var execution = await ExecuteAsync(action, cancellationToken).ConfigureAwait(false);
                var message = !string.IsNullOrWhiteSpace(execution.Message)
                    ? execution.Message
                    : execution.IsSuccess
                        ? action.SuccessMessage
                        : null;

                progress?.Report(new InternetRepairStepUpdate(
                    action,
                    execution.IsSuccess ? InternetRepairStepState.Succeeded : InternetRepairStepState.Failed,
                    message));

                steps.Add(new InternetRepairStepResult(
                    action,
                    execution.IsSuccess,
                    execution.ExitCode,
                    execution.Output,
                    execution.Error,
                    message));
            }
            catch (OperationCanceledException)
            {
                progress?.Report(new InternetRepairStepUpdate(action, InternetRepairStepState.Cancelled, null));
                throw;
            }
            catch (Exception ex)
            {
                progress?.Report(new InternetRepairStepUpdate(action, InternetRepairStepState.Failed, ex.Message));
                steps.Add(new InternetRepairStepResult(action, false, -1, null, ex.Message, ex.Message));
            }
        }

        return new InternetRepairResult(steps);
    }

    private static async Task<ProcessExecutionResult> ExecuteAsync(InternetRepairAction action, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = action.Command,
            Arguments = action.Arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start '{action.Command}'.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

        var output = Normalize(await outputTask.ConfigureAwait(false));
        var error = Normalize(await errorTask.ConfigureAwait(false));
        var exitCode = process.ExitCode;
        var isSuccess = exitCode == 0;
        var messageSource = !string.IsNullOrWhiteSpace(error) ? error : output;
        var message = ExtractFirstLine(messageSource);

        return new ProcessExecutionResult(isSuccess, exitCode, output, error, message);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Intentionally ignored.
        }
    }

    private static string? ExtractFirstLine(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var line = text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(line) ? null : line.Trim();
    }

    private static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private sealed record ProcessExecutionResult(bool IsSuccess, int ExitCode, string? Output, string? Error, string? Message);
}
