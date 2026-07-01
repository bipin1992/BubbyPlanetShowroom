CREATE TABLE IF NOT EXISTS daily_cash_closing
(
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    closing_date DATE NOT NULL,

    opening_balance DECIMAL(12,2) NOT NULL DEFAULT 0,
    cash_sales DECIMAL(12,2) NOT NULL DEFAULT 0,
    other_cash_in DECIMAL(12,2) NOT NULL DEFAULT 0,
    cash_in_reason VARCHAR(300) NULL,

    shop_expense DECIMAL(12,2) NOT NULL DEFAULT 0,
    staff_advance DECIMAL(12,2) NOT NULL DEFAULT 0,
    vendor_payment DECIMAL(12,2) NOT NULL DEFAULT 0,
    bank_deposit DECIMAL(12,2) NOT NULL DEFAULT 0,
    refund_amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    other_cash_out DECIMAL(12,2) NOT NULL DEFAULT 0,
    cash_out_reason VARCHAR(300) NULL,

    counter_left_for_tomorrow DECIMAL(12,2) NOT NULL DEFAULT 0,
    cash_given_to_owner DECIMAL(12,2) NOT NULL DEFAULT 0,

    total_cash_in_hand DECIMAL(12,2) NOT NULL DEFAULT 0,
    total_cash_out DECIMAL(12,2) NOT NULL DEFAULT 0,
    available_before_closing DECIMAL(12,2) NOT NULL DEFAULT 0,
    expected_owner_cash DECIMAL(12,2) NOT NULL DEFAULT 0,
    difference_amount DECIMAL(12,2) NOT NULL DEFAULT 0,

    note VARCHAR(500) NULL,
    created_by_user VARCHAR(100) NOT NULL,
    created_by_role VARCHAR(50) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    UNIQUE KEY uq_daily_cash_closing_date (closing_date),
    INDEX ix_daily_cash_closing_created_at (created_at)
);

CREATE TABLE IF NOT EXISTS daily_cash_movements
(
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    closing_id INT NOT NULL,
    movement_date DATE NOT NULL,
    movement_type VARCHAR(10) NOT NULL,
    amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    reason VARCHAR(300) NOT NULL,
    created_by_user VARCHAR(100) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX ix_daily_cash_movements_closing_id (closing_id),
    INDEX ix_daily_cash_movements_date_type (movement_date, movement_type),
    CONSTRAINT fk_daily_cash_movements_closing
        FOREIGN KEY (closing_id) REFERENCES daily_cash_closing(id)
        ON DELETE CASCADE
);
