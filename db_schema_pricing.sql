-- Selling-price calculator settings (rent / salary / 9-ending / profit slabs).
-- Applied automatically by DB.EnsurePricingSchema on first open of the Selling Price tab.

CREATE TABLE IF NOT EXISTS pricing_settings
(
    id INT NOT NULL PRIMARY KEY,
    monthly_rent DECIMAL(12,2) NOT NULL DEFAULT 30000.00,
    monthly_salary DECIMAL(12,2) NOT NULL DEFAULT 30000.00,
    expected_monthly_sales DECIMAL(12,2) NOT NULL DEFAULT 3000.00,
    discount_percent DECIMAL(6,2) NOT NULL DEFAULT 0.00,
    price_ending_digit TINYINT NOT NULL DEFAULT 9,
    total_transport_cost DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    total_parcel_quantity INT NOT NULL DEFAULT 1,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

INSERT IGNORE INTO pricing_settings
    (id, monthly_rent, monthly_salary, expected_monthly_sales, discount_percent, price_ending_digit,
     total_transport_cost, total_parcel_quantity)
VALUES
    (1, 30000.00, 30000.00, 3000.00, 0.00, 9, 0.00, 1);

CREATE TABLE IF NOT EXISTS pricing_profit_slabs
(
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    min_purchase_cost DECIMAL(12,2) NOT NULL DEFAULT 0,
    max_purchase_cost DECIMAL(12,2) NULL,
    margin_percent DECIMAL(6,2) NOT NULL DEFAULT 0,
    sort_order INT NOT NULL DEFAULT 0,
    INDEX ix_pricing_profit_slabs_sort (sort_order, min_purchase_cost)
);

-- Default slabs (also seeded by DB.EnsurePricingSchema):
-- 0-50, 50-100, 100-200, 200-300, 300-400, 400-500 @ 45%
-- then 500-1000 @ 40%, 1000-1500 @ 35%, 1500-2000 @ 30%, 2000+ @ 25%
