using System.ComponentModel;

namespace Jellyfin.Plugin.MetaTube.Translation;

public enum TranslationEngine
{
    [Description("Baidu")]
    Baidu,

    [Description("Google")]
    Google,

    [Description("Google (Free)")]
    GoogleFree,

    [Description("DeepL")]
    DeepL,

    [Description("DeepLX (Direct)")]
    DeepLX, // 新增：直接对接 DeepLX 接口

    [Description("OpenAI")]
    OpenAi
}
