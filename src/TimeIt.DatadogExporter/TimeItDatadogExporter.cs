﻿using DatadogTestLogger.Vendors.Datadog.Trace;
using DatadogTestLogger.Vendors.Datadog.Trace.Ci;
using Spectre.Console;
using TimeIt.Common.Configuration;
using TimeIt.Common.Exporter;
using TimeIt.Common.Results;

namespace TimeIt.DatadogExporter;

public class TimeItDatadogExporter : IExporter
{
    private Config? _configuration;
    private readonly TestSession _testSession;
    private readonly DateTime _startDate;
    private TestModule? _testModule;

    public TimeItDatadogExporter()
    {
        _testSession = TestSession.GetOrCreate(Environment.CommandLine, Environment.CurrentDirectory, "time-it");
        _startDate = DateTime.UtcNow;
    }
    
    /// <inheritdoc />
    public string Name => "Datadog";

    /// <inheritdoc />
    public bool Enabled => _configuration?.EnableDatadog ?? true;

    /// <inheritdoc />
    public void SetConfiguration(Config configuration)
    {
        _configuration = configuration;
        _testModule ??= _testSession.CreateModule(_configuration?.FileName ?? "config_file", "time-it", typeof(TimeItDatadogExporter).Assembly.GetName().Version?.ToString() ?? "(unknown)", _startDate);
    }

    /// <inheritdoc />
    public void Export(IEnumerable<ScenarioResult> results)
    {
        var errors = false;
        var minStartDate = results.Select(r => r.Start).Min();
        _testModule ??= _testSession.CreateModule(_configuration?.FileName ?? "config_file", "time-it", typeof(TimeItDatadogExporter).Assembly.GetName().Version?.ToString() ?? "(unknown)", minStartDate);
        var testSuite = _testModule.GetOrCreateSuite("scenarios", minStartDate);
        try
        {
            foreach (var scenarioResult in results)
            {
                var test = testSuite.CreateTest(scenarioResult.Name, scenarioResult.Start);

                // Set benchmark metadata
                test.SetBenchmarkMetadata(new BenchmarkHostInfo
                {
                    OsVersion = Environment.OSVersion.VersionString,
                    ProcessorCount = Environment.ProcessorCount,
                    RuntimeVersion = FrameworkDescription.Instance.ProductVersion,
                }, new BenchmarkJobInfo
                {
                    Platform = FrameworkDescription.Instance.OSPlatform,
                    RuntimeName = FrameworkDescription.Instance.Name,
                });

                // Add duration benchmark data
                test.AddBenchmarkData(
                    BenchmarkMeasureType.Duration,
                    "Duration of a run",
                    BenchmarkDiscreteStats.GetFrom(scenarioResult.Durations.ToArray()));

                // Add metrics
                if (scenarioResult.MetricsData.TryGetValue("process.time_to_start_ms", out var timeToStart))
                {
                    var timeToStartArray = timeToStart.Select(v => v * 1000000).ToArray();
                    test.AddBenchmarkData(
                        BenchmarkMeasureType.ApplicationLaunch,
                        "Time expend in application startup",
                        BenchmarkDiscreteStats.GetFrom(timeToStartArray));
                }

                if (scenarioResult.MetricsData.TryGetValue("process.internal_duration_ms", out var internalDuration))
                {
                    var internalDurationArray = internalDuration.Select(v => v * 1000000).ToArray();
                    test.AddBenchmarkData(
                        BenchmarkMeasureType.RunTime,
                        "Time expend in application run",
                        BenchmarkDiscreteStats.GetFrom(internalDurationArray));
                }

                foreach (var metric in scenarioResult.Metrics)
                {
                    // Due to a backend limitation on big objects we only store metrics ending in
                    // .n, .mean, .max, .min and .std_dev
                    if (metric.Key.EndsWith(".n") ||
                        metric.Key.EndsWith(".mean") ||
                        metric.Key.EndsWith(".max") ||
                        metric.Key.EndsWith(".min") ||
                        metric.Key.EndsWith(".std_dev"))
                    {
                        test.SetTag($"metrics.{metric.Key}", metric.Value);
                    }
                }

                // Set Error
                if (!string.IsNullOrEmpty(scenarioResult.Error))
                {
                    test.SetErrorInfo("Time-It Error", scenarioResult.Error, null);
                }

                // Meta configuration
                test.SetTag("test.configuration.process_name", scenarioResult.ProcessName);
                test.SetTag("test.configuration.process_arguments", scenarioResult.ProcessArguments);
                test.SetTag("test.working_directory", scenarioResult.WorkingDirectory);
                foreach (var envVar in scenarioResult.EnvironmentVariables)
                {
                    test.SetTag($"test.environment_variables.{envVar.Key}", envVar.Value);
                }

                // Setting custom tags
                foreach (var tag in scenarioResult.Tags)
                {
                    test.SetTag(tag.Key, tag.Value);
                }

                // Close test
                test.Close(string.IsNullOrEmpty(scenarioResult.Error) ? TestStatus.Pass : TestStatus.Fail,
                    scenarioResult.Duration);
            }
        }
        catch (Exception ex)
        {
            errors = true;
            AnsiConsole.MarkupLine("[red]Error exporting to datadog:[/]");
            AnsiConsole.WriteException(ex);
        }
        finally
        {
            testSuite.Close();
            _testModule.Close();
            _testSession.Close(TestStatus.Pass);
        }

        if (!errors)
        {
            AnsiConsole.MarkupLine($"[lime]The Datadog exported ran successfully.[/]");
        }
    }
}