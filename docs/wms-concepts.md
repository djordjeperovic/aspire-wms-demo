# WMS Concepts

This document explains warehouse management terminology and concepts used in this project.

## Core Concepts

### Product / SKU

A **Stock Keeping Unit (SKU)** is a unique identifier for a product. Each SKU has:
- Name and description
- Category
- Dimensions (length, width, height)
- Weight
- Reorder point (minimum stock level)

### Location Hierarchy

Warehouse locations are organized hierarchically:

```
Warehouse
└── Zone (e.g., "Receiving", "Storage-A", "Shipping")
    └── Aisle (e.g., "A01", "A02")
        └── Rack (e.g., "R01", "R02")
            └── Bin (e.g., "B01", "B02") ← Actual storage location
```

**Location Code Format**: `ZONE-AISLE-RACK-BIN` (e.g., `STOR-A01-R03-B02`)

### Inventory Item

The relationship between a Product and a Location with a quantity:
- Which product
- Where it's stored
- How many units

### Stock Movement

Every inventory change is recorded as a movement with:
- Movement type (receive, ship, adjust, transfer)
- Quantity (positive or negative)
- Reason code
- Timestamp
- User who performed it

## Inbound Operations

### Purchase Order (PO)

A request to a supplier for products:
- Supplier information
- Expected delivery date
- List of products and quantities expected

**PO States**: `Draft` → `Submitted` → `Partially Received` → `Received` → `Closed`

### Receiving

The process of accepting goods from a supplier:
1. Match delivered goods to PO
2. Check quantities (handle over/under delivery)
3. Quality inspection (if required)
4. Create receipt record

### Put-away

Moving received goods to storage locations:
1. System suggests optimal location
2. Worker moves product to location
3. Confirms put-away in system
4. Inventory is updated

## Outbound Operations

### Customer Order

A request from a customer for products:
- Customer information
- Shipping address
- List of products and quantities ordered
- Priority level

**Order States**: `Pending` → `Allocated` → `Picking` → `Packed` → `Shipped`

### Inventory Allocation

Reserving inventory for an order:
- Soft allocation: Reserve without physical movement
- Prevents overselling
- Released if order is cancelled

### Pick Task

Work assignment for warehouse workers:
- Which products to pick
- From which locations
- For which order
- Optimal pick path

**Pick Strategies**:
- **FIFO** (First In, First Out): Pick oldest stock first
- **FEFO** (First Expired, First Out): For perishables
- **Nearest**: Pick from closest location

### Packing

Preparing picked items for shipping:
1. Verify picked quantities
2. Select appropriate packaging
3. Add packing materials
4. Generate shipping label
5. Mark as packed

### Shipping

Final step before goods leave warehouse:
1. Assign to carrier
2. Generate tracking number
3. Load onto vehicle
4. Confirm shipment
5. Reduce inventory

## Key Metrics

| Metric | Description |
|--------|-------------|
| **Inventory Accuracy** | Physical count vs. system count |
| **Order Fill Rate** | Orders shipped complete / Total orders |
| **Pick Accuracy** | Correct picks / Total picks |
| **Receiving Cycle Time** | Time from arrival to put-away |
| **Order Cycle Time** | Time from order to shipment |
| **Inventory Turnover** | Cost of goods sold / Average inventory |

## Best Practices Implemented

1. **Real-time visibility**: All movements tracked immediately
2. **Location management**: Organized hierarchy for efficient picking
3. **Movement history**: Full audit trail for all changes
4. **Stock alerts**: Automatic low-stock notifications
5. **FIFO by default**: Ensures stock rotation
