using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Nova.Observability.File;

public static class NovaFileLoggingExtensions
{
    public static ILoggingBuilder AddNovaFile(
        this ILoggingBuilder builder,
        Action<NovaFileLoggingOptions>
            configure)
    {
        if (builder == null)
            throw new ArgumentNullException(
                nameof(builder));

        if (configure == null)
            throw new ArgumentNullException(
                nameof(configure));

        var options =
            new NovaFileLoggingOptions();

        configure(
            options);

        builder.Services
            .AddSingleton<
                ILoggerProvider>(
                    new NovaFileLoggerProvider(
                        options));

        return builder;
    }
    public static ILoggingBuilder AddNovaFile(
    this ILoggingBuilder builder,
    IConfiguration configuration,
    string sectionName,
    Action<NovaFileLoggingOptions>? configure = null)
    {
        if (builder == null)
            throw new ArgumentNullException(
                nameof(builder));

        if (configuration == null)
            throw new ArgumentNullException(
                nameof(configuration));

        if (string.IsNullOrWhiteSpace(
                sectionName))
        {
            throw new ArgumentException(
                "Section name cannot be empty.",
                nameof(sectionName));
        }

        var options =
            new NovaFileLoggingOptions();

        ApplyConfiguration(
            configuration,
            sectionName,
            options);

        configure?.Invoke(
            options);

        if (!options.Enabled)
            return builder;

        builder.Services
            .AddSingleton<ILoggerProvider>(
                new NovaFileLoggerProvider(
                    options));

        return builder;
    }
    private static void ApplyConfiguration(
    IConfiguration configuration,
    string sectionName,
    NovaFileLoggingOptions options)
    {
        var section =
            configuration.GetSection(
                sectionName);

        options.Enabled =
            ReadBool(
                section["Enabled"],
                options.Enabled);

        options.DirectoryPath =
            ReadString(
                section["DirectoryPath"],
                options.DirectoryPath);

        options.FileNamePrefix =
            ReadString(
                section["FileNamePrefix"],
                options.FileNamePrefix);

        options.Format =
            ReadEnum(
                section["Format"],
                options.Format);

        options.MinimumLevel =
            ReadEnum(
                section["MinimumLevel"],
                options.MinimumLevel);

        options.QueueCapacity =
            ReadInt(
                section["QueueCapacity"],
                options.QueueCapacity);

        options.MaxFileSizeMegabytes =
            ReadInt(
                section["MaxFileSizeMegabytes"],
                options.MaxFileSizeMegabytes);

        options.RetentionDays =
            ReadInt(
                section["RetentionDays"],
                options.RetentionDays);

        options.ShutdownTimeoutMilliseconds =
            ReadInt(
                section["ShutdownTimeoutMilliseconds"],
                options.ShutdownTimeoutMilliseconds);
        options.UseLocalTime =
            ReadBool(
                section["UseLocalTime"],
                options.UseLocalTime);

        options.TimestampFormat =
            ReadString(
                section["TimestampFormat"],
                options.TimestampFormat);

        options.UseShortCategoryName =
            ReadBool(
                section["UseShortCategoryName"],
                options.UseShortCategoryName);

        options.IncludeTraceId =
            ReadBool(
                section["IncludeTraceId"],
                options.IncludeTraceId);

        options.TraceIdDisplayLength =
            ReadInt(
                section["TraceIdDisplayLength"],
                options.TraceIdDisplayLength);

        options.HighlightIdentifiers =
            ReadBool(
                section["HighlightIdentifiers"],
                options.HighlightIdentifiers);
    }

    private static string ReadString(
        string? value,
        string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value;
    }

    private static int ReadInt(
        string? value,
        int defaultValue)
    {
        return int.TryParse(
            value,
            out var parsed)
                ? parsed
                : defaultValue;
    }

    private static bool ReadBool(
        string? value,
        bool defaultValue)
    {
        return bool.TryParse(
            value,
            out var parsed)
                ? parsed
                : defaultValue;
    }

    private static TEnum ReadEnum<TEnum>(
        string? value,
        TEnum defaultValue)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(
            value,
            ignoreCase: true,
            out var parsed)
                ? parsed
                : defaultValue;
    }
}