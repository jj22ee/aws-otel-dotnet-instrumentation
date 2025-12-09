// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;
using Amazon.S3;
using Microsoft.AspNetCore.Mvc;

namespace integration_test_app.Controllers;

[ApiController]
[Route("[controller]")]
public class AppController : ControllerBase
{
    private static readonly Meter meter = new Meter("dice-lib");
    private static readonly Counter<int> counter = meter.CreateCounter<int>("sum.counter", "ms", "test_sum_description");
    private static readonly Histogram<double> histogram = meter.CreateHistogram<double>("histogram.counter", "ms", "test_histogram_description");
    private static readonly Random random = new Random();
    
    private readonly AmazonS3Client s3Client = new AmazonS3Client();
    private readonly HttpClient httpClient = new HttpClient();

    [HttpGet]
    [Route("/outgoing-http-call")]
    public string OutgoingHttp()
    {
        _ = this.httpClient.GetAsync("https://aws.amazon.com").Result;

        return this.GetTraceId();
    }

    [HttpGet]
    [Route("/aws-sdk-call")]
    public string AWSSDKCall()
    {
        _ = this.s3Client.ListBucketsAsync().Result;

        return this.GetTraceId();
    }

    [HttpGet]
    [Route("/")]
    public string Default()
    {
        return "Application started!";
    }

    [HttpGet]
    [Route("/sum")]
    public string Sum()
    {
        counter.Add(1, new KeyValuePair<string, object>("sumAttr", "sumValue"));
        return "/sum endpoint";
    }

    [HttpGet]
    [Route("/histogram")]
    public string Histogram()
    {
        double val = GetRandomNumber(0, 5);
        histogram.Record(val);
        return $"/histogram endpoint {val}";
    }

    private static double GetRandomNumber(double min, double max)
    {
        return random.NextDouble() * (max - min) + min;
    }

    private string GetTraceId()
    {
        var traceId = Activity.Current.TraceId.ToHexString();
        var version = "1";
        var epoch = traceId.Substring(0, 8);
        var random = traceId.Substring(8);
        return "{" + "\"traceId\"" + ": " + "\"" + version + "-" + epoch + "-" + random + "\"" + "}";
    }
}
