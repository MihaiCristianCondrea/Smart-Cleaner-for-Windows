using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace SmartCleanerForWindows.Settings;

public sealed class ToolSettingsService : IDisposable
{
    private readonly ToolSettingsStore _store;
    private readonly FileSystemWatcher _watcher;
    private readonly ConcurrentDictionary<string, ToolSettingsSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    private ToolSettingsService(string? definitionRoot = null, string? userRoot = null)
    {
        var catalog = new ToolSettingsCatalog(definitionRoot);
        _store = new ToolSettingsStore(userRoot);
        var definitions = catalog.LoadDefinitions();
        foreach (var definition in definitions)
        {
            var values = _store.LoadValues(definition);
            _snapshots[definition.Id] = new ToolSettingsSnapshot
            {
                Definition = definition,
                Values = values
            };
        }

        _watcher = _store.CreateWatcher();
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
    }

    public event EventHandler<ToolSettingsChangedEventArgs>? SettingsChanged;

    public IReadOnlyList<ToolSettingsDefinition> Definitions => _snapshots.Values
        .Select(snapshot => snapshot.Definition)
        .OrderBy(definition => definition.Order)
        .ThenBy(definition => definition.Title, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public ToolSettingsSnapshot? GetSnapshot(string toolId)
    {
        return _snapshots.TryGetValue(toolId, out var snapshot)
            ? new ToolSettingsSnapshot
            {
                Definition = snapshot.Definition,
                Values = CloneValues(snapshot.Values)
            }
            : null;
    }

    public async Task UpdateAsync(string toolId, JsonObject values, CancellationToken cancellationToken = default)
    {
        if (!_snapshots.TryGetValue(toolId, out var snapshot))
        {
            return;
        }

        var updatedSnapshot = new ToolSettingsSnapshot
        {
            Definition = snapshot.Definition,
            Values = CloneValues(values)
        };

        _snapshots[toolId] = updatedSnapshot;
        await Task.Run(() => _store.SaveValues(toolId, values), cancellationToken).ConfigureAwait(false);
        OnSettingsChanged(updatedSnapshot);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var toolId = Path.GetFileNameWithoutExtension(e.Name);
        if (string.IsNullOrEmpty(toolId))
        {
            return;
        }

        if (!_snapshots.TryGetValue(toolId, out var snapshot))
        {
            return;
        }

        try
        {
            var updatedValues = _store.LoadValues(snapshot.Definition);
            var updatedSnapshot = new ToolSettingsSnapshot
            {
                Definition = snapshot.Definition,
                Values = updatedValues
            };

            _snapshots[toolId] = updatedSnapshot;
            OnSettingsChanged(updatedSnapshot);
        }
        catch
        {
            // Ignore transient read issues.
        }
    }

    private void OnSettingsChanged(ToolSettingsSnapshot snapshot)
    {
        SettingsChanged?.Invoke(this, new ToolSettingsChangedEventArgs(new ToolSettingsSnapshot
        {
            Definition = snapshot.Definition,
            Values = CloneValues(snapshot.Values)
        }));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _watcher.Dispose();
        _disposed = true;
    }

    public static ToolSettingsService CreateDefault()
    {
        return new ToolSettingsService();
    }

    private static JsonObject CloneValues(JsonObject source)
    {
        if (JsonNode.Parse(source.ToJsonString()) is JsonObject clone)
        {
            return clone;
        }

        return new JsonObject();
    }
}
