using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Products;

public sealed partial class ProductsViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Products";
    public override string Title => "Sản phẩm & Danh mục";
    public override string Subtitle => "Quản lý danh sách hàng hóa, mã vạch SKU, giá bán và phân loại";
    public override SymbolRegular Icon => SymbolRegular.Box24;
}
