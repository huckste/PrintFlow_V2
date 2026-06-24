using System.Text.Json.Serialization;

namespace PrintFlow_V2.Models;

public class PathDesc
{
    public required string Name { get; set; }
    public string Path { get; set; } = string.Empty;
    public required string Desc { get; set; }

    [JsonIgnore]
    public string ProdRelative { get; init; } = string.Empty;

    [JsonIgnore]
    public string TestRelative { get; init; } = string.Empty;
}
