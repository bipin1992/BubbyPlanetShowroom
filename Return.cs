using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Return : UserControl
    {
        private const string StoreName = "BubbyPlanet";
        private const string StoreAddressLine1 = "Daudnagar Branch, Aurangabad";
        private const string StoreAddressLine2 = "Bihar - 824143";
        private const string StorePhone = "7870828400";
        private static readonly Color ResetEnabledColor = Color.FromArgb(217, 119, 6);
        private static readonly Color DisabledButtonColor = Color.FromArgb(156, 163, 175);
        TextBox txtOrderId = new TextBox();
        Button btnSearch = new Button();
        Button btnReset = new Button();
        Button btnProcess = new Button();

        Label lblCustomer = new Label();
        Label lblPhone = new Label();
        Label lblDate = new Label();
        Label lblSubtotal = new Label();
        Label lblTax = new Label();
        Label lblTotal = new Label();
        Label lblRefund = new Label();

        DataGridView grid = new DataGridView();

        PrintDocument printDoc = new PrintDocument();
        string receiptText = "";
        private bool isProcessingReturn = false;

        private decimal Round2(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        public Return()
        {
            InitializeUI();
            printDoc.PrintPage += PrintDoc_PrintPage;
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(241, 245, 249);

            Font labelFont = new Font("Segoe UI", 10, FontStyle.Bold);
            Font valueFont = new Font("Segoe UI", 10);

            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 64;
            topPanel.Padding = new Padding(10);
            topPanel.BackColor = Color.White;

            Label lblOrder = new Label();
            lblOrder.Text = "Order ID";
            lblOrder.Font = labelFont;
            lblOrder.AutoSize = true;
            lblOrder.Top = 20;
            lblOrder.Left = 10;

            txtOrderId.Width = 150;
            txtOrderId.Left = 90;
            txtOrderId.Top = 15;
            txtOrderId.Height = 32;
            txtOrderId.BorderStyle = BorderStyle.FixedSingle;

            btnSearch.Text = "Search";
            btnSearch.Left = 260;
            btnSearch.Top = 14;
            btnSearch.Width = 100;
            btnSearch.Height = 30;
            btnSearch.BackColor = Color.FromArgb(0, 120, 215);
            btnSearch.ForeColor = Color.White;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += BtnSearch_Click;

            btnReset = new Button();
            btnReset.Text = "Reset";
            btnReset.Left = 370;
            btnReset.Top = 14;
            btnReset.Width = 100;
            btnReset.Height = 30;
            btnReset.BackColor = DisabledButtonColor;
            btnReset.ForeColor = Color.White;
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Enabled = false;
            btnReset.Click += BtnReset_Click;

            topPanel.Controls.Add(lblOrder);
            topPanel.Controls.Add(txtOrderId);
            topPanel.Controls.Add(btnSearch);
            topPanel.Controls.Add(btnReset);

            Panel infoPanel = new Panel();
            infoPanel.Dock = DockStyle.Top;
            infoPanel.Height = 100;
            infoPanel.Padding = new Padding(10);
            infoPanel.BackColor = Color.White;

            lblCustomer.Font = labelFont;
            lblCustomer.Left = 10;
            lblCustomer.Top = 10;
            lblCustomer.Width = 350;

            lblPhone.Font = labelFont;
            lblPhone.Left = 400;
            lblPhone.Top = 10;
            lblPhone.Width = 250;

            lblDate.Font = valueFont;
            lblDate.Left = 10;
            lblDate.Top = 40;
            lblDate.Width = 350;

            lblSubtotal.Font = valueFont;
            lblSubtotal.Left = 10;
            lblSubtotal.Top = 70;
            lblSubtotal.Width = 200;

            lblTax.Font = valueFont;
            lblTax.Left = 250;
            lblTax.Top = 70;
            lblTax.Width = 200;

            lblTotal.Font = valueFont;
            lblTotal.Left = 500;
            lblTotal.Top = 70;
            lblTotal.Width = 200;

            infoPanel.Controls.Add(lblCustomer);
            infoPanel.Controls.Add(lblPhone);
            infoPanel.Controls.Add(lblDate);
            infoPanel.Controls.Add(lblSubtotal);
            infoPanel.Controls.Add(lblTax);
            infoPanel.Controls.Add(lblTotal);

            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.AllowUserToAddRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.ScrollBars = ScrollBars.Vertical;
            grid.RowTemplate.Height = 32;
            grid.ColumnHeadersHeight = 36;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.Gainsboro;

            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 120, 215);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.EnableHeadersVisualStyles = false;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            grid.CellEndEdit += Grid_CellEndEdit;

            Panel bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 80;
            bottomPanel.Padding = new Padding(10);
            bottomPanel.BackColor = Color.White;

            lblRefund.Text = "TOTAL REFUND : 0";
            lblRefund.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblRefund.Left = 10;
            lblRefund.Top = 10;
            lblRefund.AutoSize = true;

            btnProcess.Text = "Process Return";
            btnProcess.Width = 180;
            btnProcess.Height = 40;
            btnProcess.Left = 10;
            btnProcess.Top = 35;
            btnProcess.BackColor = Color.FromArgb(0, 120, 215);
            btnProcess.ForeColor = Color.White;
            btnProcess.FlatStyle = FlatStyle.Flat;
            btnProcess.FlatAppearance.BorderSize = 0;
            btnProcess.Enabled = false;
            btnProcess.Click += BtnProcess_Click;

            bottomPanel.Controls.Add(lblRefund);
            bottomPanel.Controls.Add(btnProcess);

            Controls.Add(grid);
            Controls.Add(bottomPanel);
            Controls.Add(infoPanel);
            Controls.Add(topPanel);

            //txtOrderId.TextChanged += TxtOrderId_TextChanged;

            this.Load += (s, e) => txtOrderId.Focus();
            txtOrderId.KeyDown += txtOrderId_KeyDown;
            txtOrderId.KeyPress += TxtOrderId_KeyPress;
        }

        private void TxtOrderId_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void txtOrderId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // beep/extra enter rokne ke liye

                BtnSearch_Click(sender, e); // 🔥 direct call
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            if (txtOrderId.Text == "")
            {
                MessageBox.Show("Enter Order ID");
                return;
            }
            if (!int.TryParse(txtOrderId.Text.Trim(), out int parsedOrderId) || parsedOrderId <= 0)
            {
                MessageBox.Show("Enter valid numeric Order ID");
                return;
            }

            try
            {
                using (MySqlConnection con = DB.GetConnection())
                {
                    con.Open();

                    string orderQuery =
                    @"SELECT 
                        o.subtotal AS subtotal,
                        o.total_tax AS tax,
                        o.grand_total AS grand_total,
                        o.date_added,
                        c.first_name,
                        c.sur_name,
                        c.phone
                    FROM inv_orders o
                    JOIN inv_customers c ON c.id = o.customer_id
                    WHERE o.id = @orderId";

                    MySqlCommand cmd = new MySqlCommand(orderQuery, con);
                    cmd.Parameters.AddWithValue("@orderId", parsedOrderId);

                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            DateTime orderDate = Convert.ToDateTime(dr["date_added"]);
                            string customerName = dr["first_name"] + " " + dr["sur_name"];

                            lblCustomer.Text = "Customer : " + customerName;
                            lblPhone.Text = "Phone : " + dr["phone"].ToString();
                            lblDate.Text = "Date : " + orderDate.ToString("dd-MM-yyyy HH:mm:ss");

                            decimal subtotal = Convert.ToDecimal(dr["subtotal"].ToString());
                            decimal tax = Convert.ToDecimal(dr["tax"].ToString());
                            decimal total = Convert.ToDecimal(dr["grand_total"].ToString());

                            lblSubtotal.Text = "Subtotal : " + subtotal.ToString("0.00");
                            lblTax.Text = "Tax : " + tax.ToString("0.00");
                            lblTotal.Text = "Total : " + total.ToString("0.00");

                            if (!IsReturnAllowedWithin7Days(orderDate))
                            {
                                MessageBox.Show("Return allowed only within 7 days. 8th day se return allowed nahi hai.");
                                grid.Enabled = false;
                                btnProcess.Enabled = false;
                                btnReset.Enabled = true;
                                btnReset.BackColor = ResetEnabledColor;
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Order not found");
                            return;
                        }
                    }

                    // Load Items
                    string itemQuery =
                    @"SELECT
                        od.id,
                        od.item_id,
                        i.item_code,
                        i.item_name,
                        od.qty,
                        IFNULL(od.return_qty,0) return_qty,

                        od.selling_price,
                        od.gross_amount,

                        od.discount_percent,
                        od.discount_amount,

                        od.taxable_amount,
                        od.gst_amount,
                        od.net_amount
                    FROM inv_order_details od
                    JOIN inv_items_master i ON i.id = od.item_id
                    WHERE od.order_id = @orderId";

                    MySqlDataAdapter da = new MySqlDataAdapter(itemQuery, con);
                    da.SelectCommand.Parameters.AddWithValue("@orderId", parsedOrderId);

                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    bool hasReturnableItem = false;
                    foreach (DataRow drItem in dt.Rows)
                    {
                        int qty = Convert.ToInt32(drItem["qty"]);
                        int returned = Convert.ToInt32(drItem["return_qty"]);
                        if (qty - returned > 0)
                        {
                            hasReturnableItem = true;
                            break;
                        }
                    }
                    if (!hasReturnableItem)
                    {
                        MessageBox.Show("All items are already fully returned for this order.");
                        grid.DataSource = null;
                        btnProcess.Enabled = false;
                        btnReset.Enabled = true;
                        btnReset.BackColor = ResetEnabledColor;
                        return;
                    }

                    grid.DataSource = dt;

                    grid.Columns["discount_percent"].HeaderText = "Disc %";

                    grid.Columns["discount_percent"].AutoSizeMode =
                        DataGridViewAutoSizeColumnMode.None;

                    grid.Columns["discount_percent"].Width = 80;

                    grid.Columns["discount_percent"].HeaderCell.Style.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;

                    grid.Columns["discount_percent"].DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;

                    // Hide internal columns
                    if (grid.Columns.Contains("id"))
                        grid.Columns["id"].Visible = false;

                    if (grid.Columns.Contains("item_id"))
                        grid.Columns["item_id"].Visible = false;

                    if (grid.Columns.Contains("item_code"))
                        grid.Columns["item_code"].Visible = false;

                    if (grid.Columns.Contains("gross_amount"))
                        grid.Columns["gross_amount"].Visible = false;

                    if (grid.Columns.Contains("discount_amount"))
                        grid.Columns["discount_amount"].Visible = false;

                    if (grid.Columns.Contains("taxable_amount"))
                        grid.Columns["taxable_amount"].Visible = false;

                    // Column captions
                    grid.Columns["item_name"].HeaderText = "Item";
                    grid.Columns["qty"].HeaderText = "Qty";
                    grid.Columns["return_qty"].HeaderText = "Returned";
                    grid.Columns["selling_price"].HeaderText = "Price";
                    grid.Columns["discount_percent"].HeaderText = "Disc %";
                    grid.Columns["gst_amount"].HeaderText = "GST";
                    grid.Columns["net_amount"].HeaderText = "Net";

                    // Display order
                    grid.Columns["item_name"].DisplayIndex = 0;
                    grid.Columns["qty"].DisplayIndex = 1;
                    grid.Columns["return_qty"].DisplayIndex = 2;
                    grid.Columns["selling_price"].DisplayIndex = 3;
                    grid.Columns["discount_percent"].DisplayIndex = 4;
                    grid.Columns["gst_amount"].DisplayIndex = 5;
                    grid.Columns["net_amount"].DisplayIndex = 6;

                    // Formats
                    grid.Columns["selling_price"].DefaultCellStyle.Format = "0.00";
                    grid.Columns["gst_amount"].DefaultCellStyle.Format = "0.00";
                    grid.Columns["net_amount"].DefaultCellStyle.Format = "0.00";
                    grid.Columns["discount_percent"].DefaultCellStyle.Format = "0.##";

                    // Widths
                    grid.Columns["item_name"].Width = 260;
                    grid.Columns["qty"].Width = 70;
                    grid.Columns["return_qty"].Width = 90;
                    grid.Columns["selling_price"].Width = 90;
                    grid.Columns["discount_percent"].Width = 80;
                    grid.Columns["gst_amount"].Width = 90;
                    grid.Columns["net_amount"].Width = 100;

                    // Add ReturnQty column
                    if (!grid.Columns.Contains("ReturnQty"))
                    {
                        DataGridViewTextBoxColumn col =
                            new DataGridViewTextBoxColumn();

                        col.Name = "ReturnQty";
                        col.HeaderText = "Return Qty";

                        grid.Columns.Add(col);
                    }

                    // Add Refund column
                    if (!grid.Columns.Contains("Refund"))
                    {
                        DataGridViewTextBoxColumn col =
                            new DataGridViewTextBoxColumn();

                        col.Name = "Refund";
                        col.HeaderText = "Refund";
                        col.ReadOnly = true;

                        grid.Columns.Add(col);
                    }

                    // Make all readonly
                    foreach (DataGridViewColumn column in grid.Columns)
                    {
                        column.ReadOnly = true;
                    }

                    // Only ReturnQty editable
                    grid.Columns["ReturnQty"].ReadOnly = false;

                    // ReturnQty styling
                    grid.Columns["ReturnQty"].HeaderCell.Style.BackColor =
                        Color.FromArgb(0, 120, 215);

                    grid.Columns["ReturnQty"].HeaderCell.Style.ForeColor =
                        Color.White;

                    grid.Columns["ReturnQty"].DefaultCellStyle.BackColor =
                        Color.FromArgb(230, 240, 255);

                    grid.Columns["ReturnQty"].DefaultCellStyle.SelectionBackColor =
                        Color.FromArgb(180, 210, 255);

                    grid.Columns["ReturnQty"].DefaultCellStyle.SelectionForeColor =
                        Color.Black;

                    grid.Enabled = true;
                    btnProcess.Enabled = grid.Rows.Count > 0;
                    btnReset.Enabled = true;
                    btnReset.BackColor = ResetEnabledColor;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].Name == "ReturnQty")
            {
                DataGridViewRow row = grid.Rows[e.RowIndex];

                int qty = Convert.ToInt32(row.Cells["qty"].Value);

                int returned = 0;
                int.TryParse(row.Cells["return_qty"].Value?.ToString(), out returned);

                int returnQty = 0;
                string rawReturnQty = row.Cells["ReturnQty"].Value?.ToString()?.Trim() ?? "";
                if (rawReturnQty == "")
                {
                    row.Cells["ReturnQty"].Value = 0;
                    row.Cells["Refund"].Value = 0;
                    CalculateTotalRefund();
                    return;
                }
                if (!int.TryParse(rawReturnQty, out returnQty) || returnQty < 0)
                {
                    MessageBox.Show("Return Qty must be a positive whole number.");
                    row.Cells["ReturnQty"].Value = 0;
                    row.Cells["Refund"].Value = 0;
                    CalculateTotalRefund();
                    return;
                }

                decimal total = Convert.ToDecimal(row.Cells["net_amount"].Value);

                int allowed = qty - returned;

                // validation
                if (returnQty > allowed)
                {
                    MessageBox.Show("Return qty exceeds allowed limit");
                    row.Cells["ReturnQty"].Value = 0;
                    row.Cells["Refund"].Value = 0;
                    return;
                }

                // ✅ FIX HERE
                int remaining = qty - returned;

                decimal perItem = remaining == 0 ? 0 : total / remaining;

                decimal refund = Round2(perItem * returnQty);

                row.Cells["Refund"].Value = refund.ToString("0.00");

                CalculateTotalRefund();
            }
        }

        private void CalculateTotalRefund()
        {
            decimal total = 0;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Cells["Refund"].Value != null)
                {
                    decimal val;
                    if (decimal.TryParse(row.Cells["Refund"].Value.ToString(), out val))
                        total += val;
                }
            }

            lblRefund.Text = "TOTAL REFUND : " + total.ToString("0.00");
        }

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            if (isProcessingReturn)
                return;

            if (!int.TryParse(txtOrderId.Text, out int parsedOrderId))
            {
                MessageBox.Show("Invalid order id.");
                return;
            }

            if (!IsReturnAllowedForOrder(parsedOrderId))
            {
                MessageBox.Show("Return allowed only within 7 days. 8th day se return allowed nahi hai.");
                btnProcess.Enabled = false;
                return;
            }

            bool hasReturn = false;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Cells["ReturnQty"].Value != null)
                {
                    int qty;
                    if (int.TryParse(row.Cells["ReturnQty"].Value.ToString(), out qty) && qty > 0)
                    {
                        hasReturn = true;
                        break;
                    }
                }
            }

            // ❌ STOP EVERYTHING
            if (!hasReturn)
            {
                MessageBox.Show("Please enter return quantity first ❌");
                return;
            }

            isProcessingReturn = true;
            btnProcess.Enabled = false;
            try
            {
                using (MySqlConnection con = DB.GetConnection())
                {
                    con.Open();
                    using MySqlTransaction transaction = con.BeginTransaction();

                    decimal totalRefund = 0;
                    int orderId = 0;

                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        if (row.IsNewRow) continue;

                        int qty = Convert.ToInt32(row.Cells["qty"].Value);

                        int returnedAlready = 0;
                        int.TryParse(row.Cells["return_qty"].Value?.ToString(), out returnedAlready);

                        int returnNow = 0;
                        int.TryParse(row.Cells["ReturnQty"].Value?.ToString(), out returnNow);

                        decimal gross = Convert.ToDecimal(row.Cells["gross_amount"].Value);

                        decimal discountAmount =
                            Convert.ToDecimal(row.Cells["discount_amount"].Value);

                        decimal total =
                            Convert.ToDecimal(row.Cells["net_amount"].Value);

                        decimal subtotalCurrent =
                            Convert.ToDecimal(row.Cells["taxable_amount"].Value);

                        decimal tax =
                            Convert.ToDecimal(row.Cells["gst_amount"].Value);

                        string itemCode = row.Cells["item_code"].Value?.ToString() ?? "";

                        int detailId = Convert.ToInt32(row.Cells["id"].Value);
                        orderId = parsedOrderId;

                    // ✅ VALIDATION
                        if (returnedAlready + returnNow > qty)
                        {
                            MessageBox.Show("Return exceeds quantity");
                            return;
                        }

                        if (returnNow > 0)
                        {
                            int newReturnQty = returnedAlready + returnNow;

                            int currentRemaining = qty - returnedAlready;   // IMPORTANT
                            int newRemaining = qty - newReturnQty;
                            if (currentRemaining <= 0)
                                continue;

                            // paid amount proportion after discount/GST
                            decimal perItem =
                                total / currentRemaining;

                            decimal newTotal =
                                Round2(perItem * newRemaining);

                            decimal taxPerItem =
                                tax / currentRemaining;

                            decimal newTax =
                                Round2(taxPerItem * newRemaining);

                            decimal subtotalPerItem =
                                subtotalCurrent / currentRemaining;

                            decimal newSubtotal =
                                Round2(subtotalPerItem * newRemaining);

                            decimal grossPerItem =
                                gross / currentRemaining;

                            decimal newGross =
                                Round2(grossPerItem * newRemaining);

                            decimal discountPerItem =
                                discountAmount / currentRemaining;

                            decimal newDiscount =
                                Round2(discountPerItem * newRemaining);

                            // refund
                            decimal refund = Round2(perItem * returnNow);
                            totalRefund += refund;

                        // ✅ UPDATE order_details (FULL CORRECT)
                            MySqlCommand cmd = new MySqlCommand(@"
                            UPDATE inv_order_details
                                SET
                                    return_qty = @rqty,
                                    gross_amount = @gross,
                                    discount_amount = @disc,
                                    taxable_amount = @sub,
                                    gst_amount = @tax,
                                    net_amount = @total
                                WHERE id = @id", con, transaction);

                            cmd.Parameters.AddWithValue("@rqty", newReturnQty);

                            cmd.Parameters.AddWithValue("@gross", newGross);

                            cmd.Parameters.AddWithValue("@disc", newDiscount);

                            cmd.Parameters.AddWithValue("@sub", newSubtotal);

                            cmd.Parameters.AddWithValue("@tax", newTax);

                            cmd.Parameters.AddWithValue("@total", newTotal);

                            cmd.Parameters.AddWithValue("@id", detailId);

                            cmd.ExecuteNonQuery();

                            MySqlCommand stockCmd = new MySqlCommand(@"
                    UPDATE inv_stock
                    SET quantity = quantity + @qty,
                        last_updated = NOW()
                    WHERE item_code = @code", con, transaction);

                            stockCmd.Parameters.AddWithValue("@qty", returnNow);
                            stockCmd.Parameters.AddWithValue("@code", itemCode);
                            int stockRows = stockCmd.ExecuteNonQuery();
                            if (stockRows == 0)
                                throw new Exception($"Stock update failed for returned item: {itemCode}");
                        }
                    }

                // ✅ UPDATE orders table (SUM based)
                    MySqlCommand cmd2 = new MySqlCommand(@"
                    SELECT
                    IFNULL(SUM(taxable_amount),0),
                    IFNULL(SUM(gst_amount),0),
                    IFNULL(SUM(net_amount),0),
                    IFNULL(SUM(discount_amount),0)
                    FROM inv_order_details 
                    WHERE order_id = @oid", con, transaction);

                    cmd2.Parameters.AddWithValue("@oid", orderId);

                    MySqlDataReader dr = cmd2.ExecuteReader();

                    decimal subtotal = 0, taxTotal = 0, grandTotal = 0, totalDiscount = 0;

                    if (dr.Read())
                    {
                        subtotal = dr.GetDecimal(0);
                        taxTotal = dr.GetDecimal(1);
                        grandTotal = dr.GetDecimal(2);
                        totalDiscount = dr.GetDecimal(3);
                    }
                    dr.Close();

                // ✅ UPDATE orders
                    MySqlCommand cmd3 = new MySqlCommand(@"
            UPDATE inv_orders 
            SET 
                subtotal = @sub,
                total_discount = @disc,
                total_tax = @tax,
                grand_total = @gt,
                date_updated = NOW()
            WHERE id = @id", con, transaction);

                    cmd3.Parameters.AddWithValue("@sub", Round2(subtotal));
                    cmd3.Parameters.AddWithValue("@disc", Round2(totalDiscount));
                    cmd3.Parameters.AddWithValue("@tax", Round2(taxTotal));
                    cmd3.Parameters.AddWithValue("@gt", Round2(grandTotal));
                    cmd3.Parameters.AddWithValue("@id", orderId);

                    cmd3.ExecuteNonQuery();
                    transaction.Commit();

                    MessageBox.Show("Return Completed Successfully\n\nTotal Refund Amount: ₹ " + totalRefund.ToString("0.00"));
                }
            }
            finally
            {
                isProcessingReturn = false;
                btnProcess.Enabled = grid != null && grid.Enabled && grid.Rows.Count > 0;
            }


            //GenerateReceipt();

            // Dynamic height based on actual printable return lines
            int dynamicHeight = CalculateReturnPrintHeight();

            PaperSize customSize = new PaperSize("Custom", 300, dynamicHeight);
            printDoc.DefaultPageSettings.PaperSize = customSize;
            PrinterRouting.ApplyReceiptReturnPrinter(printDoc);
            if (!printDoc.PrinterSettings.IsValid)
            {
                MessageBox.Show("No valid receipt/return printer found. Please install/select printer.");
                return;
            }

            // 🔥 DIRECT PRINT (no preview)
            printDoc.Print();
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

            Font font = new Font("Segoe UI", 9);
            Font bold = new Font("Segoe UI", 9, FontStyle.Bold);
            Font header = new Font("Segoe UI", 10, FontStyle.Bold);

            float y = 5;
            string returnId = "RET-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            int pageWidth = e.PageSettings.PaperSize.Width;

            // 1) Store details (centered)
            SizeF storeSize = g.MeasureString(StoreName, header);
            g.DrawString(StoreName, header, Brushes.Black, (pageWidth - storeSize.Width) / 2, y);
            y += 15;
            SizeF add1 = g.MeasureString(StoreAddressLine1, font);
            g.DrawString(StoreAddressLine1, font, Brushes.Black, (pageWidth - add1.Width) / 2, y);
            y += 13;
            SizeF add2 = g.MeasureString(StoreAddressLine2, font);
            g.DrawString(StoreAddressLine2, font, Brushes.Black, (pageWidth - add2.Width) / 2, y);
            y += 13;
            string phoneLine = "Phone: " + StorePhone;
            SizeF phoneSize = g.MeasureString(phoneLine, font);
            g.DrawString(phoneLine, font, Brushes.Black, (pageWidth - phoneSize.Width) / 2, y);
            y += 15;

            // 2) Heading (centered)
            string heading = "RETURN RECEIPT";
            SizeF headingSize = g.MeasureString(heading, bold);
            g.DrawString(heading, bold, Brushes.Black, (pageWidth - headingSize.Width) / 2, y);
            y += 15;

            // 3) Return id
            g.DrawString("Return ID: " + returnId, font, Brushes.Black, 5, y);
            y += 13;

            // 4) Original invoice id
            g.DrawString("Original Invoice ID: " + txtOrderId.Text, font, Brushes.Black, 5, y);
            y += 13;

            // 5) Date-time
            g.DrawString("Date: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm"), font, Brushes.Black, 5, y);
            y += 13;

            // 6) Customer details
            string customer = (lblCustomer.Text ?? "").Trim();
            customer = customer.Replace("Customer Name:", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("Customer:", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("Customer", "", StringComparison.OrdinalIgnoreCase)
                               .Replace(":", "")
                               .Trim();
            if (string.IsNullOrWhiteSpace(customer))
                customer = "Walk-in Customer";
            string phone = (lblPhone.Text ?? "").Replace("Mobile:", "").Trim();
            g.DrawString("Customer: " + customer, font, Brushes.Black, 5, y);
            y += 13;
            g.DrawString(phone, font, Brushes.Black, 5, y);
            y += 12;

            g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
            y += 12;

            // Column headers
            g.DrawString("Item", bold, Brushes.Black, 5, y);
            g.DrawString("Qty", bold, Brushes.Black, 160, y);
            g.DrawString("Amt", bold, Brushes.Black, 220, y);
            y += 15;

            g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
            y += 10;
            decimal totalRefundAmount = 0m;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Cells["ReturnQty"].Value != null)
                {
                    int qty;
                    if (int.TryParse(row.Cells["ReturnQty"].Value.ToString(), out qty) && qty > 0)
                    {
                        string name = row.Cells["item_name"].Value.ToString();
                        decimal refund = Convert.ToDecimal(row.Cells["Refund"].Value);
                        totalRefundAmount += refund;

                        // 🔥 Handle long name (wrap)
                        if (name.Length > 18)
                        {
                            string line1 = name.Substring(0, 18);
                            string line2 = name.Substring(18);

                            g.DrawString(line1, font, Brushes.Black, 5, y);
                            y += 12;

                            g.DrawString(line2, font, Brushes.Black, 5, y);
                        }
                        else
                        {
                            g.DrawString(name, font, Brushes.Black, 5, y);
                        }

                        // 👉 FIXED POSITION (no overlap)
                        g.DrawString(qty.ToString(), font, Brushes.Black, 160, y);
                        g.DrawString(refund.ToString("0.00"), font, Brushes.Black, 220, y);

                        y += 18;
                    }
                }
            }

            y += 10;
            g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
            y += 15;

            g.DrawString("TOTAL REFUND : ₹ " + totalRefundAmount.ToString("0.00"), bold, Brushes.Black, 5, y);
            y += 20;

            g.DrawString("Thank You!", font, Brushes.Black, 90, y);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            txtOrderId.Text = "";
            ResetReturnForm();
            txtOrderId.Focus();
        }

        private void TxtOrderId_TextChanged(object sender, EventArgs e)
        {
            ResetReturnForm();
        }

        private void ResetReturnForm()
        {
            lblCustomer.Text = "";
            lblPhone.Text = "";
            lblDate.Text = "";
            lblSubtotal.Text = "";
            lblTax.Text = "";
            lblTotal.Text = "";
            lblRefund.Text = "TOTAL REFUND : 0";

            grid.DataSource = null;
            grid.Rows.Clear();
            grid.Columns.Clear();
            grid.Enabled = true;
            btnProcess.Enabled = false;
            btnReset.Enabled = false;
            btnReset.BackColor = DisabledButtonColor;
        }

        private int CalculateReturnPrintHeight()
        {
            // Header + table labels + footer space
            int baseHeight = 290;
            int perLineHeight = 18;
            int printableLines = 0;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (row.Cells["ReturnQty"].Value == null)
                    continue;

                if (!int.TryParse(row.Cells["ReturnQty"].Value.ToString(), out int qty) || qty <= 0)
                    continue;

                string name = row.Cells["item_name"].Value?.ToString() ?? "";
                // If long name, print in two lines
                printableLines += name.Length > 18 ? 2 : 1;
            }

            if (printableLines <= 0)
                printableLines = 1;

            return baseHeight + (printableLines * perLineHeight);
        }

        private bool IsReturnAllowedWithin7Days(DateTime orderDate)
        {
            double elapsedDays = (DateTime.Now.Date - orderDate.Date).TotalDays;
            return elapsedDays < 8;
        }

        private bool IsReturnAllowedForOrder(int orderId)
        {
            using (MySqlConnection con = DB.GetConnection())
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT date_added FROM inv_orders WHERE id=@id LIMIT 1", con);
                cmd.Parameters.AddWithValue("@id", orderId);
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return false;

                if (!DateTime.TryParse(result.ToString(), out DateTime orderDate))
                    return false;

                return IsReturnAllowedWithin7Days(orderDate);
            }
        }
    }
}
