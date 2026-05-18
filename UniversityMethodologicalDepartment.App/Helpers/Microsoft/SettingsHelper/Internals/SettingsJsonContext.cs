using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;

namespace Helpers.Microsoft;

[JsonSourceGenerationOptions(WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Dictionary<string, System.Text.Json.JsonElement>))]
[JsonSerializable(typeof(SettingsHelper))]
[JsonSerializable(typeof(ElementTheme))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<string>))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}