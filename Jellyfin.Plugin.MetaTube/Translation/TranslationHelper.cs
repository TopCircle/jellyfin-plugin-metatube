using System.Collections.Specialized;
using System.Net.Http.Json;
using Jellyfin.Plugin.MetaTube.Configuration;
using Jellyfin.Plugin.MetaTube.Metadata;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.MetaTube.Translation;

public static class TranslationHelper
{
    private const string AutoLanguageCode = "auto";
    private const string JapaneseLanguageCode = "ja";

    private static readonly SemaphoreSlim Semaphore = new(1);
    private static readonly HttpClient HttpClient = new();

    private static PluginConfiguration Configuration => Plugin.Instance.Configuration;

    private static async Task<string> TranslateAsync(string q, string from, string to,
        CancellationToken cancellationToken)
    {
        int millisecondsDelay = 100;
        var nv = new NameValueCollection();

        // 如果是 DeepLX 引擎，执行独立逻辑，不走 ApiClient
        if (Configuration.TranslationEngine == TranslationEngine.DeepLX)
        {
            return await TranslateDeepLXDirect(q, to, cancellationToken);
        }

        switch (Configuration.TranslationEngine)
        {
            case TranslationEngine.Baidu:
                millisecondsDelay = 1000;
                nv.Add(new NameValueCollection
                {
                    { "baidu-app-id", Configuration.BaiduAppId },
                    { "baidu-app-key", Configuration.BaiduAppKey }
                });
                break;
            case TranslationEngine.Google:
                millisecondsDelay = 100;
                nv.Add(new NameValueCollection
                {
                    { "google-api-key", Configuration.GoogleApiKey },
                    { "google-api-url", Configuration.GoogleApiUrl }
                });
                break;
            case TranslationEngine.GoogleFree:
                millisecondsDelay = 100;
                nv.Add(new NameValueCollection());
                break;
            case TranslationEngine.DeepL:
                millisecondsDelay = 100;
                nv.Add(new NameValueCollection
                {
                    { "deepl-api-key", Configuration.DeepLApiKey },
                    { "deepl-api-url", Configuration.DeepLApiUrl }
                });
                break;
            case TranslationEngine.OpenAi:
                millisecondsDelay = 1000;
                nv.Add(new NameValueCollection
                {
                    { "openai-api-key", Configuration.OpenAiApiKey },
                    { "openai-api-url", Configuration.OpenAiApiUrl },
                    { "openai-model", Configuration.OpenAiModel }
                });
                break;
            default:
                throw new ArgumentException($"Invalid translation engine: {Configuration.TranslationEngine}");
        }

        await Semaphore.WaitAsync(cancellationToken);

        try
        {
            async Task<string> TranslateWithDelay()
            {
                await Task.Delay(millisecondsDelay, cancellationToken);
                return (await ApiClient
                    .TranslateAsync(q, from, to, Configuration.TranslationEngine.ToString(), nv, cancellationToken)
                    .ConfigureAwait(false)).TranslatedText;
            }

            return await RetryAsync(TranslateWithDelay, 5);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static async Task<string> TranslateDeepLXDirect(string q, string to, CancellationToken cancellationToken)
    {
        // 目标语言映射：DeepLX 通常需要大写的 ZH
        string targetLang = to.ToUpper().Contains("ZH") ? "ZH" : to.ToUpper();
        
        // 你的专用接口地址
        const string url = "https://api.deeplx.org/wdOnh8dZ4hAAQqe81i6e2RwOx1Ba5v7mNcIzaUlKFZU/translate";

        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            async Task<string> DoPost()
            {
                var payload = new { text = q, target_lang = targetLang };
                var response = await HttpClient.PostAsJsonAsync(url, payload, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<DeepLXResponse>(content);

                return result?.Code == 200 ? result.Data : q;
            }

            return await RetryAsync(DoPost, 3);
        }
        catch
        {
            return q; // 出错返回原文
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public static async Task TranslateAsync(MovieInfo m, string to, CancellationToken cancellationToken)
    {
        if (string.Equals(to, JapaneseLanguageCode, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"language not allowed: {to}");

        if (Configuration.TranslationMode.HasFlag(TranslationMode.Title) && !string.IsNullOrWhiteSpace(m.Title))
            m.Title = await TranslateAsync(m.Title, AutoLanguageCode, to, cancellationToken);

        if (Configuration.TranslationMode.HasFlag(TranslationMode.Summary) && !string.IsNullOrWhiteSpace(m.Summary))
            m.Summary = await TranslateAsync(m.Summary, AutoLanguageCode, to, cancellationToken);
    }

    private static async Task<T> RetryAsync<T>(Func<Task<T>> func, int retryCount)
    {
        while (true)
        {
            try
            {
                return await func();
            }
            catch when (--retryCount > 0)
            {
            }
        }
    }

    // 内部响应模型
    private class DeepLXResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("data")]
        public string Data { get; set; }
    }
}
