using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using MySqlX.XDevAPI;
using System.Collections.Generic;
using Mysqlx.Crud;
using System.Transactions;

public class HoldBill
{
    public string Token { get; set; }
    public DateTime CreatedOn { get; set; }
    public List<HoldItem> Items { get; set; } = new();

    public bool RewardApplied { get; set; }

    public decimal RewardDiscountPercent { get; set; }

    public string Mobile { get; set; }

    public string FirstName { get; set; }

    public string Surname { get; set; }
    public string MembershipName { get; set; }
}

public class HoldItem
{
    public string ItemCode { get; set; }

    public string ItemName { get; set; }

    public string Size { get; set; }

    public decimal Price { get; set; }

    public int Qty { get; set; }

    public decimal Discount { get; set; }

    public decimal AutoDiscount { get; set; }

    public decimal ManualDiscount { get; set; }

    public decimal RewardDiscount { get; set; }

    public decimal Gross { get; set; }

    public decimal Taxable { get; set; }

    public decimal GST { get; set; }

    public decimal Net { get; set; }

    public bool DiscountManual { get; set; }

    public decimal GSTPercent { get; set; }

    public int ItemId { get; set; }

    public string Color { get; set; }
}

namespace BubbyPlanetShowroom
{
    public class Receipt : UserControl
    {
        private PrintDocument printDocument = new PrintDocument();
        private const string StoreName = "Bubbyplanet";
        private const string StoreAddressLine1 = "Daudnagar Branch, Aurangabad";
        private const string StoreAddressLine2 = "Bihar - 824143";
        private const string StorePhone = "7870828400";
        private const string StoreEmail = "bubbyplanet@gmail.com";
        private const string StoreWebsite = "bubbyplanet.com";
        private static readonly Color ResetEnabledColor = Color.FromArgb(185, 28, 28);
        private static readonly Color PrintEnabledColor = Color.FromArgb(22, 163, 74);
        private static readonly Color DisabledButtonColor = Color.FromArgb(156, 163, 175);
        TextBox txtBarcode;
        //DataGridView dgvLeft;
        DataGridView dgvRight;
        DataGridView dgvDraft;
        Panel draftPanel;
        Label lblGrandTotal;
        Button btnPrint;
        Button btnReset;
        Button btnRetryDraft;
        Button btnClearDraft;
        ListBox lstHoldBills;
        private List<HoldBill> holdBills = new List<HoldBill>();

        private decimal rewardDiscountPercent = 0;
        private bool rewardApplied = false;
        private decimal rewardPurchaseAmount = 0;
        private string lastRewardMobile = "";
        private string lastRewardCheckMobile = "";
        private decimal lastRewardCheckGrandTotal = -1;

        private bool isLoadingHoldBill = false;
        private string currentMembership = "";
        private TextBox txtPaidAmount;
        private TextBox txtBillAmount;
        private Label lblReturnAmount;
        private sealed class DraftScan
        {
            public DateTime Time { get; set; }
            public string Barcode { get; set; } = "";
            public string Reason { get; set; } = "";
        }

        private readonly BindingList<DraftScan> draftScans = new BindingList<DraftScan>();

        // Customer Controls
        TextBox txtName;
        TextBox txtMobile;
        Button btnCheckMobile;
        //Label lblMembership;
        TextBox txtSurname;
        Button btnUpdateCustomer;
        ComboBox cmbPaymentMethod;
        int currentCustomerId = 0;
        bool currentCustomerIsStaff = false;
        int lastOrderId = 0;
        Label lblCustomerStatus;

        decimal grandTotal = 0;
        decimal totaltaxableamount = 0;
        decimal totalgst = 0;
        bool hasShownRecalcDataError = false;
        private static bool DiscountDebugEnabled = false;

        private decimal Round2(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private decimal ClampDiscount(decimal value)
        {
            if (value < 0)
                return 0;
            if (value > 100)
                return 100;
            return value;
        }

        private decimal GetCellDecimal(DataGridViewRow row, string columnName)
        {
            if (row?.Cells[columnName]?.Value == null)
                return 0;

            return decimal.TryParse(row.Cells[columnName].Value.ToString(), out decimal value)
                ? value
                : 0;
        }

        private decimal GetCellDecimal(DataGridViewRow row, int columnIndex)
        {
            if (row?.Cells[columnIndex]?.Value == null)
                return 0;

            return decimal.TryParse(row.Cells[columnIndex].Value.ToString(), out decimal value)
                ? value
                : 0;
        }

        private void SetDiscountBreakdown(
            DataGridViewRow row,
            decimal autoDiscount,
            decimal manualDiscount,
            decimal rewardDiscount)
        {
            autoDiscount = ClampDiscount(autoDiscount);
            manualDiscount = Math.Max(0, manualDiscount);
            rewardDiscount = Math.Max(0, rewardDiscount);

            row.Cells["Auto_Discount"].Value = autoDiscount;
            row.Cells["Manual_Discount"].Value = manualDiscount;
            row.Cells["Reward_Discount"].Value = rewardDiscount;
            row.Cells[1].Value = ClampDiscount(autoDiscount + manualDiscount + rewardDiscount);
        }

        private bool IsManualDiscountRow(DataGridViewRow row)
        {
            object manualVal = row?.Cells["Discount_Manual"]?.Value ?? 0;
            if (manualVal is bool b)
                return b;

            return manualVal.ToString() == "1";
        }

        private void LstHoldBills_DoubleClick(object sender, EventArgs e)
        {
            int index = lstHoldBills.SelectedIndex;

            if (index < 0)
                return;

            HoldBill selectedBill = holdBills[index];

            LoadHoldBill(selectedBill);

            holdBills.RemoveAt(index);

            lstHoldBills.Items.RemoveAt(index);
        }

        private void BtnHoldBill_Click(object sender, EventArgs e)
        {
            if (dgvRight.Rows.Count == 0)
                return;

            HoldBill bill = new HoldBill();

            bill.Token = "H" + DateTime.Now.ToString("HHmmss");
            bill.CreatedOn = DateTime.Now;

            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (row.IsNewRow)
                    continue;

                bill.Items.Add(new HoldItem
                {
                    ItemCode = row.Cells["Item_Code"].Value?.ToString(),

                    ItemName = row.Cells[0].Value?.ToString(),

                    Discount = GetCellDecimal(row, 1),

                    AutoDiscount = GetCellDecimal(row, "Auto_Discount"),

                    ManualDiscount = GetCellDecimal(row, "Manual_Discount"),

                    RewardDiscount = GetCellDecimal(row, "Reward_Discount"),

                    Size = row.Cells[2].Value?.ToString(),

                    Price = Convert.ToDecimal(row.Cells[3].Value),

                    Qty = Convert.ToInt32(row.Cells[4].Value),

                    Gross = Convert.ToDecimal(row.Cells[5].Value),

                    Taxable = Convert.ToDecimal(row.Cells[6].Value),

                    GST = Convert.ToDecimal(row.Cells[7].Value),

                    Net = Convert.ToDecimal(row.Cells[8].Value),

                    GSTPercent = Convert.ToDecimal(row.Cells["GSTPercent"].Value ?? 0),

                    ItemId = Convert.ToInt32(row.Cells["Item_Id"].Value ?? 0),

                    Color = row.Cells["Color"].Value?.ToString() ?? "",

                    DiscountManual = Convert.ToBoolean(row.Cells["Discount_Manual"].Value ?? false)
                });
            }

            bill.Mobile = txtMobile.Text.Trim();
            bill.FirstName = txtName.Text.Trim();
            bill.Surname = txtSurname.Text.Trim();

            bill.RewardApplied = rewardApplied;
            bill.RewardDiscountPercent = rewardDiscountPercent;
            bill.MembershipName = currentMembership;

            holdBills.Add(bill);

            lstHoldBills.Items.Add(
                $"{bill.Token} ({bill.Items.Count} Items)"
            );

            ResetBill();
        }

        private void BtnResumeBill_Click(object sender, EventArgs e)
        {
            if (holdBills.Count == 0)
            {
                MessageBox.Show("No Hold Bills Found");
                return;
            }

            string msg = "";

            for (int i = 0; i < holdBills.Count; i++)
            {
                msg += $"{i + 1}. {holdBills[i].Token}\n";
            }

            string input =
                Microsoft.VisualBasic.Interaction.InputBox(
                    msg + "\nEnter Number");

            if (!int.TryParse(input, out int index))
                return;

            index--;

            if (index < 0 || index >= holdBills.Count)
                return;

            LoadHoldBill(holdBills[index]);

            holdBills.RemoveAt(index);

            if (index >= 0 && index < lstHoldBills.Items.Count)
                lstHoldBills.Items.RemoveAt(index);
        }

        private void LoadHoldBill(HoldBill bill)
        {
            try
            {
                isLoadingHoldBill = true;

                ResetBill();

                txtMobile.Text = bill.Mobile;
                txtName.Text = bill.FirstName;
                txtSurname.Text = bill.Surname;

                rewardApplied = bill.RewardApplied;
                rewardDiscountPercent = bill.RewardDiscountPercent;

                lastRewardMobile = bill.Mobile;
                currentMembership = bill.MembershipName;

                foreach (var item in bill.Items)
                {
                    dgvRight.Rows.Add(
                        item.ItemName,
                        item.Discount,
                        item.Size,
                        item.Price,
                        item.Qty,
                        item.Gross,
                        item.Taxable,
                        item.GST,
                        item.Net,
                        item.GSTPercent,
                        item.ItemCode,
                        item.ItemId,
                        item.Color,
                        item.DiscountManual,
                        item.AutoDiscount,
                        item.ManualDiscount,
                        item.RewardDiscount
                    );
                }

                RecalculateTotals();
            }
            finally
            {
                isLoadingHoldBill = false;
            }
        }

        private void CalculateLineAmounts(
        decimal sellingPrice,
        decimal gstPercent,
        decimal discountPercent,
        int qty,
        out decimal subtotal,
        out decimal gstAmount,
        out decimal total)
        {
            if (discountPercent < 0)
                discountPercent = 0;
            if (discountPercent > 100)
                discountPercent = 100;

            // Discount on GST-inclusive selling price.
            decimal discountAmountPerUnit = (sellingPrice * discountPercent) / 100m;

            // Net amount remains GST inclusive, matching retail invoice practice.
            decimal netAmountPerUnit = sellingPrice - discountAmountPerUnit;
            total = Round2(netAmountPerUnit * qty);

            // Reverse-calculate taxable value from GST-inclusive net amount.
            subtotal = Round2((total * 100m) / (100m + gstPercent));

            // GST amount = net amount - taxable value.
            gstAmount = Round2(total - subtotal);
        }

