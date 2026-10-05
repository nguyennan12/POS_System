namespace POS.Application.Abstractions.Excel;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ExcelColumnAttribute : Attribute
{
    public string Name { get; }
    public string[] Aliases { get; }
    public int Order { get; set; } = -1;
    public string? NumberFormat { get; set; }

    public ExcelColumnAttribute(string name, params string[] aliases)
    {
        Name = name;
        Aliases = aliases ?? Array.Empty<string>();
    }
}
