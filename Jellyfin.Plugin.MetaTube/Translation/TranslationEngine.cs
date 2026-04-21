using Newtonsoft.Json;

namespace Jellyfin.Plugin.MetaTube.Models
{
    /// <summary>
    /// 翻译引擎类型枚举
    /// </summary>
    public enum TranslationEngine
    {
        Disabled,
        Google,
        Baidu,
        OpenAi,
        DeepLX // 🚀 新增：支持 DeepLX 协议
    }

    /// <summary>
    /// DeepLX API 响应模型
    /// 用于解析类似 {"code":200, "data":"翻译后的文字"} 的结构
    /// </summary>
    public class DeepLXResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        // 部分 DeepLX 实现可能包含 alternatives，这里可选
        [JsonProperty("alternatives")]
        public string[] Alternatives { get; set; }
    }
}