        public Receipt()
        {
            InitializeUI();
            printDocument.PrintPage += PrintDocument_PrintPage;
            btnPrint.Click += BtnPrint_Click;

            this.Load += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    txtBarcode.Focus();
                    txtBarcode.SelectAll();
                }));
            };

            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible)
                {
                    BeginInvoke(new Action(() =>
                    {
                        txtBarcode.Focus();
                        txtBarcode.SelectAll();
                    }));
                }
            };
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(241, 245, 249);

            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.ColumnCount = 2;
            mainLayout.RowCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.Controls.Add(mainLayout);

            Panel centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), BackColor = Color.FromArgb(241, 245, 249) };
            Panel rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), BackColor = Color.FromArgb(241, 245, 249) };

            mainLayout.Controls.Add(centerPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);

            TableLayoutPanel barcodeActionPanel = new TableLayoutPanel();
            barcodeActionPanel.Dock = DockStyle.Top;
            barcodeActionPanel.Height = 75;
            barcodeActionPanel.Padding = new Padding(0, 0, 0, 6);
            barcodeActionPanel.BackColor = Color.Transparent;

            barcodeActionPanel.ColumnCount = 7;

            barcodeActionPanel.ColumnStyles.Clear();

            barcodeActionPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 280F));

            barcodeActionPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 120F));

            barcodeActionPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 130F));

            barcodeActionPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 20F));

            barcodeActionPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 160F));

            barcodeActionPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 160F));

            barcodeActionPanel.ColumnStyles.Add(
    new ColumnStyle(SizeType.Absolute, 180F));

            txtBarcode = new TextBox();
            txtBarcode.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            txtBarcode.BorderStyle = BorderStyle.FixedSingle;
            txtBarcode.BackColor = Color.White;
            txtBarcode.PlaceholderText = "Scan or enter barcode";
            txtBarcode.Dock = DockStyle.Fill;
            txtBarcode.Margin = new Padding(0, 0, 8, 0);
            txtBarcode.KeyDown += TxtBarcode_KeyDown;
            txtBarcode.Dock = DockStyle.Fill;

            txtBarcode.Margin =
    new Padding(0, 8, 8, 8);

            dgvRight = new DataGridView();
            dgvRight.Dock = DockStyle.Fill;
            dgvRight.AllowUserToAddRows = false;
            dgvRight.AllowUserToDeleteRows = true;
            dgvRight.AllowUserToResizeRows = false;
            dgvRight.RowHeadersVisible = false;
            dgvRight.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRight.MultiSelect = false;
            dgvRight.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRight.ScrollBars = ScrollBars.Vertical;
            dgvRight.BackgroundColor = Color.White;
            dgvRight.BorderStyle = BorderStyle.FixedSingle;
            dgvRight.GridColor = Color.Gainsboro;
            dgvRight.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvRight.RowTemplate.Height = 32;
            dgvRight.ColumnHeadersHeight = 36;
            dgvRight.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvRight.ColumnCount = 9;
            dgvRight.Columns[0].Name = "Item Name";

            dgvRight.Columns[1].Name = "Discount %";
            dgvRight.Columns[1].ReadOnly = false;

            dgvRight.Columns[2].Name = "Size";

            dgvRight.Columns[3].Name = "Price";

            dgvRight.Columns[4].Name = "Qty";
            dgvRight.Columns[4].ReadOnly = false;

            dgvRight.Columns[5].Name = "Gross";
            dgvRight.Columns[5].ReadOnly = true;

            dgvRight.Columns[6].Name = "Taxable";
            dgvRight.Columns[6].ReadOnly = true;

            dgvRight.Columns[7].Name = "GST";
            dgvRight.Columns[7].ReadOnly = true;

            dgvRight.Columns[8].Name = "Net";
            dgvRight.Columns[8].ReadOnly = true;

            dgvRight.Columns[0].FillWeight = 50;
            dgvRight.Columns[1].FillWeight = 6;
            dgvRight.Columns[2].FillWeight = 6;
            dgvRight.Columns[3].FillWeight = 6;
            dgvRight.Columns[4].FillWeight = 6;
            dgvRight.Columns[5].FillWeight = 6; // Gross
            dgvRight.Columns[6].FillWeight = 6; // Taxable
            dgvRight.Columns[7].FillWeight = 6;  // GST
            dgvRight.Columns[8].FillWeight = 10; // Net

            dgvRight.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvRight.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvRight.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvRight.EnableHeadersVisualStyles = false;
            dgvRight.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgvRight.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgvRight.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvRight.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgvRight.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            // Hidden column for GST percentage
            dgvRight.Columns.Add("GSTPercent", "GSTPercent");
            dgvRight.Columns["GSTPercent"].Visible = false;
            // hidden column for Item Code (for stock update) 
            dgvRight.Columns.Add("Item_Code", "Item_Code");
            dgvRight.Columns["Item_Code"].Visible = false;
            // hidden column for item id (for exact order detail save)
            dgvRight.Columns.Add("Item_Id", "Item_Id");
            dgvRight.Columns["Item_Id"].Visible = false;
            // hidden column for color (for variant-level print clarity)
            dgvRight.Columns.Add("Color", "Color");
            dgvRight.Columns["Color"].Visible = false;

            // hidden column to track if discount was manually edited by user (1=true,0=false)
            dgvRight.Columns.Add("Discount_Manual", "Discount_Manual");
            dgvRight.Columns["Discount_Manual"].Visible = false;
            dgvRight.Columns.Add("Auto_Discount", "Auto_Discount");
            dgvRight.Columns["Auto_Discount"].Visible = false;
            dgvRight.Columns.Add("Manual_Discount", "Manual_Discount");
            dgvRight.Columns["Manual_Discount"].Visible = false;
            dgvRight.Columns.Add("Reward_Discount", "Reward_Discount");
            dgvRight.Columns["Reward_Discount"].Visible = false;

            btnReset = new Button();
            btnReset.Text = "Reset Bill";
            btnReset.Dock = DockStyle.None;
            btnReset.Height = 35;
            btnReset.Width = 100;
            btnReset.Text = "Reset Bill";
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.BackColor = ResetEnabledColor;
            btnReset.ForeColor = Color.White;
            btnReset.Enabled = false;
            btnReset.Click += BtnReset_Click;

            btnReset.Margin = new Padding(0, 8, 8, 8);

            lblGrandTotal = new Label();
            lblGrandTotal.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblGrandTotal.Text = "Grand Total: 0.00";
            lblGrandTotal.Dock = DockStyle.Fill;
            lblGrandTotal.BackColor = Color.FromArgb(15, 23, 42);
            lblGrandTotal.ForeColor = Color.White;
            lblGrandTotal.TextAlign = ContentAlignment.MiddleLeft;

            btnPrint = new Button();
            btnPrint.Text = "Print Bill";
            btnPrint.Width = 100;
            btnPrint.Height = 35;
            btnPrint.FlatStyle = FlatStyle.Flat;
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.BackColor = PrintEnabledColor;
            btnPrint.ForeColor = Color.White;
            btnPrint.Enabled = false;

            btnPrint.Margin = new Padding(0, 8, 8, 8);


            // ===== Paid Panel =====
            TableLayoutPanel paidPanel = new TableLayoutPanel();
            paidPanel.Dock = DockStyle.Fill;
            paidPanel.RowCount = 2;
            paidPanel.ColumnCount = 1;

            paidPanel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 20));

            paidPanel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 40));

            Label lblPaid = new Label();
            lblPaid.Text = "Paid Amount";
            lblPaid.Dock = DockStyle.Fill;
            lblPaid.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            txtPaidAmount = new TextBox();
            txtPaidAmount.Dock = DockStyle.Fill;
            txtPaidAmount.Font = new Font("Segoe UI", 11);
            txtPaidAmount.TextAlign = HorizontalAlignment.Right;

            paidPanel.Controls.Add(lblPaid, 0, 0);
            paidPanel.Controls.Add(txtPaidAmount, 0, 1);


            // ===== Bill Panel =====
            TableLayoutPanel billPanel = new TableLayoutPanel();
            billPanel.Dock = DockStyle.Fill;
            billPanel.RowCount = 2;
            billPanel.ColumnCount = 1;

            billPanel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 20));

            billPanel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 40));

            Label lblBill = new Label();
            lblBill.Text = "Bill Amount";
            lblBill.Dock = DockStyle.Fill;
            lblBill.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            txtBillAmount = new TextBox();
            txtBillAmount.Dock = DockStyle.Fill;
            txtBillAmount.Font = new Font("Segoe UI", 11);
            txtBillAmount.ReadOnly = true;
            txtBillAmount.BackColor = Color.WhiteSmoke;
            txtBillAmount.TextAlign = HorizontalAlignment.Right;
            txtBillAmount.ReadOnly = true;

            billPanel.Controls.Add(lblBill, 0, 0);
            billPanel.Controls.Add(txtBillAmount, 0, 1);


            // ===== Return Panel =====
            // ===== Return Panel =====
            Panel pnlReturn = new Panel();
            pnlReturn.Dock = DockStyle.Fill;
            pnlReturn.BackColor = Color.White;
            pnlReturn.BorderStyle = BorderStyle.FixedSingle;
            pnlReturn.Padding = new Padding(3);

            Label lblReturnTitle = new Label();
            lblReturnTitle.Text = "Return";
            lblReturnTitle.Dock = DockStyle.Top;
            lblReturnTitle.Height = 16;
            lblReturnTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblReturnTitle.Font = new Font("Segoe UI", 8, FontStyle.Bold);

            lblReturnAmount = new Label();
            lblReturnAmount.Text = "₹0.00";
            lblReturnAmount.Dock = DockStyle.Fill;
            lblReturnAmount.TextAlign = ContentAlignment.MiddleCenter;
            lblReturnAmount.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblReturnAmount.ForeColor = Color.FromArgb(22, 163, 74);
            lblReturnAmount.Height = 10;

            pnlReturn.Controls.Add(lblReturnAmount);
            pnlReturn.Controls.Add(lblReturnTitle);


            // ===== Button Styling =====

            btnReset.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            btnPrint.Font = new Font("Segoe UI", 10, FontStyle.Regular);


            // ===== Add Controls =====
            barcodeActionPanel.Controls.Add(txtBarcode, 0, 0);
            barcodeActionPanel.Controls.Add(btnReset, 1, 0);
            barcodeActionPanel.Controls.Add(btnPrint, 2, 0);

            barcodeActionPanel.Controls.Add(new Panel(), 3, 0);

            barcodeActionPanel.Controls.Add(paidPanel, 4, 0);
            barcodeActionPanel.Controls.Add(billPanel, 5, 0);
            barcodeActionPanel.Controls.Add(pnlReturn, 6, 0);


            Panel draftPanel = new Panel();
            this.draftPanel = draftPanel;
            draftPanel.Dock = DockStyle.Top;
            draftPanel.Height = 130;
            draftPanel.Padding = new Padding(0, 0, 0, 6);
            draftPanel.BackColor = Color.Transparent;

            TableLayoutPanel draftLayout = new TableLayoutPanel();
            draftLayout.Dock = DockStyle.Fill;
            draftLayout.ColumnCount = 1;
            draftLayout.RowCount = 2;
            draftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            draftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel draftHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                Height = 36
            };
            Label lblDraft = new Label
            {
                Text = "Draft (Not Added Items)",
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Width = 220,
            };

            btnRetryDraft = new Button
            {
                Text = "Retry Selected",
                Dock = DockStyle.Right,
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 64, 175),
                ForeColor = Color.White
            };
            btnRetryDraft.Height = 30;
            btnRetryDraft.FlatAppearance.BorderSize = 0;
            btnRetryDraft.Click += (s, e) => RetrySelectedDraft();

            btnClearDraft = new Button
            {
                Text = "Clear Draft",
                Dock = DockStyle.Right,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White
            };
            btnClearDraft.Height = 30;
            btnClearDraft.FlatAppearance.BorderSize = 0;
            btnClearDraft.Click += (s, e) =>
            {
                draftScans.Clear();
                UpdateDraftVisibility();
            };

            draftHeader.Controls.Add(btnRetryDraft);
            draftHeader.Controls.Add(btnClearDraft);
            draftHeader.Controls.Add(lblDraft);

            dgvDraft = new DataGridView();
            dgvDraft.Dock = DockStyle.Fill;
            dgvDraft.AllowUserToAddRows = false;
            dgvDraft.AllowUserToDeleteRows = false;
            dgvDraft.AllowUserToResizeRows = false;
            dgvDraft.ReadOnly = true;
            dgvDraft.RowHeadersVisible = false;
            dgvDraft.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDraft.MultiSelect = false;
            dgvDraft.AutoGenerateColumns = false;
            dgvDraft.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDraft.BackgroundColor = Color.White;
            dgvDraft.BorderStyle = BorderStyle.FixedSingle;
            dgvDraft.ColumnHeadersHeight = 28;
            dgvDraft.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvDraft.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Time", HeaderText = "Time", FillWeight = 20 });
            dgvDraft.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Barcode", HeaderText = "Barcode", FillWeight = 35 });
            dgvDraft.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Reason", HeaderText = "Reason", FillWeight = 45 });
            dgvDraft.DataSource = draftScans;
            dgvDraft.CellDoubleClick += (s, e) => RetrySelectedDraft();

            draftLayout.Controls.Add(draftHeader, 0, 0);
            draftLayout.Controls.Add(dgvDraft, 0, 1);
            draftPanel.Controls.Add(draftLayout);

            draftScans.ListChanged += (s, e) => UpdateDraftVisibility();
            UpdateDraftVisibility();

            Panel totalsPanel = new Panel();
            totalsPanel.Dock = DockStyle.Bottom;
            totalsPanel.Height = 42;
            totalsPanel.Padding = new Padding(0, 0, 0, 6);
            totalsPanel.BackColor = Color.Transparent;

            totalsPanel.Controls.Add(lblGrandTotal);

            centerPanel.Controls.Add(dgvRight);
            centerPanel.Controls.Add(draftPanel);
            centerPanel.Controls.Add(barcodeActionPanel);
            centerPanel.Controls.Add(totalsPanel);

            TableLayoutPanel customerLayout = new TableLayoutPanel();
            customerLayout.Dock = DockStyle.Top;
            customerLayout.AutoSize = true;
            customerLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            customerLayout.ColumnCount = 1;
            customerLayout.RowCount = 14;
            customerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            customerLayout.Padding = new Padding(14);
            customerLayout.BackColor = Color.White;

            Label lblMobile = new Label();
            lblMobile.Text = "Mobile Number";
            lblMobile.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblMobile.Dock = DockStyle.Fill;
            lblMobile.Margin = new Padding(0, 2, 0, 6);

            TableLayoutPanel mobileLayout = new TableLayoutPanel();
            mobileLayout.Dock = DockStyle.Top;
            mobileLayout.AutoSize = true;
            mobileLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            mobileLayout.ColumnCount = 2;
            mobileLayout.RowCount = 1;
            mobileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mobileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            mobileLayout.Padding = new Padding(0);
            mobileLayout.Margin = new Padding(0, 2, 0, 0);

            txtMobile = new TextBox();
            txtMobile.Dock = DockStyle.Fill;
            txtMobile.Width = 200;
            txtMobile.Height = 34;
            txtMobile.Margin = new Padding(0);
            txtMobile.MaxLength = 10;
            txtMobile.KeyDown += TxtMobileSearch_KeyDown;
            txtMobile.KeyPress += TxtMobile_KeyPress;
            txtMobile.TextChanged += TxtMobile_TextChanged;
            txtMobile.Validating += TxtMobile_Validating;

            btnCheckMobile = new Button();
            btnCheckMobile.Text = "Check";
            btnCheckMobile.Dock = DockStyle.Top;
            btnCheckMobile.Height = 34;
            btnCheckMobile.Margin = new Padding(8, 0, 0, 0);
            btnCheckMobile.FlatStyle = FlatStyle.Flat;
            btnCheckMobile.FlatAppearance.BorderSize = 0;
            btnCheckMobile.BackColor = Color.FromArgb(30, 64, 175);
            btnCheckMobile.ForeColor = Color.White;
            btnCheckMobile.Cursor = Cursors.Hand;
            btnCheckMobile.Click += BtnCheckMobile_Click;

            mobileLayout.Controls.Add(txtMobile, 0, 0);
            mobileLayout.Controls.Add(btnCheckMobile, 1, 0);

            Label lblFirst = new Label();
            lblFirst.Text = "First Name";
            lblFirst.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblFirst.Dock = DockStyle.Fill;
            lblFirst.Margin = new Padding(0, 12, 0, 6);

            txtName = new TextBox();
            txtName.Dock = DockStyle.Top;
            txtName.Height = 34;
            txtName.Margin = new Padding(0);

            Label lblSurname = new Label();
            lblSurname.Text = "Surname";
            lblSurname.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblSurname.Dock = DockStyle.Fill;
            lblSurname.Margin = new Padding(0, 12, 0, 6);

            txtSurname = new TextBox();
            txtSurname.Dock = DockStyle.Top;
            txtSurname.Height = 34;
            txtSurname.Margin = new Padding(0);

            btnUpdateCustomer = new Button();
            btnUpdateCustomer.Text = "Update Customer";
            btnUpdateCustomer.Dock = DockStyle.Top;
            btnUpdateCustomer.Height = 38;
            btnUpdateCustomer.BackColor = Color.FromArgb(30, 64, 175);
            btnUpdateCustomer.ForeColor = Color.White;
            btnUpdateCustomer.FlatStyle = FlatStyle.Flat;
            btnUpdateCustomer.FlatAppearance.BorderSize = 0;
            btnUpdateCustomer.Margin = new Padding(0, 14, 0, 0);
            btnUpdateCustomer.Click += BtnUpdateCustomer_Click;

            Label lblPaymentMethod = new Label();
            lblPaymentMethod.Text = "Payment Method";
            lblPaymentMethod.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblPaymentMethod.Dock = DockStyle.Fill;
            lblPaymentMethod.Margin = new Padding(0, 12, 0, 6);

            cmbPaymentMethod = new ComboBox();
            cmbPaymentMethod.Dock = DockStyle.Top;
            cmbPaymentMethod.Height = 34;
            cmbPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaymentMethod.Items.AddRange(new object[]
            {
                "",
                "Cash",
                "Online"
            });
            cmbPaymentMethod.SelectedIndex = 0;

            lblCustomerStatus = new Label();
            lblCustomerStatus.Text = "";
            lblCustomerStatus.AutoSize = true;
            lblCustomerStatus.ForeColor = Color.Green;
            lblCustomerStatus.Margin = new Padding(0, 4, 0, 4);

            customerLayout.Controls.Add(lblMobile, 0, 0);
            customerLayout.Controls.Add(mobileLayout, 0, 1);
            customerLayout.Controls.Add(lblCustomerStatus, 0, 2);
            customerLayout.Controls.Add(lblFirst, 0, 3);
            customerLayout.Controls.Add(txtName, 0, 4);
            customerLayout.Controls.Add(lblSurname, 0, 5);
            customerLayout.Controls.Add(txtSurname, 0, 6);

            customerLayout.Controls.Add(new Panel { Height = 10, Dock = DockStyle.Top, BackColor = Color.Transparent }, 0, 6);
            customerLayout.Controls.Add(btnUpdateCustomer, 0, 7);
            customerLayout.Controls.Add(lblPaymentMethod, 0, 8);
            customerLayout.Controls.Add(cmbPaymentMethod, 0, 9);
            

            Label lblHoldSection = new Label();
            lblHoldSection.Text = "Hold Bills";
            lblHoldSection.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblHoldSection.Dock = DockStyle.Fill;
            lblHoldSection.Margin = new Padding(0, 12, 0, 6);

            Button btnHoldBill = new Button();
            btnHoldBill.Text = "Hold Current Bill";
            btnHoldBill.Height = 38;
            btnHoldBill.Dock = DockStyle.Top;
            btnHoldBill.BackColor = Color.DarkOrange;
            btnHoldBill.ForeColor = Color.White;
            btnHoldBill.FlatStyle = FlatStyle.Flat;
            btnHoldBill.Click += BtnHoldBill_Click;

            Button btnViewHoldBills = new Button();
            btnViewHoldBills.Text = "View Hold Bills";
            btnViewHoldBills.Height = 38;
            btnViewHoldBills.Dock = DockStyle.Top;
            btnViewHoldBills.Margin = new Padding(0, 8, 0, 0);
            btnViewHoldBills.BackColor = Color.SteelBlue;
            btnViewHoldBills.ForeColor = Color.White;
            btnViewHoldBills.FlatStyle = FlatStyle.Flat;
            btnViewHoldBills.Click += BtnResumeBill_Click;

            customerLayout.Controls.Add(lblHoldSection, 0, 10);
            customerLayout.Controls.Add(btnHoldBill, 0, 11);
            customerLayout.Controls.Add(btnViewHoldBills, 0, 12);

            rightPanel.Controls.Add(customerLayout);

            lstHoldBills = new ListBox();
            lstHoldBills.Dock = DockStyle.Bottom;
            lstHoldBills.Height = 150;

            lstHoldBills.DoubleClick += LstHoldBills_DoubleClick;

            rightPanel.Controls.Add(lstHoldBills);

            dgvRight.CellEndEdit += DgvRight_CellEndEdit;
            dgvRight.RowsAdded += (s, e) => UpdateActionButtonsState();
            dgvRight.RowsRemoved += (s, e) => UpdateActionButtonsState();
            UpdateActionButtonsState();
            txtPaidAmount.TextChanged += (s, e) => CalculateReturnAmount();

            txtBarcode.Margin = new Padding(0, 10, 8, 10);

            btnReset.Margin = new Padding(0, 10, 8, 10);
            btnPrint.Margin = new Padding(0, 10, 8, 10);
        }

        private void CalculateReturnAmount()
        {
            decimal paid = 0;

            decimal.TryParse(txtPaidAmount.Text, out paid);

            txtBillAmount.Text = grandTotal.ToString("0.00");

            decimal balance = paid - grandTotal;

            if (balance >= 0)
            {
                lblReturnAmount.ForeColor = Color.Green;
                lblReturnAmount.Text = "₹" + balance.ToString("N2");
            }
            else
            {
                lblReturnAmount.ForeColor = Color.Red;
                lblReturnAmount.Text = "-₹" + Math.Abs(balance).ToString("N2");
            }
        }

        private void TxtMobile_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void TxtMobile_TextChanged(object sender, EventArgs e)
        {
            if (isLoadingHoldBill)
                return;

            string original = txtMobile.Text;
            string digitsOnly = new string(original.Where(char.IsDigit).Take(10).ToArray());

            if (original != digitsOnly)
            {
                int cursor = txtMobile.SelectionStart;
                txtMobile.Text = digitsOnly;
                txtMobile.SelectionStart = Math.Min(cursor, txtMobile.Text.Length);
            }

            // Reward remove if mobile changed
            string currentMobile = txtMobile.Text.Trim();

            if (rewardApplied &&
                !string.IsNullOrWhiteSpace(lastRewardMobile) &&
                currentMobile != lastRewardMobile)
            {
                RemoveRewardDiscount();

                lastRewardMobile = "";
                lastRewardCheckMobile = "";
                lastRewardCheckGrandTotal = -1;
            }

            if (txtMobile.Text.Length == 10)
            {
                LoadCustomerByMobile(txtMobile.Text.Trim());
                RefreshAutoDiscountsForCurrentCustomer();

                MaybeCheckRewardDiscount();
            }
            else
            {
                lblCustomerStatus.Text = "";
                currentCustomerId = 0;
                currentCustomerIsStaff = false;

                // Agar mobile clear kar diya gaya hai
                if (rewardApplied)
                {
                    RemoveRewardDiscount();
                    lastRewardMobile = "";
                }

                lastRewardCheckMobile = "";
                lastRewardCheckGrandTotal = -1;
            }
        }

        private void MaybeCheckRewardDiscount()
        {
            if (isLoadingHoldBill || rewardApplied)
                return;

            string mobile = txtMobile.Text.Trim();
            if (!IsValidMobile(mobile) || grandTotal <= 0)
                return;

            if (lastRewardCheckMobile == mobile &&
                lastRewardCheckGrandTotal == grandTotal)
                return;

            if (CheckRewardDiscount())
            {
                lastRewardCheckMobile = mobile;
                lastRewardCheckGrandTotal = grandTotal;
            }
        }

        private bool TryGetRewardCustomerInfo(
            MySqlConnection conn,
            string mobile,
            out int customerId,
            out int rewardLastOrderId)
        {
            customerId = 0;
            rewardLastOrderId = 0;

            using (MySqlCommand customerCmd =
                new MySqlCommand(@"
                SELECT
                    id,
                    IFNULL(reward_last_order_id,0) AS reward_last_order_id
                FROM inv_customers
                WHERE phone=@phone
                LIMIT 1",
                conn))
            {
                customerCmd.Parameters.AddWithValue("@phone", mobile);

                using (MySqlDataReader reader = customerCmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return false;

                    customerId = Convert.ToInt32(reader["id"]);
                    rewardLastOrderId =
                        Convert.ToInt32(reader["reward_last_order_id"]);
                    return true;
                }
            }
        }

        private decimal GetPreviousRewardPurchase(
            MySqlConnection conn,
            string mobile,
            int rewardLastOrderId)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return 0;

            MySqlCommand cmd =
                new MySqlCommand(@"
                    SELECT IFNULL(SUM(o.grand_total), 0)
                    FROM inv_orders o
                    INNER JOIN inv_customers c ON c.id = o.customer_id
                    WHERE c.phone = @phone
                      AND o.id > @lastRewardOrderId",
                conn);

            cmd.Parameters.AddWithValue("@phone", mobile.Trim());
            cmd.Parameters.AddWithValue("@lastRewardOrderId", rewardLastOrderId);

            return Round2(Convert.ToDecimal(cmd.ExecuteScalar()));
        }

        private decimal GetCurrentBillBeforeReward()
        {
            if (rewardApplied)
                return CalculateCurrentBillWithReward(0);

            return grandTotal > 0
                ? grandTotal
                : CalculateCurrentBillWithReward(0);
        }

        private decimal CalculateCurrentBillWithReward(decimal rewardDiscount)
        {
            decimal currentTotal = 0;

            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (!decimal.TryParse(row.Cells[3].Value?.ToString(), out decimal price) ||
                    !int.TryParse(row.Cells[4].Value?.ToString(), out int qty) ||
                    !decimal.TryParse(row.Cells["GSTPercent"].Value?.ToString(), out decimal gstPercent))
                {
                    continue;
                }

                decimal discount;
                if (IsManualDiscountRow(row))
                {
                    discount = GetCellDecimal(row, "Manual_Discount");
                }
                else
                {
                    discount =
                        ClampDiscount(
                            GetCellDecimal(row, "Auto_Discount") +
                            rewardDiscount);
                }

                CalculateLineAmounts(
                    price,
                    gstPercent,
                    discount,
                    qty,
                    out _,
                    out _,
                    out decimal lineTotal);

                currentTotal += lineTotal;
            }

            return Round2(currentTotal);
        }

        private bool TryGetRewardRule(
            MySqlConnection conn,
            decimal amount,
            out string membershipName,
            out decimal discountPercent,
            out decimal minPurchase)
        {
            membershipName = "";
            discountPercent = 0;
            minPurchase = 0;

            MySqlCommand cmd =
                new MySqlCommand(@"
                    SELECT discount_percent,
                           min_purchase
                    FROM inv_reward_discount_rules
                    WHERE min_purchase <= @amt
                    AND is_active = 1
                    ORDER BY min_purchase DESC
                    LIMIT 1", conn);

            cmd.Parameters.AddWithValue("@amt", amount);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.Read())
                    return false;

                discountPercent =
                    Convert.ToDecimal(reader["discount_percent"]);
                minPurchase =
                    Convert.ToDecimal(reader["min_purchase"]);
                membershipName =
                    $"₹{minPurchase:N0}+ ({discountPercent:0.##}% off)";

                return discountPercent > 0;
            }
        }

        private void EnsureAppliedRewardStillEligible()
        {
            if (isLoadingHoldBill || !rewardApplied)
                return;

            string mobile = txtMobile.Text.Trim();
            if (!IsValidMobile(mobile))
            {
                RemoveRewardDiscount();
                return;
            }

            try
            {
                using (MySqlConnection conn = DB.GetConnection())
                {
                    conn.Open();

                    TryGetRewardCustomerInfo(
                        conn,
                        mobile,
                        out int customerId,
                        out int rewardLastOrderId);

                    decimal previousPurchase =
                        GetPreviousRewardPurchase(
                            conn,
                            mobile,
                            rewardLastOrderId);

                    decimal currentBill =
                        GetCurrentBillBeforeReward();
                    decimal eligiblePurchase =
                        Round2(previousPurchase + currentBill);

                    if (!TryGetRewardRule(
                            conn,
                            eligiblePurchase,
                            out _,
                            out decimal eligibleRewardDiscount,
                            out _) ||
                        eligibleRewardDiscount != rewardDiscountPercent)
                    {
                        rewardPurchaseAmount = eligiblePurchase;
                        RemoveRewardDiscount();
                        return;
                    }

                    rewardPurchaseAmount = eligiblePurchase;
                }
            }
            catch
            {
            }
        }

        private bool CheckRewardDiscount()
        {
            try
            {
                string mobile = txtMobile.Text.Trim();
                if (!IsValidMobile(mobile))
                    return false;

                using (MySqlConnection conn = DB.GetConnection())
                {
                    conn.Open();

                    int rewardLastOrderId = 0;
                    TryGetRewardCustomerInfo(
                        conn,
                        mobile,
                        out _,
                        out rewardLastOrderId);

                    return CalculateRewardDiscount(
                        conn,
                        mobile,
                        rewardLastOrderId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }
        }

        private bool CalculateRewardDiscount(
            MySqlConnection conn,
            string mobile,
            int rewardLastOrderId)
        {
            decimal previousPurchase =
                GetPreviousRewardPurchase(conn, mobile, rewardLastOrderId);
            decimal currentBill = GetCurrentBillBeforeReward();
            decimal eligiblePurchase =
                Round2(previousPurchase + currentBill);

            if (currentBill <= 0)
                return false;

            if (!TryGetRewardRule(
                    conn,
                    eligiblePurchase,
                    out string membershipName,
                    out decimal rewardDiscount,
                    out _))
            {
                return false;
            }

            currentMembership = membershipName;
            rewardPurchaseAmount = eligiblePurchase;

            AskRewardRedemption(
                rewardDiscount,
                previousPurchase,
                currentBill,
                eligiblePurchase);
            return true;
        }

        private void RemoveRewardDiscount()
        {
            if (rewardDiscountPercent <= 0)
                return;

            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (IsManualDiscountRow(row))
                {
                    SetDiscountBreakdown(row, 0, GetCellDecimal(row, "Manual_Discount"), 0);
                    continue;
                }

                SetDiscountBreakdown(
                    row,
                    GetCellDecimal(row, "Auto_Discount"),
                    0,
                    0);
            }

            rewardApplied = false;
            rewardDiscountPercent = 0;
            rewardPurchaseAmount = 0;
            lastRewardCheckMobile = "";
            lastRewardCheckGrandTotal = -1;
            currentMembership = "";

            RecalculateRowAmounts();
        }

        private void AskRewardRedemption(
            decimal rewardDiscount,
            decimal previousPurchase,
            decimal currentBill,
            decimal eligiblePurchase)
        {
            DialogResult dr =
                MessageBox.Show(
                $"🎉 Reward Available!\n\n" +
                $"Previous Purchase : ₹{previousPurchase:N2}\n" +
                $"Current Bill      : ₹{currentBill:N2}\n" +
                $"-----------------------------\n" +
                $"Eligible Amount   : ₹{eligiblePurchase:N2}\n\n" +
                $"Reward Discount   : {rewardDiscount}%\n\n" +
                $"Do you want to redeem this reward discount?",
                "Reward Program",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (dr != DialogResult.Yes)
            {
                rewardApplied = false;
                rewardDiscountPercent = 0;
                currentMembership = "";
                rewardPurchaseAmount = eligiblePurchase;
                return;
            }

            rewardApplied = true;
            rewardDiscountPercent = rewardDiscount;
            lastRewardMobile = txtMobile.Text.Trim();

            ApplyRewardDiscount();
        }

        private void ApplyRewardDiscount()
        {
            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (IsManualDiscountRow(row))
                {
                    SetDiscountBreakdown(row, 0, GetCellDecimal(row, "Manual_Discount"), 0);
                    continue;
                }

                SetDiscountBreakdown(
                    row,
                    GetCellDecimal(row, "Auto_Discount"),
                    0,
                    rewardDiscountPercent);
            }

            RecalculateRowAmounts();
        }

        private void TxtMobile_Validating(object sender, CancelEventArgs e)
        {
            string mobile = txtMobile.Text.Trim();
            if (string.IsNullOrWhiteSpace(mobile))
                return; // walk-in customer allowed

            if (!IsValidMobile(mobile))
            {
                MessageBox.Show("Enter valid 10 digit mobile number.");
                e.Cancel = true;
            }
        }

        private TextBox CreateTextBox(int left, int top)
        {
            return new TextBox
            {
                Left = left,
                Top = top,
                Width = 220
            };
        }

        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string barcode = txtBarcode.Text.Trim();

                if (string.IsNullOrWhiteSpace(barcode))
                {
                    MessageBox.Show("Scan barcode first.");
                    return;
                }
                if (barcode.Length > 60)
                {
                    MessageBox.Show("Barcode format invalid.");
                    AddDraftScan(barcode, "Barcode format invalid.");
                    txtBarcode.Clear();
                    return;
                }

                // 🔥 VALIDATION FIRST
                bool isValid = ValidateItemBeforeLoad(barcode, out string validationReason);

                if (!isValid)
                {
                    AddDraftScan(barcode, validationReason);
                    txtBarcode.Clear();
                    return; // ❌ STOP HERE (VERY IMPORTANT)
                }

                // ✅ ONLY IF VALID
                bool loaded = LoadItem(barcode, out string loadReason);
                if (!loaded)
                {
                    AddDraftScan(barcode, loadReason);
                }
                else
                {
                    RemoveDraftIfExists(barcode);
                }

                txtBarcode.Clear();
            }
        }

        private bool ValidateItemBeforeLoad(string barcode, out string failureReason)
        {
            failureReason = "";
            using (MySqlConnection conn = DB.GetConnection())
            {
                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    failureReason = "Database connection failed.";
                    MessageBox.Show($"Database connection failed ❌\n{ex.Message}");
                    return false;
                }

                string query = @"
        SELECT s.quantity 
        FROM inv_items_master i
LEFT JOIN inv_stock s ON LOWER(TRIM(i.item_code)) = LOWER(TRIM(s.item_code))
        WHERE i.item_code = @code
        LIMIT 1";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@code", barcode);

                MySqlDataReader reader = cmd.ExecuteReader();

                // ❌ Item not found
                if (!reader.Read())
                {
                    failureReason = "Item not found.";
                    MessageBox.Show("Item not found ❌");
                    return false;
                }

                // ❌ Stock entry missing
                if (reader["quantity"] == DBNull.Value)
                {
                    failureReason = "Item stock entry missing.";
                    MessageBox.Show("Item stock me exist nahi karta ❌");
                    return false;
                }

                if (!int.TryParse(reader["quantity"]?.ToString(), out int availableQty))
                {
                    failureReason = "Invalid stock quantity.";
                    MessageBox.Show("Invalid stock quantity found for this item ❌");
                    return false;
                }

                // ❌ Out of stock
                if (availableQty <= 0)
                {
                    failureReason = "Out of stock.";
                    MessageBox.Show("Out of stock ❌");
                    return false;
                }

                reader.Close(); // IMPORTANT

                // 🔥 EXISTING QTY IN GRID (VERY IMPORTANT)
                int existingQty = 0;

                foreach (DataGridViewRow row in dgvRight.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    if (row.Cells["Item_Code"].Value != null &&
                        row.Cells["Item_Code"].Value.ToString() == barcode)
                    {
                        int.TryParse(row.Cells[4].Value?.ToString(), out existingQty);
                        break;
                    }
                }

                // 🔥 FINAL CHECK (MAIN LOGIC)
                if (existingQty + 1 > availableQty)
                {
                    failureReason = $"Stock: {availableQty}, Already Added: {existingQty}.";
                    MessageBox.Show($"Stock: {availableQty}, Already Added: {existingQty} ❌");
                    return false;
                }

                return true;
            }
        }

        private Bitmap GenerateBarcode(string text)
        {
            var writer = new BarcodeWriter<Bitmap>
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 180,
                    Height = 50,
                    Margin = 1
                },
                Renderer = new BitmapRenderer()
            };

            return writer.Write(text);
        }

        private bool LoadItem(string barcode, out string failureReason)
        {
            failureReason = "";
            using (MySqlConnection conn = DB.GetConnection())
            {
                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    failureReason = "Database connection failed.";
                    MessageBox.Show($"Database connection failed ❌\n{ex.Message}");
                    return false;
                }

                DB.EnsureAgeDiscountSchema(conn);

                string query = @"
                SELECT
                    i.id,
                    i.item_code,
                    i.item_name,
                    i.size,
                    IFNULL(i.color,'') AS color,
                    IFNULL(i.main_category,'') AS main_category,
                    IFNULL(i.sub_category,'') AS sub_category,
                    IFNULL(i.gender,'') AS gender,
                    s.date_added AS stock_date_added,
                    i.selling_price,
                    i.GST
                FROM inv_items_master i
                LEFT JOIN inv_stock s ON LOWER(TRIM(i.item_code)) = LOWER(TRIM(s.item_code))
                WHERE i.item_code=@item_code
                LIMIT 1;";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@item_code", barcode);

                MySqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    failureReason = "Item not found.";
                    MessageBox.Show("Item Not Found ❌");
                    return false;
                }

                if (!int.TryParse(reader["id"]?.ToString(), out int itemId))
                {
                    failureReason = "Invalid item id.";
                    MessageBox.Show("Invalid item id found ❌");
                    return false;
                }
                string itemcode = reader["item_code"].ToString();
                string name = reader["item_name"].ToString();
                string size = reader["size"].ToString();
                string color = reader["color"].ToString();
                string mainCategory = reader["main_category"].ToString();
                string subCategory = reader["sub_category"].ToString();
                string gender = reader["gender"].ToString();
                DateTime stockAddedOn = DateTime.Now;
                DateTime.TryParse(reader["stock_date_added"]?.ToString(), out stockAddedOn);
                if (!decimal.TryParse(reader["selling_price"]?.ToString(), out decimal finalPrice))
                {
                    failureReason = "Invalid selling price.";
                    MessageBox.Show("Invalid selling price found ❌");
                    return false;
                }
                if (!decimal.TryParse(reader["GST"]?.ToString(), out decimal gstPercent))
                {
                    failureReason = "Invalid GST value.";
                    MessageBox.Show("Invalid GST value found ❌");
                    return false;
                }

                reader.Close();

                // Prefer inv_stock.date_added (stock-in date) for age-based discount.
                // Do this AFTER closing reader to avoid "open DataReader" issues.
                try
                {
                    using var stockDateCmd = new MySqlCommand(
                        "SELECT date_added FROM inv_stock WHERE LOWER(TRIM(item_code))=LOWER(TRIM(@code)) LIMIT 1",
                        conn
                    );
                    stockDateCmd.Parameters.AddWithValue("@code", itemcode);
                    object sd = stockDateCmd.ExecuteScalar();
                    if (sd != null && DateTime.TryParse(sd.ToString(), out DateTime parsed))
                        stockAddedOn = parsed;
                }
                catch { }

                decimal defaultDiscount = GetAutoDiscountPercent(conn, itemcode ?? "", currentCustomerIsStaff);
                decimal currentRewardDiscount = rewardApplied ? rewardDiscountPercent : 0;
                decimal effectiveDiscount = ClampDiscount(defaultDiscount + currentRewardDiscount);
                //DebugDiscountDecision(itemcode ?? "", mainCategory ?? "", stockAddedOn, defaultDiscount);
                int qty = 1;

                CalculateLineAmounts(
                    finalPrice,
                    gstPercent,
                    effectiveDiscount,
                    qty,
                    out decimal subtotal,
                    out decimal gstAmount,
                    out decimal total
                );

                //decimal gross = finalPrice * qty;

                // 🔥 STOCK CHECK
                string stockQuery = "SELECT quantity FROM inv_stock WHERE item_code=@code LIMIT 1";
                MySqlCommand stockCmd = new MySqlCommand(stockQuery, conn);
                stockCmd.Parameters.AddWithValue("@code", itemcode);

                object stockResult = stockCmd.ExecuteScalar();

                if (stockResult == null)
                {
                    failureReason = "Item stock entry missing.";
                    MessageBox.Show("Item stock me exist nahi karta ❌");
                    return false;
                }

                if (!int.TryParse(stockResult?.ToString(), out int availableQty))
                {
                    failureReason = "Invalid stock quantity.";
                    MessageBox.Show("Invalid stock quantity found ❌");
                    return false;
                }

                if (availableQty <= 0)
                {
                    failureReason = "Out of stock.";
                    MessageBox.Show("Out of stock ❌");
                    return false;
                }

                // 🔥 CHECK EXISTING QTY IN GRID
                int existingQty = 0;
                DataGridViewRow existingRow = null;

                foreach (DataGridViewRow row in dgvRight.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    if (row.Cells["Item_Code"].Value != null &&
                        row.Cells["Item_Code"].Value.ToString() == itemcode)
                    {
                        int.TryParse(row.Cells[4].Value?.ToString(), out existingQty);
                        existingRow = row;
                        break;
                    }
                }

                // 🔥 FINAL CHECK (MAIN LOGIC)
                if (existingQty + 1 > availableQty)
                {
                    failureReason = $"Stock: {availableQty}, Already Added: {existingQty}.";
                    MessageBox.Show($"Stock: {availableQty}, Already Added: {existingQty} ❌");
                    return false;
                }

                // 🔥 IF ITEM ALREADY IN GRID → INCREMENT
                if (existingRow != null)
                {
                    qty = existingQty + 1;

                    bool isManual = IsManualDiscountRow(existingRow);

                    decimal discount;

                    decimal currentDiscount = GetCellDecimal(existingRow, 1);
                    decimal autoDiscount = defaultDiscount;
                    decimal manualDiscount = GetCellDecimal(existingRow, "Manual_Discount");
                    decimal rowRewardDiscount = rewardApplied ? rewardDiscountPercent : 0;

                    if (!isManual)
                    {
                        discount = ClampDiscount(autoDiscount + rowRewardDiscount);
                        SetDiscountBreakdown(existingRow, autoDiscount, 0, rowRewardDiscount);
                    }
                    else
                    {
                        if (manualDiscount <= 0)
                            manualDiscount = currentDiscount;

                        discount = ClampDiscount(manualDiscount);
                        SetDiscountBreakdown(existingRow, 0, manualDiscount, 0);
                    }

                    CalculateLineAmounts(
                        finalPrice,
                        gstPercent,
                        discount,
                        qty,
                        out subtotal,
                        out gstAmount,
                        out total);

                    decimal gross = Round2(finalPrice * qty);

                    existingRow.Cells[4].Value = qty;
                    existingRow.Cells[5].Value = gross;      // Gross
                    existingRow.Cells[6].Value = subtotal;   // Taxable
                    existingRow.Cells[7].Value = gstAmount;  // GST
                    existingRow.Cells[8].Value = total;      // Net

                    existingRow.Cells["Item_Id"].Value = itemId;
                    existingRow.Cells["Color"].Value = color;
                }
                else
                {
                    CalculateLineAmounts(
                        finalPrice,
                        gstPercent,
                        effectiveDiscount,
                        qty,
                        out subtotal,
                        out gstAmount,
                        out total);

                    decimal gross = Round2(finalPrice * qty);

                    dgvRight.Rows.Add(
                        name,               // 0 Item
                        effectiveDiscount, // 1 Discount %
                        size,               // 2 Size
                        finalPrice,         // 3 Price
                        qty,                // 4 Qty
                        gross,              // 5 Gross
                        subtotal,           // 6 Taxable
                        gstAmount,          // 7 GST
                        total,              // 8 Net
                        gstPercent,
                        itemcode,
                        itemId,
                        color,
                        0, // Discount_Manual
                        defaultDiscount,
                        0,
                        currentRewardDiscount
                    );
                }

                RecalculateTotals();
            }

            return true;
        }

        private int GetAgeMonths(DateTime addedOn, DateTime now)
        {
            DateTime a = addedOn.Date;
            DateTime n = now.Date;

            if (n < a)
                return 0;

            int months = (n.Year - a.Year) * 12 + (n.Month - a.Month);
            if (n.Day < a.Day)
                months -= 1;

            return Math.Max(0, months);
        }

        private decimal GetAutoDiscountPercent(MySqlConnection conn, string itemCode, string mainCategory, string subCategory, string gender, DateTime addedOn)
        {
            try
            {
                int ageMonths = GetAgeMonths(addedOn, DateTime.Now);

                // Rule precedence:
                // 1) More specific rule wins (item_code > category > sub_category > gender)
                // 2) If tie, higher min_age_months wins (closest threshold)
                // 3) If tie, higher discount_percent wins
                using var cmd = new MySqlCommand(@"
                    SELECT IFNULL(discount_percent,0)
                    FROM inv_age_discount_rules
                    WHERE is_active = 1
                      AND min_age_months <= @age
                      AND (item_code IS NULL OR TRIM(item_code) = '' OR LOWER(TRIM(item_code)) = LOWER(TRIM(@code)))
                      AND (main_category IS NULL OR TRIM(main_category) = '' OR LOWER(TRIM(main_category)) = LOWER(TRIM(@cat)))
                      AND (sub_category IS NULL OR TRIM(sub_category) = '' OR LOWER(TRIM(sub_category)) = LOWER(TRIM(@sub)))
                      AND (gender IS NULL OR TRIM(gender) = '' OR LOWER(TRIM(gender)) = LOWER(TRIM(@gender)))
                    ORDER BY
                      (
                        CASE WHEN item_code IS NOT NULL AND TRIM(item_code) <> '' THEN 8 ELSE 0 END +
                        CASE WHEN main_category IS NOT NULL AND TRIM(main_category) <> '' THEN 4 ELSE 0 END +
                        CASE WHEN sub_category IS NOT NULL AND TRIM(sub_category) <> '' THEN 2 ELSE 0 END +
                        CASE WHEN gender IS NOT NULL AND TRIM(gender) <> '' THEN 1 ELSE 0 END
                      ) DESC,
                      min_age_months DESC,
                      discount_percent DESC
                    LIMIT 1;", conn);

                cmd.Parameters.AddWithValue("@age", ageMonths);
                cmd.Parameters.AddWithValue("@code", (itemCode ?? "").Trim());
                cmd.Parameters.AddWithValue("@cat", (mainCategory ?? "").Trim());
                cmd.Parameters.AddWithValue("@sub", (subCategory ?? "").Trim());
                cmd.Parameters.AddWithValue("@gender", (gender ?? "").Trim());

                object val = cmd.ExecuteScalar();
                if (val == null)
                    return 0;

                if (!decimal.TryParse(val.ToString(), out decimal d))
                    return 0;

                if (d < 0) d = 0;
                if (d > 100) d = 100;
                return d;
            }
            catch
            {
                return 0;
            }
        }

        private decimal GetAutoDiscountPercent(MySqlConnection conn, string itemCode, bool isStaffCustomer)
        {
            try
            {
                using var cmd = new MySqlCommand(@"
                SELECT IFNULL(r.discount_percent,0)
                FROM inv_items_master i
                JOIN inv_stock s ON LOWER(TRIM(s.item_code)) = LOWER(TRIM(i.item_code))
                JOIN inv_age_discount_rules r ON r.is_active = 1
                WHERE LOWER(TRIM(i.item_code)) = LOWER(TRIM(@code))
                  AND r.min_age_months <= TIMESTAMPDIFF(MONTH, DATE(s.date_added), CURDATE())
                  AND (IFNULL(r.staff_only,0) = 0 OR @isStaff = 1)
                  AND (
                    (
                      r.item_code IS NOT NULL
                      AND TRIM(r.item_code) <> ''
                      AND LOWER(TRIM(r.item_code)) = LOWER(TRIM(i.item_code))
                    )
                    OR
                    (
                      (r.item_code IS NULL OR TRIM(r.item_code) = '')
                      AND (r.main_category IS NULL OR TRIM(r.main_category) = '' OR LOWER(TRIM(r.main_category)) = LOWER(TRIM(IFNULL(i.main_category,''))))
                      AND (r.sub_category IS NULL OR TRIM(r.sub_category) = '' OR LOWER(TRIM(r.sub_category)) = LOWER(TRIM(IFNULL(i.sub_category,''))))
                      AND (r.gender IS NULL OR TRIM(r.gender) = '' OR LOWER(TRIM(r.gender)) = LOWER(TRIM(IFNULL(i.gender,''))))
                    )
                  )
                ORDER BY
                  (
                    CASE WHEN IFNULL(r.staff_only,0) = 1 THEN 16 ELSE 0 END +
                    CASE WHEN r.item_code IS NOT NULL AND TRIM(r.item_code) <> '' THEN 8 ELSE 0 END +
                    CASE WHEN r.main_category IS NOT NULL AND TRIM(r.main_category) <> '' THEN 4 ELSE 0 END +
                    CASE WHEN r.sub_category IS NOT NULL AND TRIM(r.sub_category) <> '' THEN 2 ELSE 0 END +
                    CASE WHEN r.gender IS NOT NULL AND TRIM(r.gender) <> '' THEN 1 ELSE 0 END
                  ) DESC,
                  r.min_age_months DESC,
                  r.discount_percent DESC
                LIMIT 1;", conn);

                cmd.Parameters.AddWithValue("@code", (itemCode ?? "").Trim());
                cmd.Parameters.AddWithValue("@isStaff", isStaffCustomer ? 1 : 0);
                object val = cmd.ExecuteScalar();
                if (val == null)
                    return 0;

                decimal d = Convert.ToDecimal(val);
                if (d < 0) d = 0;
                if (d > 100) d = 100;
                return d;
            }
            catch
            {
                return 0;
            }
        }

        private void DebugDiscountDecision(string itemCode, string mainCategory, DateTime stockAddedOn, decimal discountPercent)
        {
            if (!DiscountDebugEnabled)
                return;

            int ageMonths = GetAgeMonths(stockAddedOn, DateTime.Now);
            MessageBox.Show($"DiscDebug\nCode: {itemCode}\nCat: {mainCategory}\nStockAdded: {stockAddedOn:yyyy-MM-dd}\nAgeMonths: {ageMonths}\nApplied%: {discountPercent:0.##}");
        }

        private void RecalculateTotals()
        {
            grandTotal = 0;
            totaltaxableamount = 0;
            totalgst = 0;
            hasShownRecalcDataError = false;

            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (row.IsNewRow)
                    continue;

                // Taxable = Cell[6]
                // GST     = Cell[7]
                // Net     = Cell[8]

                if (row.Cells[6].Value != null)
                {
                    if (!decimal.TryParse(row.Cells[6].Value?.ToString(), out decimal taxable) ||
                        !decimal.TryParse(row.Cells[7].Value?.ToString(), out decimal gst) ||
                        !decimal.TryParse(row.Cells[8].Value?.ToString(), out decimal total))
                    {
                        if (!hasShownRecalcDataError)
                        {
                            MessageBox.Show(
                                "Some row amount values are invalid. Please check Qty or rescan item.");

                            hasShownRecalcDataError = true;
                        }

                        continue;
                    }

                    totaltaxableamount += taxable;
                    totalgst += gst;
                    grandTotal += total;
                }
            }

            totaltaxableamount = Round2(totaltaxableamount);
            totalgst = Round2(totalgst);
            grandTotal = Round2(grandTotal);

            lblGrandTotal.Text =
                "Grand Total: " + grandTotal.ToString("0.00");

            CalculateReturnAmount();

            EnsureAppliedRewardStillEligible();
            MaybeCheckRewardDiscount();
        }



        private void TxtMobileSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                TriggerCustomerCheck();
            }
        }

        private void AddDraftScan(string barcode, string reason)
        {
            string trimmed = barcode?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            draftScans.Insert(0, new DraftScan
            {
                Time = DateTime.Now,
                Barcode = trimmed,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Not added." : reason.Trim()
            });

            UpdateDraftVisibility();
        }

        private void RemoveDraftIfExists(string barcode)
        {
            string trimmed = barcode?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            for (int i = draftScans.Count - 1; i >= 0; i--)
            {
                if (string.Equals(draftScans[i].Barcode, trimmed, StringComparison.OrdinalIgnoreCase))
                    draftScans.RemoveAt(i);
            }

            UpdateDraftVisibility();
        }

        private void UpdateDraftVisibility()
        {
            if (draftPanel == null)
                return;

            bool hasDraft = draftScans.Count > 0;
            draftPanel.Visible = hasDraft;
        }

        private void RetrySelectedDraft()
        {
            if (dgvDraft == null || dgvDraft.CurrentRow == null)
                return;

            if (dgvDraft.CurrentRow.DataBoundItem is not DraftScan selected)
                return;

            txtBarcode.Text = selected.Barcode;
            txtBarcode.Focus();

            bool isValid = ValidateItemBeforeLoad(selected.Barcode, out string validationReason);
            if (!isValid)
            {
                selected.Time = DateTime.Now;
                selected.Reason = validationReason;
                dgvDraft.Refresh();
                return;
            }

            bool loaded = LoadItem(selected.Barcode, out string loadReason);
            if (!loaded)
            {
                selected.Time = DateTime.Now;
                selected.Reason = loadReason;
                dgvDraft.Refresh();
                return;
            }

            RemoveDraftIfExists(selected.Barcode);
        }

        private void BtnCheckMobile_Click(object sender, EventArgs e)
        {
            TriggerCustomerCheck();
        }

        private void TriggerCustomerCheck()
        {
            string mobile = txtMobile.Text.Trim();
            if (!IsValidMobile(mobile))
            {
                MessageBox.Show("Enter valid 10 digit mobile number");
                txtMobile.Focus();
                return;
            }

            LoadCustomerByMobile(mobile);
            RefreshAutoDiscountsForCurrentCustomer();
        }



        private void LoadCustomerByMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return;

            using (MySqlConnection conn = DB.GetConnection())
            {
                conn.Open();
                DB.EnsureAgeDiscountSchema(conn);
                currentCustomerIsStaff = IsStaffMobile(conn, mobile);

                string query = "SELECT * FROM inv_customers WHERE phone=@phone";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@phone", mobile);

                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    if (!int.TryParse(reader["id"]?.ToString(), out currentCustomerId))
                    {
                        MessageBox.Show("Invalid customer id found.");
                        currentCustomerId = 0;
                        return;
                    }
                    txtName.Text = reader["first_name"].ToString();
                    txtSurname.Text = reader["sur_name"].ToString();

                    lblCustomerStatus.Text = currentCustomerIsStaff ? "Existing Customer (Staff)" : "✓ Existing Customer";
                    lblCustomerStatus.ForeColor = Color.Green;
                }
                else
                {
                    currentCustomerId = 0;
                    txtName.Clear();
                    txtSurname.Clear();

                    lblCustomerStatus.Text = currentCustomerIsStaff ? "Staff" : "New Customer";
                    lblCustomerStatus.ForeColor = currentCustomerIsStaff ? Color.Green : Color.DarkOrange;
                }
            }
        }

        private bool IsStaffMobile(MySqlConnection conn, string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return false;

            try
            {
                using var cmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM inv_users WHERE status=1 AND role='Staff' AND phone=@phone",
                    conn);
                cmd.Parameters.AddWithValue("@phone", mobile.Trim());
                object result = cmd.ExecuteScalar();
                return result != null && Convert.ToInt32(result) > 0;
            }
            catch
            {
                return false;
            }
        }

        private void RefreshAutoDiscountsForCurrentCustomer()
        {
            if (dgvRight.Rows.Count == 0)
                return;

            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                DB.EnsureAgeDiscountSchema(conn);

                foreach (DataGridViewRow row in dgvRight.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    string itemCode = row.Cells["Item_Code"].Value?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(itemCode))
                        continue;

                    bool isManual = IsManualDiscountRow(row);

                    decimal autoDiscount = GetAutoDiscountPercent(conn, itemCode, currentCustomerIsStaff);
                    decimal manualDiscount = isManual ? GetCellDecimal(row, "Manual_Discount") : 0;
                    decimal rowRewardDiscount = rewardApplied ? rewardDiscountPercent : GetCellDecimal(row, "Reward_Discount");

                    if (isManual)
                        SetDiscountBreakdown(row, 0, manualDiscount, 0);
                    else
                        SetDiscountBreakdown(row, autoDiscount, 0, rowRewardDiscount);
                }

                RecalculateRowAmounts();
            }
            catch
            {
            }
        }

        private void BtnUpdateCustomer_Click(object sender, EventArgs e)
        {
            string mobile = txtMobile.Text.Trim();
            if (!IsValidMobile(mobile))
            {
                MessageBox.Show("Enter valid 10 digit mobile number");
                return;
            }

            using (MySqlConnection conn = DB.GetConnection())
            {
                conn.Open();

                if (currentCustomerId == 0)
                {
                    // Insert new customer
                    string insertQuery = @"INSERT INTO inv_customers 
                (first_name, sur_name, phone, status, date_added)
                VALUES (@fname, @sname, @phone, 1, NOW())";

                    MySqlCommand cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@fname", txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("@sname", txtSurname.Text.Trim());
                    cmd.Parameters.AddWithValue("@phone", mobile);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Customer Added Successfully ✅");
                }
                else
                {
                    // Update existing customer
                    string updateQuery = @"UPDATE inv_customers 
                SET first_name=@fname,
                    sur_name=@sname,
                    phone=@phone,
                    date_updated=NOW()
                WHERE id=@id";

                    MySqlCommand cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@fname", txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("@sname", txtSurname.Text.Trim());
                    cmd.Parameters.AddWithValue("@phone", mobile);
                    cmd.Parameters.AddWithValue("@id", currentCustomerId);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Customer Updated Successfully ✅");
                }
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (!ValidateBill())
                return;

            btnPrint.Enabled = false;
            try
            {
                bool savedAndPrinted = SaveSaleAndPrint();
                if (!savedAndPrinted)
                    return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Print failed: " + ex.Message);
            }
            finally
            {
                UpdateActionButtonsState();
            }
        }

        private bool HasAtLeastOneItem()
        {
            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (!row.IsNewRow && row.Cells.Count > 0 && row.Cells[0].Value != null &&
                    !string.IsNullOrWhiteSpace(row.Cells[0].Value.ToString()))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateActionButtonsState()
        {
            if (dgvRight == null)
                return;

            bool hasItem = HasAtLeastOneItem();
            if (btnPrint != null)
            {
                btnPrint.Enabled = hasItem;
                btnPrint.BackColor = hasItem ? PrintEnabledColor : DisabledButtonColor;
            }
            if (btnReset != null)
            {
                btnReset.Enabled = hasItem;
                btnReset.BackColor = hasItem ? ResetEnabledColor : DisabledButtonColor;
            }
        }

        private int GetPrintableItemCount()
        {
            int itemCount = 0;
            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (!row.IsNewRow && row.Cells[0].Value != null)
                    itemCount++;
            }
            return itemCount;
        }

        private void ConfigureReceiptPaperSize()
        {
            int itemCount = GetPrintableItemCount();
            // Base includes header/footer + return policy block.
            // Keep some extra room for customer details + discount breakdown per item.
            int baseHeight = 490;
            int perItemHeight = 95;
            int dynamicHeight = baseHeight + (itemCount * perItemHeight);

            PaperSize customSize = new PaperSize("Custom", 300, dynamicHeight);
            printDocument.DefaultPageSettings.PaperSize = customSize;
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            int gatewayQuantity = 0;

            // ✅ Fix printer margin issue
            g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

            int pageWidth = e.PageSettings.PaperSize.Width;

            Font headerFont = new Font("Segoe UI", 12, FontStyle.Bold);
            Font normalFont = new Font("Segoe UI", 8);
            Font boldFont = new Font("Segoe UI", 8, FontStyle.Bold);
            Font totalFont = new Font("Segoe UI", 10, FontStyle.Bold);
            Font policyFont = new Font("Segoe UI", 7);
            Font policyHeaderFont = new Font("Segoe UI", 8, FontStyle.Bold);

            float y = 5;
            int itemNumber = 1;

            // ===== Header (CENTERED) =====
            string title = StoreName;
            SizeF titleSize = g.MeasureString(title, headerFont);
            g.DrawString(title, headerFont, Brushes.Black, (pageWidth - titleSize.Width) / 2, y);
            y += 20;
            string address1 = StoreAddressLine1;
            SizeF add1Size = g.MeasureString(address1, normalFont);
            g.DrawString(address1, normalFont, Brushes.Black, (pageWidth - add1Size.Width) / 2, y);
            y += 13;
            string address2 = StoreAddressLine2;
            SizeF add2Size = g.MeasureString(address2, normalFont);
            g.DrawString(address2, normalFont, Brushes.Black, (pageWidth - add2Size.Width) / 2, y);
            y += 15;

            g.DrawString($"Invoice: {lastOrderId}", normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("Date: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm"), normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("Payment: " + (cmbPaymentMethod?.Text ?? "Cash"), normalFont, Brushes.Black, 5, y);
            y += 15;

            // ===== Customer Details =====
            string customerName = $"{(txtName?.Text ?? "").Trim()} {(txtSurname?.Text ?? "").Trim()}".Trim();
            string customerMobile = (txtMobile?.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(customerName))
                customerName = "Walk-in";

            if (string.IsNullOrWhiteSpace(customerMobile))
                customerMobile = "-";

            g.DrawString($"Customer: {customerName}", normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString($"Mobile: {customerMobile}", normalFont, Brushes.Black, 5, y);
            y += 15;

            if (!string.IsNullOrWhiteSpace(currentMembership))
            {
                g.DrawString($"Membership: {currentMembership}",normalFont,Brushes.Black,5,y);
                y += 15;
            }

            g.DrawString(new string('-', 48), normalFont, Brushes.Black, 5, y);
            y += 15;

            // ===== Items =====
            for (int i = 0; i < dgvRight.Rows.Count; i++)
            {
                if (dgvRight.Rows[i].IsNewRow)
                    continue;

                if (dgvRight.Rows[i].Cells[0].Value != null)
                {
                    string name = dgvRight.Rows[i].Cells[0].Value.ToString();
                    string discountText = dgvRight.Rows[i].Cells[1].Value?.ToString() ?? "0";
                    string size = dgvRight.Rows[i].Cells[2].Value.ToString();
                    string itemCode = dgvRight.Rows[i].Cells["Item_Code"].Value?.ToString() ?? "";

                    if (!decimal.TryParse(dgvRight.Rows[i].Cells[3].Value?.ToString(), out decimal priceVal) ||
                     !int.TryParse(dgvRight.Rows[i].Cells[4].Value?.ToString(), out int qtyVal) ||
                     !decimal.TryParse(dgvRight.Rows[i].Cells[5].Value?.ToString(), out decimal grossVal) ||
                     !decimal.TryParse(dgvRight.Rows[i].Cells[6].Value?.ToString(), out decimal subtotalVal) ||
                     !decimal.TryParse(dgvRight.Rows[i].Cells[7].Value?.ToString(), out decimal gstVal) ||
                     !decimal.TryParse(dgvRight.Rows[i].Cells[8].Value?.ToString(), out decimal totalVal) ||
                     !decimal.TryParse(dgvRight.Rows[i].Cells["GSTPercent"].Value?.ToString(), out decimal gstPercent))
                    {
                        MessageBox.Show($"Invalid amount data in row {i + 1}. Please check bill before printing.");
                        e.HasMorePages = false;
                        return;
                    }

                    if (!decimal.TryParse(discountText, out decimal discountPercentVal))
                        discountPercentVal = 0;
                    if (discountPercentVal < 0)
                        discountPercentVal = 0;

                    decimal originalTotalInclTax = Round2(priceVal * qtyVal);
                    decimal discountAmountInclTax = Round2(originalTotalInclTax - totalVal);
                    if (discountAmountInclTax < 0)
                        discountAmountInclTax = 0;

                    gatewayQuantity = gatewayQuantity + qtyVal;

                    g.DrawString($"Item {itemNumber}: {name}", boldFont, Brushes.Black, 5, y);
                    y += 13;

                    g.DrawString($"Code: {itemCode}  Size : {size}", normalFont, Brushes.Black, 4, y);
                    y += 13;

                    //if (!string.IsNullOrWhiteSpace(size))
                    //{
                    //    g.DrawString($"Size : {size}", normalFont, Brushes.Black, 5, y);
                    //    y += 13;
                    //}

                    g.DrawString($"Price: {priceVal:0.00}  Qty: {qtyVal}  Gross: {grossVal:0.00}",normalFont,Brushes.Black,4,y);
                    y += 13;

                    //g.DrawString($"Gross : {grossVal:0.00}",normalFont,Brushes.Black,5,y);
                    //y += 13;

                    g.DrawString($"Discount : {discountPercentVal:0.##}% (-{discountAmountInclTax:0.00})",normalFont,Brushes.Black,4,y);
                    y += 13;

                    g.DrawString($"Taxable: {subtotalVal:0.00}  GST: {gstVal:0.00}",normalFont,Brushes.Black,4,y);
                    y += 13;

                    //g.DrawString($"GST     : {gstVal:0.00}",normalFont,Brushes.Black,5,y);
                    //y += 13;

                    g.DrawString($"Net: {totalVal:0.00}",boldFont,Brushes.Black,6,y);
                    y += 15;

                    g.DrawString("--------------------------------",normalFont,Brushes.Black,5,y);
                    y += 15;

                    itemNumber++;
                }
            }

            g.DrawString("Gate check quantity: " + gatewayQuantity.ToString(""), totalFont, Brushes.Black, 5, y);
            y += 15;

            // ===== Footer =====
            g.DrawString(new string('-', 48), normalFont, Brushes.Black, 5, y);
            y += 15;

            g.DrawString("GRAND TOTAL: " + grandTotal.ToString("0.00"), totalFont, Brushes.Black, 5, y);
            y += 22;

            g.DrawString("Taxable: " + totaltaxableamount.ToString("0.00"), normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("GST: " + totalgst.ToString("0.00"), normalFont, Brushes.Black, 5, y);
            y += 15;
            decimal cgstTotal = Round2(totalgst / 2m);
            decimal sgstTotal = Round2(totalgst - cgstTotal);
            g.DrawString("CGST: " + cgstTotal.ToString("0.00") + "  SGST: " + sgstTotal.ToString("0.00"), normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("Total: " + grandTotal.ToString("0.00"), normalFont, Brushes.Black, 5, y);
            y += 18;

            g.DrawString("Phone: " + StorePhone, normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("Email: " + StoreEmail, normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("Website: " + StoreWebsite, normalFont, Brushes.Black, 5, y);
            y += 22;

            // ===== Return Policy =====
            g.DrawString(new string('-', 48), normalFont, Brushes.Black, 5, y);
            y += 14;
            g.DrawString("Return Policy", policyHeaderFont, Brushes.Black, 5, y);
            y += 13;
            g.DrawString("1. Return window: within 7 days from bill date.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("2. Original bill/invoice required.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("3. Item must be unused, unwashed, tags and box intact.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("4. Garments (altered/stitched) are non-returnable.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("5. Footwear with used/dirty sole non-returnable; box required.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("6. Toys opened/damaged seal/battery-used usually non-returnable,", policyFont, Brushes.Black, 5, y);
            y += 10;
            g.DrawString("   unless manufacturing defect.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("7. Socks/innerwear/accessories mostly non-returnable.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("8. No cash refund. Exchange only for same or higher value item.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("9. Counter checks: barcode match, tag match, invoice match.", policyFont, Brushes.Black, 5, y);
            y += 16;

            // ===== BARCODE =====
            // ===== BARCODE (ORDER ID) =====
            string barcodeText = lastOrderId.ToString();
            Bitmap barcodeImage = GenerateBarcode(barcodeText);

            if (barcodeImage != null)
            {
                //int barcodeWidth = 180;
                //int barcodeHeight = 50;

                //float barcodeX = (pageWidth - barcodeWidth) / 2;

                //// 🔥 Barcode print
                //g.DrawImage(barcodeImage, barcodeX, y, barcodeWidth, barcodeHeight);
                //y += barcodeHeight + 5;

                int barcodeWidth = 180;
                int barcodeHeight = 50;

                float barcodeX = (pageWidth - barcodeWidth) / 2;

                // Barcode Print
                g.DrawImage(barcodeImage, barcodeX, y, barcodeWidth, barcodeHeight);
                y += barcodeHeight + 5;

                // Invoice Number
                string orderText = "" + lastOrderId;

                Font textFont = new Font("Segoe UI", 9, FontStyle.Bold);

                SizeF textSize = g.MeasureString(orderText, textFont);

                float textX = (pageWidth - textSize.Width) / 2;

                g.DrawString(orderText, textFont, Brushes.Black, textX, y);

                y += textSize.Height + 5;
            }

            // ===== Thank You =====
            string thankYou = "Thank you for shopping with us";
            SizeF thankSize = g.MeasureString(thankYou, normalFont);
            g.DrawString(thankYou, normalFont, Brushes.Black, (pageWidth - thankSize.Width) / 2, y);

            e.HasMorePages = false;
        }

        private bool SaveSaleAndPrint()
        {
            if (dgvRight.Rows.Count == 0)
            {
                MessageBox.Show("No items to save.");
                return false;
            }

            //if (!HasInternetConnection())
            //{
            //    MessageBox.Show("Internet connection is not available. Please connect to internet and try again.");
            //    return false;
            //}

            ConfigureReceiptPaperSize();
            PrinterRouting.ApplyReceiptReturnPrinter(printDocument);
            if (!printDocument.PrinterSettings.IsValid)
            {
                MessageBox.Show("No valid receipt printer found. Please install/select printer.");
                return false;
            }

            using (MySqlConnection conn = DB.GetConnection())
            {
                conn.Open();
                EnsureOrderPaymentColumns(conn);

                if (!RevalidateStockBeforeSave(conn, out string stockError))
                {
                    MessageBox.Show(stockError);
                    return false;
                }

                MySqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    int customerId = GetOrCreateCustomer(conn, transaction);
                    int orderId = InsertOrder(conn, transaction, customerId);
                    lastOrderId = orderId;
                    InsertOrderDetails(conn, transaction, orderId);
                    if (rewardApplied)
                    {
                        MySqlCommand rewardCmd =
                            new MySqlCommand(@"
                            UPDATE inv_customers
                            SET reward_last_order_id=@orderId
                            WHERE id=@customerId",
                            conn,
                            transaction);

                        rewardCmd.Parameters.AddWithValue("@orderId", orderId);
                        rewardCmd.Parameters.AddWithValue("@customerId", customerId);
                        rewardCmd.ExecuteNonQuery();
                    }

                    
                    // Uncomment for testing
                    //PrintPreviewDialog preview = new PrintPreviewDialog();
                    //preview.Document = printDocument;
                    //preview.Width = 1200;
                    //preview.Height = 800;
                    //preview.ShowDialog();

                    // Uncomment for production
                    printDocument.Print();

                    transaction.Commit();
                    MessageBox.Show("Order Saved Successfully ✅\nOrder ID: " + orderId);
                    ResetBill();
                    return true;
                }
                catch (Exception ex)
                {
                    try { transaction.Rollback(); } catch { }
                    MessageBox.Show("Print/Save failed: " + ex.Message);
                    return false;
                }
            }
        }

        private bool HasInternetConnection()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                using var request = new HttpRequestMessage(HttpMethod.Get, "http://clients3.google.com/generate_204");
                using HttpResponseMessage response = client.Send(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private bool RevalidateStockBeforeSave(MySqlConnection conn, out string error)
        {
            error = "";
            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string itemCode = row.Cells["Item_Code"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(itemCode))
                {
                    error = "Item code missing in one row. Please remove and rescan.";
                    return false;
                }

                if (!int.TryParse(row.Cells[4].Value?.ToString(), out int qty) || qty <= 0)
                {
                    error = "Invalid quantity detected. Please update quantity and try again.";
                    return false;
                }

                using var cmd = new MySqlCommand(
                    "SELECT s.quantity FROM inv_items_master i LEFT JOIN inv_stock s ON LOWER(TRIM(i.item_code))=LOWER(TRIM(s.item_code)) WHERE i.item_code=@code LIMIT 1",
                    conn);
                cmd.Parameters.AddWithValue("@code", itemCode);
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    error = $"Item '{itemCode}' not found or stock entry missing. Please rescan.";
                    return false;
                }

                if (!int.TryParse(result.ToString(), out int availableQty))
                {
                    error = $"Invalid stock quantity for item '{itemCode}'. Please try again.";
                    return false;
                }

                if (availableQty < qty)
                {
                    error = $"Stock not enough for item '{itemCode}'. Available: {availableQty}, Selected: {qty}.";
                    return false;
                }
            }

            return true;
        }

        private int GetOrCreateCustomer(MySqlConnection conn, MySqlTransaction transaction)
        {
            // 👉 WALK-IN CUSTOMER (no mobile)
            if (string.IsNullOrWhiteSpace(txtMobile.Text))
            {
                MySqlCommand insertWalkIn = new MySqlCommand(
                    @"INSERT INTO inv_customers(first_name, sur_name, phone, status, date_added)
              VALUES('Walk-in', 'Customer', NULL, 1, NOW());
              SELECT LAST_INSERT_ID();",
                    conn, transaction);

                object walkInId = insertWalkIn.ExecuteScalar();
                if (walkInId == null || !int.TryParse(walkInId.ToString(), out int createdWalkInId))
                    throw new Exception("Failed to create walk-in customer id.");
                return createdWalkInId;
            }

            // 👉 EXISTING CUSTOMER CHECK
            MySqlCommand checkCmd = new MySqlCommand(
                "SELECT id FROM inv_customers WHERE phone=@phone LIMIT 1",
                conn, transaction);

            checkCmd.Parameters.AddWithValue("@phone", txtMobile.Text.Trim());

            object result = checkCmd.ExecuteScalar();

            if (result != null)
            {
                if (!int.TryParse(result.ToString(), out int existingCustomerId))
                    throw new Exception("Invalid customer id found for this mobile.");
                return existingCustomerId;
            }

            // 👉 NEW CUSTOMER INSERT
            MySqlCommand insertCmd = new MySqlCommand(
            @"INSERT INTO inv_customers
                (
                    first_name,
                    sur_name,
                    phone,
                    status,
                    reward_last_order_id,
                    date_added
                )
                VALUES
                (
                    @name,
                    @surname,
                    @phone,
                    1,
                    0,
                    NOW()
                );

                SELECT LAST_INSERT_ID();",
            conn,
            transaction);

            insertCmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
            insertCmd.Parameters.AddWithValue("@surname", txtSurname.Text.Trim());
            insertCmd.Parameters.AddWithValue("@phone", txtMobile.Text.Trim());

            object insertedCustomerId = insertCmd.ExecuteScalar();

            if (insertedCustomerId == null ||
                !int.TryParse(insertedCustomerId.ToString(), out int newCustomerId))
            {
                throw new Exception("Failed to create customer id.");
            }

            return newCustomerId;
        }

        private int InsertOrder(
    MySqlConnection conn,
    MySqlTransaction transaction,
    int customerId)
        {
            decimal subtotal = 0;      // Taxable Total
            decimal tax = 0;           // GST Total
            decimal totalDiscount = 0; // Discount Total

            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (!decimal.TryParse(
                        row.Cells[5].Value?.ToString(),
                        out decimal gross))
                {
                    throw new Exception(
                        "Invalid gross amount found while saving order.");
                }

                if (!decimal.TryParse(
                        row.Cells[6].Value?.ToString(),
                        out decimal taxable))
                {
                    throw new Exception(
                        "Invalid taxable amount found while saving order.");
                }

                if (!decimal.TryParse(
                        row.Cells[7].Value?.ToString(),
                        out decimal gst))
                {
                    throw new Exception(
                        "Invalid GST amount found while saving order.");
                }

                subtotal += taxable;
                tax += gst;

                decimal discountAmount =
                    Round2(gross - (taxable + gst));

                totalDiscount += discountAmount;
            }

            subtotal = Round2(subtotal);
            tax = Round2(tax);
            totalDiscount = Round2(totalDiscount);

            using MySqlCommand cmd = new MySqlCommand(@"
        INSERT INTO inv_orders
        (
            customer_id,
            subtotal,
            total_discount,
            total_tax,
            grand_total,
            payment_method,
            created_by,
            date_added
        )
        VALUES
        (
            @cid,
            @sub,
            @discount,
            @tax,
            @grand,
            @pmethod,
            @createdBy,
            NOW()
        );

        SELECT LAST_INSERT_ID();",
                conn,
                transaction);

            cmd.Parameters.AddWithValue("@cid", customerId);

            cmd.Parameters.AddWithValue("@sub", subtotal);

            cmd.Parameters.AddWithValue(
                "@discount",
                totalDiscount);

            cmd.Parameters.AddWithValue("@tax", tax);

            cmd.Parameters.AddWithValue(
                "@grand",
                grandTotal);

            cmd.Parameters.AddWithValue(
                "@pmethod",
                (cmbPaymentMethod?.Text ?? "Cash").Trim());

            cmd.Parameters.AddWithValue(
                "@createdBy",
                string.IsNullOrWhiteSpace(
                    LoginForm.LoggedInUser)
                        ? "Unknown"
                        : LoginForm.LoggedInUser);

            object orderIdObj =
                cmd.ExecuteScalar();

            if (orderIdObj == null ||
                !int.TryParse(
                    orderIdObj.ToString(),
                    out int orderId))
            {
                throw new Exception(
                    "Failed to create order id.");
            }

            return orderId;
        }

        private void InsertOrderDetails(
    MySqlConnection conn,
    MySqlTransaction transaction,
    int orderId)
        {
            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (row.IsNewRow || row.Cells[0].Value == null)
                    continue;

                string itemCode =
                    row.Cells["Item_Code"].Value?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(itemCode))
                    throw new Exception(
                        "Item code missing while saving order details.");

                object itemIdObj = row.Cells["Item_Id"].Value;

                if (itemIdObj == null ||
                    string.IsNullOrWhiteSpace(itemIdObj.ToString()))
                {
                    throw new Exception(
                        $"Item id missing for item code: {itemCode}");
                }

                if (!int.TryParse(itemIdObj.ToString(), out int itemId))
                {
                    throw new Exception(
                        $"Invalid item id for item code: {itemCode}");
                }

                if (!int.TryParse(
                        row.Cells[4].Value?.ToString(),
                        out int qty) ||
                    qty <= 0)
                {
                    throw new Exception(
                        $"Invalid quantity for item code: {itemCode}");
                }

                decimal.TryParse(
                    row.Cells[1].Value?.ToString(),
                    out decimal discountPercent);

                if (!decimal.TryParse(
                        row.Cells[3].Value?.ToString(),
                        out decimal price) ||

                    !decimal.TryParse(
                        row.Cells[5].Value?.ToString(),
                        out decimal gross) ||

                    !decimal.TryParse(
                        row.Cells[6].Value?.ToString(),
                        out decimal taxable) ||

                    !decimal.TryParse(
                        row.Cells[7].Value?.ToString(),
                        out decimal gst) ||

                    !decimal.TryParse(
                        row.Cells[8].Value?.ToString(),
                        out decimal net))
                {
                    throw new Exception(
                        $"Invalid amount values for item code: {itemCode}");
                }

                decimal discountAmount =
                    Round2(gross - (taxable + gst));

                using MySqlCommand cmd = new MySqlCommand(@"
            INSERT INTO inv_order_details
            (
                order_id,
                item_id,
                qty,
                selling_price,
                gross_amount,
                discount_percent,
                discount_amount,
                taxable_amount,
                gst_amount,
                net_amount
            )
            VALUES
            (
                @oid,
                @iid,
                @qty,
                @price,
                @gross,
                @discountPercent,
                @discountAmount,
                @taxable,
                @gst,
                @net
            )",
                    conn,
                    transaction);

                cmd.Parameters.AddWithValue("@oid", orderId);
                cmd.Parameters.AddWithValue("@iid", itemId);
                cmd.Parameters.AddWithValue("@qty", qty);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@gross", gross);

                cmd.Parameters.AddWithValue("@discountPercent", discountPercent);

                cmd.Parameters.AddWithValue(
                    "@discountAmount",
                    discountAmount);

                cmd.Parameters.AddWithValue(
                    "@taxable",
                    taxable);

                cmd.Parameters.AddWithValue(
                    "@gst",
                    gst);

                cmd.Parameters.AddWithValue(
                    "@net",
                    net);

                cmd.ExecuteNonQuery();

                UpdateStock(
                    conn,
                    transaction,
                    itemCode,
                    qty);
            }
        }

        private void UpdateStock(MySqlConnection conn, MySqlTransaction transaction, string itemCode, int qty)
        {
            string query = @"UPDATE inv_stock 
                     SET quantity = quantity - @qty,
                         last_updated = NOW()
                     WHERE item_code = @code AND quantity >= @qty";

            MySqlCommand cmd = new MySqlCommand(query, conn, transaction);

            cmd.Parameters.AddWithValue("@qty", qty);
            cmd.Parameters.AddWithValue("@code", itemCode);

            int affectedRows = cmd.ExecuteNonQuery();
            if (affectedRows == 0)
                throw new Exception($"Stock update failed for item {itemCode}. Please recheck available quantity.");
        }

        private void DgvRight_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 4 || e.ColumnIndex == 1)
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvRight.Rows.Count)
                    return;

                DataGridViewRow row = dgvRight.Rows[e.RowIndex];
                if (row.IsNewRow)
                    return;

                if (row.Cells[4].Value == null)
                {
                    row.Cells[4].Value = 1;
                    RecalculateRowAmounts();
                    return;
                }

                int qty;

                if (!int.TryParse(row.Cells[4].Value.ToString(), out qty))
                {
                    MessageBox.Show("Invalid Quantity");
                    row.Cells[4].Value = 1;
                    RecalculateRowAmounts();
                    return;
                }

                if (qty <= 0)
                {
                    dgvRight.Rows.RemoveAt(e.RowIndex);
                    RecalculateTotals();
                    return;
                }

                string itemCode = row.Cells["Item_Code"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(itemCode))
                {
                    MessageBox.Show("Item code missing in this row. Please remove and rescan.");
                    return;
                }

                decimal refreshedAutoDiscount = GetCellDecimal(row, "Auto_Discount");

                // 🔥 STOCK CHECK (same as before)
                using (MySqlConnection conn = DB.GetConnection())
                {
                    conn.Open();

                    string query = "SELECT quantity FROM inv_stock WHERE item_code=@code LIMIT 1";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@code", itemCode);

                    object result = cmd.ExecuteScalar();

                    if (result == null)
                    {
                        MessageBox.Show("Stock not found ❌");
                        row.Cells[4].Value = 1;
                        RecalculateRowAmounts();
                        return;
                    }

                    if (!int.TryParse(result?.ToString(), out int availableQty))
                    {
                        MessageBox.Show("Stock quantity invalid for this item ❌");
                        return;
                    }

                    if (qty > availableQty)
                    {
                        MessageBox.Show($"Only {availableQty} items available ❌");

                        if (availableQty <= 0)
                        {
                            dgvRight.Rows.RemoveAt(e.RowIndex);
                            RecalculateTotals();
                            return;
                        }

                        int newQty = availableQty;
                        row.Cells[4].Value = newQty;

                        // 🔥 IMPORTANT: recalculate after fixing qty
                        if (!decimal.TryParse(row.Cells[3].Value?.ToString(), out decimal Nprice) ||
                            !decimal.TryParse(row.Cells["GSTPercent"].Value?.ToString(), out decimal NgstPercent))
                        {
                            MessageBox.Show("Price/GST data invalid. Please rescan item.");
                            return;
                        }

                        decimal Ndiscount = 0;

                        decimal.TryParse(
                            row.Cells[1].Value?.ToString(),
                            out Ndiscount
                        );

                        CalculateLineAmounts(
                                            Nprice,
                                            NgstPercent,
                                            Ndiscount,
                                            newQty,
                                            out decimal Nsubtotal,
                                            out decimal NgstAmount,
                                            out decimal Ntotal
                                        );

                        decimal Ngross = Round2(Nprice * newQty);

                        row.Cells[5].Value = Ngross;      // Gross
                        row.Cells[6].Value = Nsubtotal;   // Taxable
                        row.Cells[7].Value = NgstAmount;  // GST
                        row.Cells[8].Value = Ntotal;      // Net

                        RecalculateTotals();

                        return;
                    }

                    refreshedAutoDiscount = GetAutoDiscountPercent(conn, itemCode, currentCustomerIsStaff);
                }

                // 🔥 ✅ CORRECT CALCULATION (same as LoadItem)
                if (!decimal.TryParse(row.Cells[3].Value?.ToString(), out decimal price) ||
                    !decimal.TryParse(row.Cells["GSTPercent"].Value?.ToString(), out decimal gstPercent))
                {
                    MessageBox.Show("Price/GST data invalid. Please rescan item.");
                    return;
                }

                decimal discount = GetCellDecimal(row, 1);
                decimal rewardDiscount = rewardApplied ? rewardDiscountPercent : GetCellDecimal(row, "Reward_Discount");

                // Mark discount as manual edit if user edited the Discount % column.
                if (e.ColumnIndex == 1)
                {
                    row.Cells["Discount_Manual"].Value = 1;
                    decimal manualDiscount = ClampDiscount(discount);
                    SetDiscountBreakdown(row, 0, manualDiscount, 0);
                    discount = GetCellDecimal(row, 1);
                }
                else
                {
                    bool isManual = IsManualDiscountRow(row);

                    decimal manualDiscount = isManual ? GetCellDecimal(row, "Manual_Discount") : 0;
                    if (isManual)
                        SetDiscountBreakdown(row, 0, manualDiscount, 0);
                    else
                        SetDiscountBreakdown(row, refreshedAutoDiscount, 0, rewardDiscount);
                    discount = GetCellDecimal(row, 1);
                }

                CalculateLineAmounts(
                    price,
                    gstPercent,
                    discount,
                    qty,
                    out decimal subtotal,
                    out decimal gstAmount,
                    out decimal total
                );

                decimal gross = Round2(price * qty);

                row.Cells[5].Value = gross;
                row.Cells[6].Value = subtotal;
                row.Cells[7].Value = gstAmount;
                row.Cells[8].Value = total;

                RecalculateTotals();
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Reset current bill?",
                "Confirm",
                MessageBoxButtons.YesNo);

            if (result != DialogResult.Yes)
                return;

            dgvRight.Rows.Clear();
            draftScans.Clear();
            UpdateDraftVisibility();

            grandTotal = 0;
            totaltaxableamount = 0;
            totalgst = 0;

            lblGrandTotal.Text = "Grand Total: 0.00";

            txtMobile.Clear();
            txtName.Clear();
            txtSurname.Clear();
            if (cmbPaymentMethod != null)
                cmbPaymentMethod.SelectedIndex = 0;

            currentCustomerId = 0;
            currentCustomerIsStaff = false;
            rewardApplied = false;
            rewardDiscountPercent = 0;
            rewardPurchaseAmount = 0;
            lastRewardMobile = "";
            lastRewardCheckMobile = "";
            lastRewardCheckGrandTotal = -1;

            txtBarcode.Focus();
        }

        private void ResetBill()
        {
            dgvRight.Rows.Clear();

            grandTotal = 0;
            totaltaxableamount = 0;
            totalgst = 0;

            lblGrandTotal.Text = "Grand Total: 0.00";

            txtMobile.Clear();
            txtName.Clear();
            txtSurname.Clear();
            if (cmbPaymentMethod != null)
                cmbPaymentMethod.SelectedIndex = 0;

            currentCustomerId = 0;
            currentCustomerIsStaff = false;

            rewardApplied = false;
            rewardDiscountPercent = 0;
            rewardPurchaseAmount = 0;
            lastRewardMobile = "";
            lastRewardCheckMobile = "";
            lastRewardCheckGrandTotal = -1;
            currentMembership = "";

            txtBarcode.Focus();
        }

        private bool ValidateBill()
        {
            int validRowCount = 0;
            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (!row.IsNewRow && row.Cells[0].Value != null)
                    validRowCount++;
            }

            if (validRowCount == 0)
            {
                MessageBox.Show("Please scan at least one item.");
                txtBarcode.Focus();
                return false;
            }

            string mobile = txtMobile.Text.Trim();
            if (!string.IsNullOrWhiteSpace(mobile) && !IsValidMobile(mobile))
            {
                MessageBox.Show("Enter valid 10 digit mobile number.");
                txtMobile.Focus();
                return false;
            }

            if (cmbPaymentMethod == null || string.IsNullOrWhiteSpace(cmbPaymentMethod.Text))
            {
                MessageBox.Show("Please select payment method.");
                cmbPaymentMethod?.Focus();
                return false;
            }

            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (row.Cells[4].Value == null || string.IsNullOrWhiteSpace(row.Cells[4].Value.ToString()))
                {
                    MessageBox.Show("Invalid quantity detected.");
                    return false;
                }

                int qty;

                if (!int.TryParse(row.Cells[4].Value.ToString(), out qty))
                {
                    MessageBox.Show("Invalid quantity.");
                    return false;
                }

                if (qty <= 0)
                {
                    MessageBox.Show("Quantity must be greater than 0.");
                    return false;
                }

                if (row.Cells["Item_Code"].Value == null || string.IsNullOrWhiteSpace(row.Cells["Item_Code"].Value.ToString()))
                {
                    MessageBox.Show("Item code missing in one row. Please remove and rescan.");
                    return false;
                }

                if (row.Cells["Item_Id"].Value == null || string.IsNullOrWhiteSpace(row.Cells["Item_Id"].Value.ToString()))
                {
                    MessageBox.Show("Item id missing in one row. Please remove and rescan.");
                    return false;
                }

                if (!decimal.TryParse(row.Cells[3].Value?.ToString(), out decimal price) ||
                    !decimal.TryParse(row.Cells[5].Value?.ToString(), out decimal gross) ||
                    !decimal.TryParse(row.Cells[6].Value?.ToString(), out decimal taxable) ||
                    !decimal.TryParse(row.Cells[7].Value?.ToString(), out decimal gst) ||
                    !decimal.TryParse(row.Cells[8].Value?.ToString(), out decimal net) ||
                    !decimal.TryParse(row.Cells["GSTPercent"].Value?.ToString(), out decimal gstPercent))
                {
                    MessageBox.Show("Invalid amount data detected in bill.");
                    return false;
                }

                decimal discount = GetCellDecimal(row, 1);

                if (price < 0 || gross < 0 || taxable < 0 || gst < 0 || net < 0 || discount < 0)
                {
                    MessageBox.Show("Negative amount detected in bill.");
                    return false;
                }

                decimal expectedGross = Round2(price * qty);
                CalculateLineAmounts(
                    price,
                    gstPercent,
                    discount,
                    qty,
                    out decimal expectedTaxable,
                    out decimal expectedGst,
                    out decimal expectedLineNet);

                if (Math.Abs(expectedGross - gross) > 0.01m ||
                    Math.Abs(expectedTaxable - taxable) > 0.01m ||
                    Math.Abs(expectedGst - gst) > 0.01m ||
                    Math.Abs(expectedLineNet - net) > 0.01m)
                {
                    MessageBox.Show(
                        $"Amount mismatch in item: {row.Cells[0].Value}\n" +
                        $"Discount={discount}%\n" +
                        $"Expected Taxable={expectedTaxable}\n" +
                        $"Expected GST={expectedGst}\n" +
                        $"Expected Net={expectedLineNet}\n" +
                        $"Current Taxable={taxable}\n" +
                        $"Current GST={gst}\n" +
                        $"Current Net={net}");
                    return false;
                }

                decimal expectedNet = Round2(taxable + gst);

                if (Math.Abs(expectedNet - net) > 0.01m)
                {
                    MessageBox.Show(
                        $"Amount mismatch.\n" +
                        $"Taxable={taxable}\n" +
                        $"GST={gst}\n" +
                        $"Net={net}\n" +
                        $"Expected={expectedNet}");
                    return false;
                }
            }

            return true;
        }

        private void EnsureOrderPaymentColumns(MySqlConnection conn)
        {
            EnsureColumnExists(conn, "inv_orders", "payment_method", "VARCHAR(40) NOT NULL DEFAULT 'Cash'");
        }

        private void EnsureColumnExists(MySqlConnection conn, string tableName, string columnName, string definition)
        {
            using (MySqlCommand checkCmd = new MySqlCommand($"SHOW COLUMNS FROM `{tableName}` LIKE @col", conn))
            {
                checkCmd.Parameters.AddWithValue("@col", columnName);
                object result = checkCmd.ExecuteScalar();
                if (result != null)
                    return;
            }

            using (MySqlCommand alterCmd = new MySqlCommand($"ALTER TABLE `{tableName}` ADD COLUMN `{columnName}` {definition}", conn))
            {
                alterCmd.ExecuteNonQuery();
            }
        }

        private bool IsValidMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length != 10)
                return false;

            foreach (char ch in mobile)
            {
                if (!char.IsDigit(ch))
                    return false;
            }

            return true;
        }

        private void RecalculateRowAmounts()
        {
            foreach (DataGridViewRow row in dgvRight.Rows)
            {
                if (row.IsNewRow)
                    continue;

                decimal price =
                    Convert.ToDecimal(row.Cells[3].Value);

                int qty =
                    Convert.ToInt32(row.Cells[4].Value);

                if (IsManualDiscountRow(row))
                {
                    SetDiscountBreakdown(row, 0, GetCellDecimal(row, "Manual_Discount"), 0);
                }
                else
                {
                    SetDiscountBreakdown(
                        row,
                        GetCellDecimal(row, "Auto_Discount"),
                        0,
                        rewardApplied ? rewardDiscountPercent : GetCellDecimal(row, "Reward_Discount"));
                }

                decimal discount =
                    GetCellDecimal(row, 1);

                decimal gstPercent =
                    Convert.ToDecimal(
                        row.Cells["GSTPercent"].Value);

                CalculateLineAmounts(
                    price,
                    gstPercent,
                    discount,
                    qty,
                    out decimal taxable,
                    out decimal gstAmount,
                    out decimal net);

                decimal gross =
                    Round2(price * qty);

                // NEW MAPPING
                row.Cells[5].Value = gross;
                row.Cells[6].Value = taxable;
                row.Cells[7].Value = gstAmount;
                row.Cells[8].Value = net;
            }

            RecalculateTotals();
        }
    }
}
