sql query

ALTER TABLE inv_order_details
ADD COLUMN selling_price DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER qty,

ADD COLUMN gross_amount DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER selling_price,

ADD COLUMN discount_amount DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER discount_percent,

ADD COLUMN taxable_amount DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER discount_amount,

ADD COLUMN gst_amount DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER taxable_amount,

ADD COLUMN net_amount DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER gst_amount;



=========================
migrate old data into new style

UPDATE inv_order_details
SET
    selling_price = price,
    gross_amount = (price * qty),
    discount_amount =
        ((price * qty) * IFNULL(discount_percent,0) / 100),
    taxable_amount = subtotal,
    gst_amount = tax,
    net_amount = total;
	
========================================

//ALTER TABLE inv_customers
//ADD COLUMN reward_reveal_date DATE NULL AFTER status,
//ADD COLUMN total_purchase FLOAT(10,2) NOT NULL DEFAULT 0 AFTER reward_reveal_date;

//UPDATE inv_customers
//SET reward_reveal_date = date_added
//WHERE reward_reveal_date IS NULL;

ALTER TABLE inv_customers
ADD COLUMN reward_last_order_id INT NULL
AFTER phone;

UPDATE inv_customers
SET reward_last_order_id = 0
WHERE reward_last_order_id IS NULL;
==================================

CREATE TABLE inv_reward_discount_rules
(
    id INT AUTO_INCREMENT PRIMARY KEY,

    min_purchase DECIMAL(12,2) NOT NULL,

    discount_percent DECIMAL(5,2) NOT NULL,

    is_active TINYINT(1) NOT NULL DEFAULT 1,

    created_on DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO inv_reward_discount_rules
(
    min_purchase,
    discount_percent
)
VALUES
(1000, 5),
(5000, 10),
(10000, 15),
(15000, 20);
======================================

//SELECT
//    COUNT(*) AS total_customers_to_update
//FROM inv_customers
//WHERE reward_reveal_date IS NULL;


=======================================
ALTER TABLE inv_order_details
DROP COLUMN price,
DROP COLUMN subtotal,
DROP COLUMN discount,
DROP COLUMN tax,
DROP COLUMN total;