using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Nova.Observability.File;

internal sealed class NovaFileLogWriter :
    IDisposable
{
    private readonly NovaFileLoggingOptions
        _options;

    private StreamWriter? _writer;

    private DateTime _currentDate;

    private int _fileSequence;

    private readonly JsonSerializerOptions
        _jsonOptions;

    internal NovaFileLogWriter(
        NovaFileLoggingOptions options)
    {
        _options = options;

        _jsonOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase
            };
    }

    internal void Write(
        NovaFileLogEntry entry)
    {
        EnsureWriter(
            entry.TimestampUtc.UtcDateTime);

        if (_writer == null)
            return;

        if (_options.Format ==
            NovaFileLogFormat.JsonLines)
        {
            var json =
                JsonSerializer.Serialize(
                    entry,
                    _jsonOptions);

            _writer.WriteLine(
                json);
        }
        else
        {
            WriteText(
                entry);
        }

        /*
         * Business thread zaten burada değil.
         * Background writer thread'inde olduğumuz
         * için ilk MVP'de güvenilirlik adına
         * her kayıtta flush ediyoruz.
         */
        _writer.Flush();
    }

    public void Dispose()
    {
        try
        {
            _writer?.Dispose();
        }
        catch
        {
        }

        _writer = null;
    }

    private void EnsureWriter(
        DateTime timestampUtc)
    {
        var date =
            timestampUtc.Date;

        if (_writer != null &&
            _currentDate == date &&
            !CurrentFileReachedLimit())
        {
            return;
        }

        CloseCurrentWriter();

        if (_currentDate != date)
        {
            _currentDate =
                date;

            _fileSequence =
                FindNextSequence(
                    date);
        }
        else
        {
            _fileSequence++;
        }

        Directory.CreateDirectory(
            _options.DirectoryPath);

        var path =
            BuildFilePath(
                date,
                _fileSequence);

        var stream =
            new FileStream(
                path,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite);

        _writer =
            new StreamWriter(
                stream,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier:
                        false));

        CleanupOldFiles();
    }

    private void WriteText(
        NovaFileLogEntry entry)
    {
        var timestamp =
            _options.UseLocalTime
                ? entry.TimestampUtc.ToLocalTime()
                : entry.TimestampUtc;

        var category =
            FormatCategory(
                entry.Category);

        var level =
            FormatLevel(
                entry.Level);

        _writer!.Write(
            timestamp.ToString(
                _options.TimestampFormat));

        _writer.Write(" | ");

        _writer.Write(level);

        _writer.Write(" | ");

        _writer.Write(category);

        if (_options.HighlightIdentifiers)
        {
            var identifierSummary =
                BuildIdentifierSummary(
                    entry);

            if (!string.IsNullOrWhiteSpace(
                    identifierSummary))
            {
                _writer.Write(" | ");

                _writer.Write(
                    identifierSummary);
            }
        }

        _writer.Write(" | ");

        _writer.Write(
            entry.Message);

        if (!string.IsNullOrWhiteSpace(
                entry.ExceptionMessage))
        {
            _writer.Write(
                " | Exception=");

            _writer.Write(
                entry.ExceptionMessage);
        }

        if (_options.IncludeTraceId &&
            !string.IsNullOrWhiteSpace(
                entry.TraceId))
        {
            _writer.Write(
                " | Trace=");

            _writer.Write(
                Shorten(
                    entry.TraceId,
                    _options.TraceIdDisplayLength));
        }

        _writer.WriteLine();
    }

    private bool CurrentFileReachedLimit()
    {
        if (_writer == null)
            return false;

        var maxBytes =
            (long)_options
                .MaxFileSizeMegabytes *
            1024L *
            1024L;

        if (maxBytes <= 0)
            return false;

        try
        {
            return _writer
                .BaseStream
                .Length >= maxBytes;
        }
        catch
        {
            return false;
        }
    }

    private string BuildFilePath(
        DateTime date,
        int sequence)
    {
        var extension =
            _options.Format ==
            NovaFileLogFormat.JsonLines
                ? "jsonl"
                : "log";

        var name =
            string.Format(
                "{0}-{1:yyyyMMdd}-{2:000}.{3}",
                _options.FileNamePrefix,
                date,
                sequence,
                extension);

        return Path.Combine(
            _options.DirectoryPath,
            name);
    }

    private int FindNextSequence(
        DateTime date)
    {
        try
        {
            if (!Directory.Exists(
                    _options.DirectoryPath))
            {
                return 0;
            }

            var extension =
                _options.Format ==
                NovaFileLogFormat.JsonLines
                    ? "jsonl"
                    : "log";

            var prefix =
                string.Format(
                    "{0}-{1:yyyyMMdd}-",
                    _options.FileNamePrefix,
                    date);

            var files =
                Directory.GetFiles(
                    _options.DirectoryPath,
                    prefix + "*." + extension);

            if (files.Length == 0)
                return 0;

            var latest =
                files
                    .OrderByDescending(
                        x => x)
                    .First();

            var fileName =
                Path.GetFileNameWithoutExtension(
                    latest);

            var sequenceText =
                fileName.Substring(
                    fileName.Length - 3);

            int sequence;

            if (!int.TryParse(
                    sequenceText,
                    out sequence))
            {
                return 0;
            }

            return sequence;
        }
        catch
        {
            return 0;
        }
    }

    private void CleanupOldFiles()
    {
        if (_options.RetentionDays <= 0)
            return;

        try
        {
            var threshold =
                DateTime.UtcNow
                    .Date
                    .AddDays(
                        -_options.RetentionDays);

            var files =
                Directory.GetFiles(
                    _options.DirectoryPath,
                    _options.FileNamePrefix +
                    "-*.*");

            foreach (var file in files)
            {
                try
                {
                    if (System.IO.File.GetLastWriteTimeUtc(
                            file) < threshold)
                    {
                        System.IO.File.Delete(
                            file);
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private void CloseCurrentWriter()
    {
        try
        {
            _writer?.Dispose();
        }
        catch
        {
        }

        _writer = null;
    }
    private string FormatCategory(
    string? category)
    {
        if (string.IsNullOrWhiteSpace(
                category))
        {
            return "-";
        }

        if (!_options.UseShortCategoryName)
            return category;

        var lastDot =
            category.LastIndexOf('.');

        if (lastDot < 0 ||
            lastDot == category.Length - 1)
        {
            return category;
        }

        return category.Substring(
            lastDot + 1);
    }
    private static string FormatLevel(
    string? level)
    {
        switch (level)
        {
            case "Trace":
                return "TRC";

            case "Debug":
                return "DBG";

            case "Information":
                return "INF";

            case "Warning":
                return "WRN";

            case "Error":
                return "ERR";

            case "Critical":
                return "CRT";

            default:
                return level ?? "---";
        }
    }
    private static string? BuildIdentifierSummary(
    NovaFileLogEntry entry)
    {
        var parts =
            new List<string>();

        object? entityType;
        object? entityId;

        entry.Properties.TryGetValue(
            "nova.entity.type",
            out entityType);

        entry.Properties.TryGetValue(
            "nova.entity.id",
            out entityId);

        if (entityId != null)
        {
            if (entityType != null)
            {
                parts.Add(
                    "Entity=" +
                    entityType +
                    ":" +
                    entityId);
            }
            else
            {
                parts.Add(
                    "EntityId=" +
                    entityId);
            }
        }

        foreach (var property in
                 entry.Properties)
        {
            if (!IsIdentifierProperty(
                    property.Key))
            {
                continue;
            }

            if (string.Equals(
                    property.Key,
                    "nova.entity.id",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value == null)
                continue;

            parts.Add(
                property.Key +
                "=" +
                property.Value);
        }

        return parts.Count == 0
            ? null
            : string.Join(
                " | ",
                parts);
    }
    private static bool IsIdentifierProperty(
    string key)
    {
        if (string.IsNullOrWhiteSpace(
                key))
        {
            return false;
        }

        if (key.Equals(
                "TraceId",
                StringComparison.OrdinalIgnoreCase) ||
            key.Equals(
                "SpanId",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return key.EndsWith(
            "Id",
            StringComparison.OrdinalIgnoreCase);
    }
    private static string Shorten(
    string value,
    int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (maxLength <= 0 ||
            value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(
            0,
            maxLength);
    }
}