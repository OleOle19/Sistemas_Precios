namespace SistemasPrecios.Api.Services;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; set; } = "storage";
}
