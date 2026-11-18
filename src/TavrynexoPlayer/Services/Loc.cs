using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Markup;

namespace TavrynexoPlayer.Services;

public sealed record LanguageInfo(string Code, string Display);

/// <summary>Runtime localization: loads Languages/&lt;code&gt;.json (embedded) and notifies bindings on change.</summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    public static readonly IReadOnlyList<LanguageInfo> Languages = new LanguageInfo[]
    {
        new("ar", "Arabic (العربية)"),
        new("bs", "Bosanski"),
        new("ca", "Català"),
        new("cs", "Čeština"),
        new("da", "Dansk"),
        new("de", "Deutsch"),
        new("en", "English"),
        new("en-GB", "English (British)"),
        new("es", "Español"),
        new("eu", "Euskara"),
        new("fr", "Français"),
        new("gl", "Galego"),
        new("hr", "Hrvatski"),
        new("id", "Indonesian"),
        new("it", "Italiano"),
        new("ko", "Korean (한국어)"),
        new("lt", "Lietuvių"),
        new("hu", "Magyar"),
        new("ms", "Malay"),
        new("nl", "Nederlands"),
        new("pl", "Polski"),
        new("pt", "Português (Portugal)"),
        new("pt-BR", "Português Brasileiro"),
        new("ro", "Română"),
        new("sk", "Slovenčina"),
        new("sl", "Slovenski"),
        new("fi", "Suomi"),
        new("sv", "Svenska"),
        new("th", "Thai (ไทย)"),
        new("tr", "Türkçe"),
        new("vi", "Vietnamese (Tiếng Việt)"),
        new("be", "Беларуская"),
        new("ru", "Русский"),
        new("tt", "Татар"),
        new("uk", "Українська"),
        new("hy", "Հայերեն"),
        new("he", "עברית"),
        new("bn", "বাংলা"),
        new("zh-CN", "中文(简体)"),
        new("ja", "日本語"),
        new("zh-TW", "繁體中文"),
    };

    private Dictionary<string, string> _current = new();
    private readonly Dictionary<string, string> _english;

    private Loc()
    {
        _english = Load("en");
        _current = _english;
    }

    public string Code { get; private set; } = "en";
    public bool IsRtl => Code is "ar" or "he";

    public string this[string key] =>
        _current.TryGetValue(key, out var v) ? v :
        _english.TryGetValue(key, out var e) ? e : key;

    public static string T(string key) => Instance[key];

    /// <summary>Text without the trailing ellipsis, for titles.</summary>
    public static string Plain(string key) => Instance[key].TrimEnd('…', '.', ' ');

    public static string F(string key, params object[] args)
    {
        try { return string.Format(CultureInfo.CurrentCulture, Instance[key], args); }
        catch (FormatException) { return Instance[key]; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? LanguageChanged;

    public void SetLanguage(string code)
    {
        if (!Languages.Any(l => l.Code == code)) code = "en";
        Code = code;
        _current = code == "en" ? _english : Load(code);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRtl)));
        LanguageChanged?.Invoke();
    }

    /// <summary>Best match for the Windows display language.</summary>
    public static string DetectSystemLanguage()
    {
        var name = CultureInfo.CurrentUICulture.Name;
        var exact = Languages.FirstOrDefault(l => string.Equals(l.Code, name, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact.Code;

        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return name.Contains("TW", StringComparison.OrdinalIgnoreCase) || name.Contains("HK", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("MO", StringComparison.OrdinalIgnoreCase) || name.Contains("Hant", StringComparison.OrdinalIgnoreCase)
                ? "zh-TW" : "zh-CN";
        if (name.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            return name.Contains("PT", StringComparison.OrdinalIgnoreCase) ? "pt" : "pt-BR";
        if (name.StartsWith("nb", StringComparison.OrdinalIgnoreCase) || name.StartsWith("nn", StringComparison.OrdinalIgnoreCase)) return "da";

        var two = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var byTwo = Languages.FirstOrDefault(l => string.Equals(l.Code, two, StringComparison.OrdinalIgnoreCase));
        return byTwo?.Code ?? "en";
    }

    private static Dictionary<string, string> Load(string code)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Languages.{code}.json");
            if (stream != null)
                return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new();
        }
        catch { /* fall back to English */ }
        return new Dictionary<string, string>();
    }
}

/// <summary>XAML: Header="{local:Tr m_media}" – live-updating translated text.</summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class TrExtension : MarkupExtension
{
    public TrExtension() { }
    public TrExtension(string key) { Key = key; }

    public string Key { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]") { Source = Loc.Instance, Mode = BindingMode.OneWay };
        return binding.ProvideValue(serviceProvider);
    }
}
