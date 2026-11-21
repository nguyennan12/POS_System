using POS.Domain.Common;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Domain.StoreLayouts;

public class StoreLayoutCell : BaseEntity
{
    public Guid LayoutId { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public LayoutCellType CellType { get; private set; }
    public int TraversalCost { get; private set; }

    // Navigation
    public StoreLayout? Layout { get; private set; }

    private StoreLayoutCell() { } // EF Core

    public StoreLayoutCell(
        Guid layoutId,
        int x,
        int y,
        LayoutCellType cellType = LayoutCellType.Walkway,
        int traversalCost = 1,
        Guid? id = null) : base(id)
    {
        LayoutId = layoutId;
        X = x;
        Y = y;
        CellType = cellType;
        TraversalCost = traversalCost;
    }

    public void Update(LayoutCellType cellType, int traversalCost)
    {
        CellType = cellType;
        TraversalCost = traversalCost >= 1 ? traversalCost : 1;
    }
}
