-- Selling-price calculator settings (rent / salary / discount / 9-ending / profit slabs).
-- Applied automatically by DB.EnsurePricingSchema on first open of the Selling Price tab.

CREATE TABLE IF NOT EXISTS pricing_settings
(
    id INT NOT NULL PRIMARY KEY,
    monthly_rent DECIMAL(12,2) NOT NULL DEFAULT 30000.00,
    monthly_salary DECIMAL(12,2) NOT NULL DEFAULT 30000.00,
    expected_monthly_sales DECIMAL(12,2) NOT NULL DEFAULT 3000.00,
    discount_percent DECIMAL(6,2) NOT NULL DEFAULT 15.00,
    price_ending_digit TINYINT NOT NULL DEFAULT 9,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

INSERT IGNORE INTO pricing_settings
    (id, monthly_rent, monthly_salary, expected_monthly_sales, discount_percent, price_ending_digit)
VALUES
    (1, 30000.00, 30000.00, 3000.00, 15.00, 9);

CREATE TABLE IF NOT EXISTS pricing_profit_slabs
(
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    min_purchase_cost DECIMAL(12,2) NOT NULL DEFAULT 0,
    max_purchase_cost DECIMAL(12,2) NULL,
    margin_percent DECIMAL(6,2) NOT NULL DEFAULT 0,
    sort_order INT NOT NULL DEFAULT 0,
    INDEX ix_pricing_profit_slabs_sort (sort_order, min_purchase_cost)
);
