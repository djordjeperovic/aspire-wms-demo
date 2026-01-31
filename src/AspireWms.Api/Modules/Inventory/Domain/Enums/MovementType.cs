namespace AspireWms.Api.Modules.Inventory.Domain.Enums;

/// <summary>
/// Types of stock movements for audit trail.
/// </summary>
public enum MovementType
{
    /// <summary>Initial stock setup</summary>
    Initial = 0,

    /// <summary>Stock received from purchase order</summary>
    Received = 1,

    /// <summary>Stock picked for order</summary>
    Picked = 2,

    /// <summary>Manual adjustment (increase)</summary>
    AdjustmentIn = 3,

    /// <summary>Manual adjustment (decrease)</summary>
    AdjustmentOut = 4,

    /// <summary>Stock transferred between locations</summary>
    Transfer = 5,

    /// <summary>Stock returned from customer</summary>
    Return = 6,

    /// <summary>Stock damaged or lost</summary>
    Damaged = 7,

    /// <summary>Stock count correction</summary>
    CountCorrection = 8
}
