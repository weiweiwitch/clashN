﻿using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using ClashN.Tool;

namespace ClashN.Base;

/// <summary>
/// </summary>
public class HttpClientHelper
{
    private static HttpClientHelper? _httpClientHelper;
    
    private readonly HttpClient _httpClient = new(new HttpClientHandler
    {
        UseCookies = false
    });
    
    private int _progressPercentage = -1;

    /// <summary>
    /// </summary>
    private HttpClientHelper()
    {
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public static HttpClientHelper GetInstance()
    {
        if (_httpClientHelper != null)
        {
            return _httpClientHelper;
        }

        var httpClientHelper = new HttpClientHelper();
        _httpClientHelper = httpClientHelper;
        
        return httpClientHelper;
    }

    public async Task<string?> TryGetAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            var response = await _httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }

    public async Task<(string, HttpResponseHeaders)> GetAsync(HttpClient client, string url, CancellationToken token)
    {
        if (string.IsNullOrEmpty(url))
        {
            return (null, null);
        }

        var response = await client.GetAsync(url, token);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(string.Format("The request returned with HTTP status code {0}", response.StatusCode));
        }

        return (await response.Content.ReadAsStringAsync(), response.Headers);
    }

    public async Task PutAsync(string url, Dictionary<string, string> headers)
    {
        var myContent = Utils.ToJson(headers);
        var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
        var byteContent = new ByteArrayContent(buffer);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        await _httpClient.PutAsync(url, byteContent);
    }

    public async Task PatchAsync(string url, Dictionary<string, string> headers)
    {
        var myContent = Utils.ToJson(headers);
        var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
        var byteContent = new ByteArrayContent(buffer);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        await _httpClient.PatchAsync(url, byteContent);
    }

    public async Task DeleteAsync(string url)
    {
        await _httpClient.DeleteAsync(url);
    }

    public async Task DownloadFileAsync(HttpClient client, string url, string fileName, IProgress<double> progress,
        CancellationToken token)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url));
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentNullException(nameof(fileName));

        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);

        if (!response.IsSuccessStatusCode)
            throw new Exception(string.Format("The request returned with HTTP status code {0}", response.StatusCode));

        var total = response.Content.Headers.ContentLength.HasValue
            ? response.Content.Headers.ContentLength.Value
            : -1L;
        var canReportProgress = total != -1 && progress != null;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var file = File.Create(fileName);

        var totalRead = 0L;
        var buffer = new byte[1024 * 1024];
        var isMoreToRead = true;
        _progressPercentage = -1;

        do
        {
            token.ThrowIfCancellationRequested();

            var read = await stream.ReadAsync(buffer, 0, buffer.Length, token);

            if (read == 0)
            {
                isMoreToRead = false;
            }
            else
            {
                var data = new byte[read];
                buffer.ToList().CopyTo(0, data, 0, read);

                // TODO: put here the code to write the file to disk
                file.Write(data, 0, read);

                totalRead += read;

                if (canReportProgress)
                {
                    var percent = Convert.ToInt32((totalRead * 1d) / (total * 1d) * 100);
                    if (_progressPercentage != percent && percent % 10 == 0)
                    {
                        _progressPercentage = percent;
                        progress?.Report(percent);
                    }
                }
            }
        } while (isMoreToRead);

        if (canReportProgress)
            progress?.Report(101);
    }
}