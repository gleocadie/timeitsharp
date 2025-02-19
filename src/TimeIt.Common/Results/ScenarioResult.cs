﻿using System.Text.Json.Serialization;
using TimeIt.Common.Configuration;

namespace TimeIt.Common.Results;

public class ScenarioResult : Scenario
{
    [JsonPropertyName("start")]
    public DateTime Start { get; set; }

    [JsonPropertyName("end")]
    public DateTime End { get; set; }

    [JsonPropertyName("duration")]
    public TimeSpan Duration { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; }
    
    [JsonPropertyName("warmUpCount")]
    public int WarmUpCount { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
    
    [JsonPropertyName("data")]
    public List<DataPoint> Data { get; set; }
    
    [JsonPropertyName("durations")]
    public List<double> Durations { get; set; }

    [JsonPropertyName("outliers")]
    public List<double> Outliers { get; set; }
    
    [JsonPropertyName("mean")]
    public double Mean { get; set; }
    
    [JsonPropertyName("max")]
    public double Max { get; set; }

    [JsonPropertyName("min")]
    public double Min { get; set; }

    [JsonPropertyName("stdev")]
    public double Stdev { get; set; }

    [JsonPropertyName("stderr")]
    public double StdErr { get; set; }

    [JsonPropertyName("p99")]
    public double P99 { get; set; }

    [JsonPropertyName("p95")]
    public double P95 { get; set; }

    [JsonPropertyName("p90")]
    public double P90 { get; set; }

    [JsonPropertyName("metrics")]
    public Dictionary<string, double> Metrics { get; set; }
    
    [JsonPropertyName("metricsData")]
    public Dictionary<string, List<double>> MetricsData { get; set; }

    public ScenarioResult()
    {
        Error = string.Empty;
        Data = new List<DataPoint>();
        Durations = new List<double>();
        Outliers = new List<double>();
        Metrics = new Dictionary<string, double>();
        MetricsData = new Dictionary<string, List<double>>();
    }
}