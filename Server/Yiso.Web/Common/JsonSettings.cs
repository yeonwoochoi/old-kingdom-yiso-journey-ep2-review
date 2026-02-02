using System.Text.Json;

namespace Yiso.Web.Common;

/// <summary>
/// JSON 직렬화 공통 설정
/// </summary>
public static class JsonSettings {
    public static readonly JsonSerializerOptions Default = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
