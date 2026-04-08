using System.Text.Json;

namespace SV22T1020292.Shop.AppCodes;

/// <summary>
/// Cung cấp truy cập HttpContext, session (JSON) và cấu hình cho tầng Shop.
/// </summary>
public static class ApplicationContext
{
    private static IHttpContextAccessor? _httpContextAccessor;
    private static IWebHostEnvironment? _webHostEnvironment;
    private static IConfiguration? _configuration;

    private static readonly JsonSerializerOptions SessionJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Gọi một lần sau <see cref="WebApplication.Build"/> để gắn dịch vụ với ngữ cảnh HTTP.
    /// </summary>
    /// <param name="httpContextAccessor">Truy cập HttpContext theo request.</param>
    /// <param name="webHostEnvironment">Môi trường host (wwwroot, content root).</param>
    /// <param name="configuration">Cấu hình ứng dụng.</param>
    public static void Configure(
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment webHostEnvironment,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// HttpContext của request hiện tại (nếu có).
    /// </summary>
    /// <returns>HttpContext của request hiện tại (nếu có).</returns>
    /// </summary>
    public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;

    /// <summary>
    /// Môi trường web host.
    /// </summary>
    /// <returns>Môi trường web host.</returns>
    public static IWebHostEnvironment? WebHostEnvironment => _webHostEnvironment;

    /// <summary>
    /// Cấu hình ứng dụng.
    /// </summary>
    /// <returns>Cấu hình ứng dụng.</returns>
    public static IConfiguration? Configuration => _configuration;

    /// <summary>
    /// Đường dẫn vật lý tới wwwroot.
    /// </summary>
    /// <returns>Đường dẫn vật lý tới wwwroot.</returns>
    public static string WebRootPath => _webHostEnvironment?.WebRootPath ?? string.Empty;

    /// <summary>
    /// Ghi đối tượng vào session dưới dạng JSON.
    /// </summary>
    /// <param name="key">Khóa session.</param>
    /// <param name="value">Giá trị cần lưu.</param>
    public static void SetSessionData(string key, object value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, SessionJsonOptions);
            _httpContextAccessor?.HttpContext?.Session.SetString(key, json);
        }
        catch
        {
            // bỏ qua lỗi serialize / session
        }
    }

    /// <summary>Đọc và giải mã JSON từ session.</summary>
    /// <typeparam name="T">Kiểu dữ liệu mong muốn.</typeparam>
    /// <param name="key">Khóa session.</param>
    /// <returns>Đối tượng đã deserialize hoặc null.</returns>
    public static T? GetSessionData<T>(string key) where T : class
    {
        try
        {
            var json = _httpContextAccessor?.HttpContext?.Session.GetString(key);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<T>(json, SessionJsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
