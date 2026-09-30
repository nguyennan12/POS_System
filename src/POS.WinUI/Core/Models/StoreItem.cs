namespace POS.WinUI.Core.Models;


public class StoreItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayText => Name;
}
