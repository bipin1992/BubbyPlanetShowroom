using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace BubbyPlanetShowroom
{
    public class SellingPrice : UserControl
    {
        private static readonly Color PageBg = Color.FromArgb(241, 245, 249);
        private static readonly Color Slate = Color.FromArgb(15, 23, 42);
        private static readonly Color HeaderBg = Color.FromArgb(30, 41, 59);
        private static readonly Color Teal = Color.FromArgb(13, 148, 136);
        private static readonly Color Sky = Color.FromArgb(14, 165, 233);
        private static readonly Color Muted = Color.FromArgb(100, 116, 139);
        private static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);
        private static readonly Color Gold = Color.FromArgb(245, 158, 11);

        private readonly bool canUsePage;
        private readonly bool canEditSettings;
        private PricingSettings settings = PricingSettings.CreateDefaults();
        private bool suppressCalc;

        private TextBox txtTransport;
        private TextBox txtQty;
        private TextBox txtItemPrice;
        private TextBox txtRent;
        private TextBox txtSalary;
        private TextBox txtExpectedSales;
        private TextBox txtEnding;

        private Label? lblSellingPrice;
        private Label lblFinalPrice;
        private Label lblActualProfit;
        private Label lblStatus;
        private DataGridView dgvBreakdown;

        private NumericUpDown nudRent;
        private NumericUpDown nudSalary;
        private NumericUpDown nudExpectedSales;
        private NumericUpDown nudEnding;
        private DataGridView dgvSlabs;

        public SellingPrice(string role = "")
        {
            string currentRole = (role ?? "").Trim();
            canUsePage = currentRole is "Master Admin" or "Admin";
            canEditSettings = string.Equals(currentRole, "Master Admin", StringComparison.OrdinalIgnoreCase);
            InitializeUI();
            Load += (_, _) =>
            {
                if (canUsePage)
                    LoadSettingsFromDb();
            };
        }

        private void InitializeUI()
        {
            Dock = DockStyle.Fill;
            BackColor = PageBg;
            Padding = new Padding(12);

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = PageBg
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            Controls.Add(root);

            if (!canUsePage)
            {
                Label denied = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "Selling Price is available only to Master Admin and Admin.",
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Slate,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                Controls.Add(denied);
                denied.BringToFront();
                return;
            }

            Panel header = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };
            header.Paint += (_, e) =>
            {
                Rectangle bounds = header.ClientRectangle;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using LinearGradientBrush brush = new LinearGradientBrush(bounds, Teal, Sky, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(brush, bounds);
                using Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "Selling Price Calculator", titleFont,
                    new Rectangle(16, 10, 480, 28), Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                using Font hintFont = new Font("Segoe UI", 8.5f);
                TextRenderer.DrawText(e.Graphics, "Is item ka transport, quantity aur per piece rate daalo  ·  selling price turant dikhega", hintFont,
                    new Rectangle(16, 38, 720, 20), Color.FromArgb(204, 251, 241),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };
            root.Controls.Add(header, 0, 0);

            TabControl tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f)
            };
            TabPage calcPage = new TabPage("Calculate");
            TabPage settingsPage = new TabPage("Pricing Settings");
            calcPage.BackColor = PageBg;
            settingsPage.BackColor = PageBg;
            calcPage.Padding = new Padding(0, 8, 0, 0);
            settingsPage.Padding = new Padding(0, 8, 0, 0);
            calcPage.Controls.Add(BuildCalculatorTab());
            settingsPage.Controls.Add(BuildSettingsTab());
            tabs.TabPages.Add(calcPage);
            tabs.TabPages.Add(settingsPage);
            root.Controls.Add(tabs, 0, 1);

            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Muted,
                Text = "Ready."
            };
            root.Controls.Add(lblStatus, 0, 2);
        }

        private Control BuildCalculatorTab()
        {
            TableLayoutPanel split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = PageBg
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));

            Panel inputCard = CreateCard();
            inputCard.Padding = new Padding(1);
            inputCard.Margin = new Padding(0, 0, 10, 0);
            Panel inputHeader = CreateSectionHeader("INPUTS", "Is item ka transport total, kitne pieces, aur per piece cost rate");
            Panel inputScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8)
            };
            TableLayoutPanel form = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 540,
                ColumnCount = 2,
                RowCount = 12,
                Padding = new Padding(4),
                BackColor = Color.White
            };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            for (int i = 0; i < 12; i++)
                form.RowStyles.Add(new RowStyle(SizeType.Absolute, i is 3 or 8 ? 28f : (i == 9 ? 78f : 36f)));

            txtTransport = NumberBox(allowDecimal: true, placeholder: "e.g. 100");
            txtQty = NumberBox(allowDecimal: false, placeholder: "e.g. 10");
            txtItemPrice = NumberBox(allowDecimal: true, placeholder: "e.g. 50");
            txtRent = NumberBox(allowDecimal: true, placeholder: "30000");
            txtSalary = NumberBox(allowDecimal: true, placeholder: "30000");
            txtExpectedSales = NumberBox(allowDecimal: false, placeholder: "3000");
            txtEnding = NumberBox(allowDecimal: false, placeholder: "9");

            suppressCalc = true;
            txtRent.Text = "30000";
            txtSalary.Text = "30000";
            txtExpectedSales.Text = "3000";
            txtEnding.Text = "9";
            suppressCalc = false;

            AddFormRow(form, 0, "Transport Cost (this item ₹)", txtTransport);
            AddFormRow(form, 1, "Quantity (this item)", txtQty);
            AddFormRow(form, 2, "Per Piece Rate (₹)", txtItemPrice);

            Label overheadTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "MONTHLY  ·  default Rent ₹30,000  ·  Salary ₹30,000  ·  Items sold 3,000",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Teal,
                TextAlign = ContentAlignment.BottomLeft
            };
            form.Controls.Add(overheadTitle, 0, 3);
            form.SetColumnSpan(overheadTitle, 2);

            AddFormRow(form, 4, "Monthly Rent (₹)", txtRent);
            AddFormRow(form, 5, "Monthly Salary (₹)", txtSalary);
            AddFormRow(form, 6, "Monthly items sold (pcs)", txtExpectedSales);
            AddFormRow(form, 7, "Price Ending Digit", txtEnding);

            Label hint = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Transport / piece = transport ÷ quantity. Per piece rate = cost price, divide nahi hota.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Muted,
                TextAlign = ContentAlignment.TopLeft
            };
            form.Controls.Add(hint, 0, 8);
            form.SetColumnSpan(hint, 2);

            lblSellingPrice = new Label
            {
                Dock = DockStyle.Fill,
                Text = "SELLING PRICE" + Environment.NewLine + "₹ —",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 53, 15),
                BackColor = Color.FromArgb(254, 243, 199),
                TextAlign = ContentAlignment.MiddleCenter
            };
            form.Controls.Add(lblSellingPrice, 0, 9);
            form.SetColumnSpan(lblSellingPrice, 2);

            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            Button btnCalc = ActionButton("Calculate", Teal, 110);
            Button btnCopy = ActionButton("Copy Selling Price", PrimaryBlue, 150);
            Button btnReset = ActionButton("Reset", Color.FromArgb(100, 116, 139), 110);
            btnCalc.Click += (_, _) => Recalculate(true);
            btnCopy.Click += (_, _) => CopyFinalPrice();
            btnReset.Click += (_, _) => ResetInputs();
            actions.Controls.Add(btnCalc);
            actions.Controls.Add(btnCopy);
            actions.Controls.Add(btnReset);
            form.Controls.Add(actions, 0, 10);
            form.SetColumnSpan(actions, 2);

            Label settingsHint = new Label
            {
                Dock = DockStyle.Fill,
                Name = "lblSettingsHint",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Muted,
                TextAlign = ContentAlignment.TopLeft
            };
            form.Controls.Add(settingsHint, 0, 11);
            form.SetColumnSpan(settingsHint, 2);

            inputScroll.Controls.Add(form);
            inputCard.Controls.Add(inputScroll);
            inputCard.Controls.Add(inputHeader);

            Panel resultCard = CreateCard();
            resultCard.Padding = new Padding(1);
            resultCard.Margin = new Padding(0);
            Panel resultHeader = CreateSectionHeader("FULL CALCULATION", "Per piece rate + profit + transport/piece + rent + salary  ·  round up to ending 9");

            TableLayoutPanel resultBody = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            resultBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 108f));
            resultBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            TableLayoutPanel summary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            lblFinalPrice = new Label { Text = "₹—" };
            lblActualProfit = new Label { Text = "₹0.00" };
            summary.Controls.Add(CreateStatCard("SELLING PRICE", lblFinalPrice, Color.FromArgb(254, 243, 199), Gold), 0, 0);
            summary.Controls.Add(CreateStatCard("ACTUAL PROFIT / PIECE", lblActualProfit, Color.FromArgb(186, 230, 253), Sky), 1, 0);

            dgvBreakdown = CreateBreakdownGrid();

            resultBody.Controls.Add(summary, 0, 0);
            resultBody.Controls.Add(dgvBreakdown, 0, 1);

            resultCard.Controls.Add(resultBody);
            resultCard.Controls.Add(resultHeader);

            split.Controls.Add(inputCard, 0, 0);
            split.Controls.Add(resultCard, 1, 0);

            return split;
        }

        private Control BuildSettingsTab()
        {
            Panel card = CreateCard();
            card.Padding = new Padding(1);
            card.Margin = new Padding(0);
            Panel header = CreateSectionHeader(
                "PRICING SETTINGS",
                canEditSettings ? "Master Admin — saved values apply to future calculations" : "Read-only — only Master Admin can change these");

            TableLayoutPanel body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.White
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 160f));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));

            TableLayoutPanel fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));

            nudRent = MoneyBox(30000m, 99999999.99m);
            nudSalary = MoneyBox(30000m, 99999999.99m);
            nudExpectedSales = MoneyBox(3000m, 99999999.99m);
            nudExpectedSales.DecimalPlaces = 0;
            nudEnding = IntBox(9, 9);
            nudEnding.Minimum = 0;
            nudEnding.Maximum = 9;

            AddLabeled(fields, 0, 0, "Monthly Rent (₹)", nudRent);
            AddLabeled(fields, 2, 0, "Monthly Salary (₹)", nudSalary);
            AddLabeled(fields, 0, 1, "Monthly items sold (pcs)", nudExpectedSales);
            AddLabeled(fields, 2, 1, "Price Ending Digit", nudEnding);

            Label slabHint = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Profit % applies only to purchase / piece. Min inclusive, Max exclusive (blank Max = no limit).",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            fields.Controls.Add(slabHint, 0, 2);
            fields.SetColumnSpan(slabHint, 4);

            dgvSlabs = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.Gainsboro,
                ColumnHeadersHeight = 32,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false
            };
            dgvSlabs.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
            dgvSlabs.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvSlabs.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvSlabs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "min",
                HeaderText = "Min purchase ₹ (inclusive)",
                FillWeight = 34
            });
            dgvSlabs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "max",
                HeaderText = "Max purchase ₹ (exclusive)",
                FillWeight = 34
            });
            dgvSlabs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "margin",
                HeaderText = "Profit margin %",
                FillWeight = 32
            });

            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight
            };
            Button btnSave = ActionButton("Save Settings", Teal, 140);
            Button btnReload = ActionButton("Reload", PrimaryBlue, 100);
            Button btnDefaults = ActionButton("Restore Defaults", Color.FromArgb(100, 116, 139), 150);
            btnSave.Click += (_, _) => SaveSettings();
            btnReload.Click += (_, _) => LoadSettingsFromDb();
            btnDefaults.Click += (_, _) => RestoreDefaultSettings();
            actions.Controls.Add(btnSave);
            actions.Controls.Add(btnReload);
            actions.Controls.Add(btnDefaults);

            bool enabled = canEditSettings;
            nudRent.Enabled = enabled;
            nudSalary.Enabled = enabled;
            nudExpectedSales.Enabled = enabled;
            nudEnding.Enabled = enabled;
            dgvSlabs.ReadOnly = !enabled;
            dgvSlabs.AllowUserToAddRows = enabled;
            dgvSlabs.AllowUserToDeleteRows = enabled;
            btnSave.Enabled = enabled;
            btnDefaults.Enabled = enabled;

            body.Controls.Add(fields, 0, 0);
            body.Controls.Add(dgvSlabs, 0, 1);
            body.Controls.Add(actions, 0, 2);

            card.Controls.Add(body);
            card.Controls.Add(header);
            return card;
        }

        private void LoadSettingsFromDb()
        {
            try
            {
                settings = PricingSettingsStore.Load();
                ApplySettingsToForm();
                Recalculate(false);
                lblStatus.Text = "Pricing settings loaded.";
            }
            catch (Exception ex)
            {
                settings = PricingSettings.CreateDefaults();
                ApplySettingsToForm();
                Recalculate(false);
                lblStatus.Text = "Using default settings (database not available).";
                MessageBox.Show("Could not load pricing settings.\n" + ex.Message, "Selling Price", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplySettingsToForm()
        {
            suppressCalc = true;
            try
            {
                SetNud(nudRent, settings.MonthlyRent);
                SetNud(nudSalary, settings.MonthlySalary);
                SetNud(nudExpectedSales, settings.ExpectedMonthlySales);
                SetNud(nudEnding, settings.PriceEndingDigit);
                SetText(txtRent, settings.MonthlyRent);
                SetText(txtSalary, settings.MonthlySalary);
                SetText(txtExpectedSales, settings.ExpectedMonthlySales, asInteger: true);
                SetText(txtEnding, settings.PriceEndingDigit, asInteger: true);
                BindSlabs(settings.Slabs);
                UpdateSettingsHint();
            }
            finally
            {
                suppressCalc = false;
            }
        }

        private void BindSlabs(List<ProfitMarginSlab> slabs)
        {
            dgvSlabs.Rows.Clear();
            foreach (ProfitMarginSlab slab in slabs)
            {
                dgvSlabs.Rows.Add(
                    slab.MinPurchaseCost.ToString("0.##", CultureInfo.InvariantCulture),
                    slab.MaxPurchaseCost.HasValue
                        ? slab.MaxPurchaseCost.Value.ToString("0.##", CultureInfo.InvariantCulture)
                        : "",
                    slab.MarginPercent.ToString("0.##", CultureInfo.InvariantCulture));
            }
        }

        private void UpdateSettingsHint()
        {
            Control? hint = Controls.Find("lblSettingsHint", true).Length > 0
                ? Controls.Find("lblSettingsHint", true)[0]
                : null;
            if (hint is not Label lbl)
                return;

            PricingSettings working = GetWorkingSettings();
            decimal rentEach = working.ExpectedMonthlySales > 0 ? working.MonthlyRent / working.ExpectedMonthlySales : 0;
            decimal salaryEach = working.ExpectedMonthlySales > 0 ? working.MonthlySalary / working.ExpectedMonthlySales : 0;
            decimal transport = ReadMoney(txtTransport, 0m);
            int qty = TryParseInt(txtQty.Text, out int parsedQty) && parsedQty > 0 ? parsedQty : 0;
            decimal transportEach = qty > 0 ? transport / qty : 0;
            lbl.Text = $"Per piece: transport ₹{transportEach:0.##}  ·  rent ₹{rentEach:0.##}  ·  salary ₹{salaryEach:0.##}  ·  ending {working.PriceEndingDigit}";
        }

        private void SaveSettings()
        {
            if (!canEditSettings)
                return;

            try
            {
                PricingSettings next = ReadSettingsFromForm();
                PricingSettingsStore.Save(next);
                settings = next;
                UpdateSettingsHint();
                Recalculate(true);
                lblStatus.Text = "Pricing settings saved. Future calculations will use these values.";
                MessageBox.Show("Pricing settings saved.", "Selling Price", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save settings.\n" + ex.Message, "Selling Price", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestoreDefaultSettings()
        {
            if (!canEditSettings)
                return;

            if (MessageBox.Show(
                    "Restore default rent, salary, sales, ending digit and profit slabs?",
                    "Restore Defaults",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            settings = PricingSettings.CreateDefaults();
            ApplySettingsToForm();
            try
            {
                PricingSettingsStore.Save(settings);
                lblStatus.Text = "Default pricing settings restored and saved.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Defaults shown, but save failed.";
                MessageBox.Show("Could not save default settings.\n" + ex.Message, "Selling Price", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Recalculate(false);
        }

        private PricingSettings ReadSettingsFromForm()
        {
            List<ProfitMarginSlab> slabs = new List<ProfitMarginSlab>();
            foreach (DataGridViewRow row in dgvSlabs.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string minText = row.Cells["min"].Value?.ToString() ?? "";
                string maxText = row.Cells["max"].Value?.ToString() ?? "";
                string marginText = row.Cells["margin"].Value?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(minText) && string.IsNullOrWhiteSpace(marginText))
                    continue;

                if (!decimal.TryParse(minText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal min)
                    && !decimal.TryParse(minText, NumberStyles.Number, CultureInfo.CurrentCulture, out min))
                    throw new InvalidOperationException("Each slab needs a numeric Min purchase amount.");

                decimal? max = null;
                if (!string.IsNullOrWhiteSpace(maxText))
                {
                    if (!decimal.TryParse(maxText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal maxVal)
                        && !decimal.TryParse(maxText, NumberStyles.Number, CultureInfo.CurrentCulture, out maxVal))
                        throw new InvalidOperationException("Max purchase must be numeric, or left blank.");
                    if (maxVal <= min)
                        throw new InvalidOperationException("Max purchase must be greater than Min.");
                    max = maxVal;
                }

                if (!decimal.TryParse(marginText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal margin)
                    && !decimal.TryParse(marginText, NumberStyles.Number, CultureInfo.CurrentCulture, out margin))
                    throw new InvalidOperationException("Each slab needs a numeric profit margin %.");
                if (margin < 0m)
                    throw new InvalidOperationException("Profit margin cannot be negative.");

                slabs.Add(new ProfitMarginSlab
                {
                    MinPurchaseCost = min,
                    MaxPurchaseCost = max,
                    MarginPercent = margin
                });
            }

            if (slabs.Count == 0)
                throw new InvalidOperationException("Add at least one profit-margin slab.");

            return new PricingSettings
            {
                MonthlyRent = ReadMoney(txtRent, settings.MonthlyRent),
                MonthlySalary = ReadMoney(txtSalary, settings.MonthlySalary),
                ExpectedMonthlySales = ReadMoney(txtExpectedSales, settings.ExpectedMonthlySales),
                DiscountPercent = 0m,
                PriceEndingDigit = ReadEndingDigit(),
                TotalTransportCost = 0m,
                TotalParcelQuantity = 1,
                Slabs = slabs
            };
        }

        private void Recalculate(bool showError)
        {
            if (suppressCalc)
                return;

            if (!TryGetItemInputs(out decimal perPieceRate, out int quantity, out decimal itemTransportCost))
            {
                if (showError)
                    MessageBox.Show("Enter this item's transport (0 allowed), quantity, and per piece rate.", "Selling Price", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                PricingSettings working = GetWorkingSettings();
                SellingPriceResult result = SellingPriceCalculations.Calculate(
                    perPieceRate,
                    quantity,
                    working,
                    itemTransportCost);

                string selling = "₹" + result.FinalSellingPrice.ToString("0");
                lblFinalPrice.Text = selling;
                if (lblSellingPrice != null)
                    lblSellingPrice.Text = "SELLING PRICE" + Environment.NewLine + selling;
                lblActualProfit.Text = "₹" + result.ActualProfit.ToString("0.00");
                RenderBreakdown(result);
                UpdateSettingsHint();
                if (showError)
                    lblStatus.Text = "Selling price: " + selling;
            }
            catch (Exception ex)
            {
                if (showError)
                    MessageBox.Show(ex.Message, "Selling Price", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool TryGetItemInputs(out decimal perPieceRate, out int quantity, out decimal itemTransportCost)
        {
            perPieceRate = 0;
            quantity = 0;
            itemTransportCost = 0;

            string transportText = (txtTransport.Text ?? "").Trim();
            if (string.IsNullOrEmpty(transportText))
                itemTransportCost = 0m;
            else if (!TryParseDecimal(transportText, out itemTransportCost) || itemTransportCost < 0m)
                return false;

            if (!TryParseInt(txtQty.Text, out quantity) || quantity <= 0)
                return false;
            if (!TryParseDecimal(txtItemPrice.Text, out perPieceRate) || perPieceRate < 0m)
                return false;

            return true;
        }

        private PricingSettings GetWorkingSettings()
        {
            return new PricingSettings
            {
                MonthlyRent = ReadMoney(txtRent, settings.MonthlyRent),
                MonthlySalary = ReadMoney(txtSalary, settings.MonthlySalary),
                ExpectedMonthlySales = ReadMoney(txtExpectedSales, settings.ExpectedMonthlySales),
                DiscountPercent = 0m,
                PriceEndingDigit = ReadEndingDigit(),
                TotalTransportCost = 0m,
                TotalParcelQuantity = 1,
                Slabs = settings.Slabs
            };
        }

        private int ReadEndingDigit()
        {
            if (TryParseInt(txtEnding.Text, out int value) && value >= 0 && value <= 9)
                return value;
            return settings.PriceEndingDigit;
        }

        private decimal ReadMoney(TextBox box, decimal fallback)
        {
            return TryParseDecimal(box.Text, out decimal value) && value >= 0m ? value : fallback;
        }

        private static void SetText(TextBox box, decimal value, bool asInteger = false)
        {
            box.Text = asInteger || value == decimal.Truncate(value)
                ? decimal.Truncate(value).ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static bool TryParseDecimal(string? text, out decimal value)
        {
            text = (text ?? "").Trim().Replace(",", "");
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
        }

        private static bool TryParseInt(string? text, out int value)
        {
            text = (text ?? "").Trim().Replace(",", "");
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                || int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
        }

        private void RenderBreakdown(SellingPriceResult result)
        {
            dgvBreakdown.Rows.Clear();

            AddStep("1. Transport cost (this item)", "", "₹" + result.TotalTransportCost.ToString("0.00"));
            AddStep("2. Quantity", "", result.Quantity.ToString(CultureInfo.InvariantCulture));
            AddStep(
                "3. Transport / piece",
                "₹" + result.TotalTransportCost.ToString("0.00") + " ÷ " + result.Quantity.ToString(CultureInfo.InvariantCulture),
                "₹" + result.TransportPerPiece.ToString("0.00"));
            AddStep(
                "4. Per piece rate (cost)",
                "",
                "₹" + result.PurchaseCostPerPiece.ToString("0.00"));
            AddStep(
                "5. Profit margin (auto)",
                "only on cost ₹" + result.PurchaseCostPerPiece.ToString("0.00"),
                result.ProfitMarginPercent.ToString("0.##") + "%");
            AddStep(
                "6. Profit / piece",
                "₹" + result.PurchaseCostPerPiece.ToString("0.00") + " × " + result.ProfitMarginPercent.ToString("0.##") + "%",
                "₹" + result.ProfitPerPiece.ToString("0.00"));
            AddStep(
                "7. Rent / piece",
                "₹" + result.MonthlyRent.ToString("0.00") + " ÷ " + result.ExpectedMonthlySales.ToString("0.##"),
                "₹" + result.RentPerPiece.ToString("0.00"));
            AddStep(
                "8. Salary / piece",
                "₹" + result.MonthlySalary.ToString("0.00") + " ÷ " + result.ExpectedMonthlySales.ToString("0.##"),
                "₹" + result.SalaryPerPiece.ToString("0.00"));
            AddStep(
                "9. Required net price",
                "₹" + result.PurchaseCostPerPiece.ToString("0.00")
                    + " + ₹" + result.ProfitPerPiece.ToString("0.00")
                    + " + ₹" + result.TransportPerPiece.ToString("0.00")
                    + " + ₹" + result.RentPerPiece.ToString("0.00")
                    + " + ₹" + result.SalaryPerPiece.ToString("0.00"),
                "₹" + result.RequiredNetPrice.ToString("0.00"),
                emphasize: true);
            AddStep(
                "10. Selling price (final)",
                "round up to ending " + result.PriceEndingDigit.ToString(CultureInfo.InvariantCulture),
                "₹" + result.FinalSellingPrice.ToString("0"),
                emphasize: true);
            AddStep(
                "11. Actual cost (no profit)",
                "₹" + result.PurchaseCostPerPiece.ToString("0.00")
                    + " + ₹" + result.TransportPerPiece.ToString("0.00")
                    + " + ₹" + result.RentPerPiece.ToString("0.00")
                    + " + ₹" + result.SalaryPerPiece.ToString("0.00"),
                "₹" + result.ActualTotalCost.ToString("0.00"));
            AddStep(
                "12. Actual profit / piece",
                "₹" + result.FinalSellingPrice.ToString("0") + " − ₹" + result.ActualTotalCost.ToString("0.00"),
                "₹" + result.ActualProfit.ToString("0.00"),
                emphasize: true);
        }

        private void AddStep(string step, string formula, string amount, bool emphasize = false)
        {
            int index = dgvBreakdown.Rows.Add(step, formula, amount);
            DataGridViewRow row = dgvBreakdown.Rows[index];
            if (!emphasize)
                return;

            Font bold = new Font("Segoe UI", 9f, FontStyle.Bold);
            row.DefaultCellStyle.Font = bold;
            row.DefaultCellStyle.ForeColor = Slate;
            row.DefaultCellStyle.BackColor = Color.FromArgb(204, 251, 241);
            row.Cells["Amount"].Style.ForeColor = Teal;
        }

        private static DataGridView CreateBreakdownGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.Gainsboro,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 32,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                ScrollBars = ScrollBars.Vertical
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            grid.DefaultCellStyle.ForeColor = Slate;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            grid.DefaultCellStyle.SelectionForeColor = Slate;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
            grid.RowTemplate.Height = 30;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Step",
                HeaderText = "Step",
                FillWeight = 32
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Formula",
                HeaderText = "Working",
                FillWeight = 46
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Amount",
                HeaderText = "Amount",
                FillWeight = 22,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            });

            return grid;
        }

        private void CopyFinalPrice()
        {
            Recalculate(true);
            string text = lblFinalPrice.Text.Replace("₹", "").Trim();
            if (string.IsNullOrWhiteSpace(text) || text == "—")
                return;
            Clipboard.SetText(text);
            lblStatus.Text = $"Copied marked price {lblFinalPrice.Text} to clipboard.";
        }

        private void ResetInputs()
        {
            suppressCalc = true;
            txtTransport.Text = "";
            txtQty.Text = "";
            txtItemPrice.Text = "";
            suppressCalc = false;

            if (lblSellingPrice != null)
                lblSellingPrice.Text = "SELLING PRICE" + Environment.NewLine + "₹ —";
            lblFinalPrice.Text = "₹—";
            lblActualProfit.Text = "₹0.00";
            dgvBreakdown.Rows.Clear();
            lblStatus.Text = "Reset. Is item ka transport, quantity aur per piece rate dubara daalo.";
        }

        private static void AddFormRow(TableLayoutPanel form, int row, string label, Control field)
        {
            form.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Slate
            }, 0, row);
            form.Controls.Add(field, 1, row);
        }

        private static void AddLabeled(TableLayoutPanel grid, int col, int row, string text, Control field)
        {
            grid.Controls.Add(new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Slate
            }, col, row);
            grid.Controls.Add(field, col + 1, row);
        }

        private TextBox NumberBox(bool allowDecimal, string placeholder)
        {
            TextBox box = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                PlaceholderText = placeholder,
                MaxLength = allowDecimal ? 14 : 9
            };
            box.KeyPress += (_, e) => NumberKeyPress(e, box, allowDecimal);
            box.KeyDown += NumberKeyDown;
            box.TextChanged += (_, _) => SanitizeAndCalc(box, allowDecimal);
            return box;
        }

        private static void NumberKeyPress(KeyPressEventArgs e, TextBox box, bool allowDecimal)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar))
                return;

            if (allowDecimal && (e.KeyChar == '.' || e.KeyChar == ',') && !box.Text.Contains('.') && !box.Text.Contains(','))
                return;

            e.Handled = true;
        }

        private static void NumberKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode is Keys.A or Keys.C or Keys.X or Keys.V or Keys.Z)
                return;
            if (e.KeyCode is Keys.Back or Keys.Delete or Keys.Left or Keys.Right or Keys.Home or Keys.End or Keys.Tab)
                return;
        }

        private void SanitizeAndCalc(TextBox box, bool allowDecimal)
        {
            if (suppressCalc)
                return;

            string raw = box.Text ?? "";
            string cleaned = allowDecimal ? KeepMoneyDigits(raw) : KeepDigitsOnly(raw);
            if (cleaned != raw)
            {
                int caret = box.SelectionStart;
                int delta = raw.Length - cleaned.Length;
                suppressCalc = true;
                box.Text = cleaned;
                box.SelectionStart = Math.Max(0, Math.Min(cleaned.Length, caret - delta));
                suppressCalc = false;
            }

            Recalculate(false);
        }

        private static string KeepDigitsOnly(string text)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsDigit(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static string KeepMoneyDigits(string text)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(text.Length);
            bool seenDot = false;
            foreach (char c in text)
            {
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                    continue;
                }

                if ((c == '.' || c == ',') && !seenDot)
                {
                    sb.Append('.');
                    seenDot = true;
                }
            }
            return sb.ToString();
        }

        private static NumericUpDown MoneyBox(decimal value, decimal max)
        {
            return new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 2,
                ThousandsSeparator = true,
                Minimum = 0,
                Maximum = max,
                Value = Math.Min(Math.Max(value, 0), max),
                Font = new Font("Segoe UI", 10f)
            };
        }

        private static NumericUpDown IntBox(decimal value, decimal max)
        {
            return new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 0,
                ThousandsSeparator = true,
                Minimum = 1,
                Maximum = max,
                Value = Math.Min(Math.Max(value, 1), max),
                Font = new Font("Segoe UI", 10f)
            };
        }

        private static void SetNud(NumericUpDown box, decimal value)
        {
            decimal clamped = Math.Min(Math.Max(value, box.Minimum), box.Maximum);
            box.Value = clamped;
        }

        private static Button ActionButton(string text, Color bg, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Width = width,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 0),
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private static Panel CreateCard()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
        }

        private static Panel CreateSectionHeader(string title, string hint)
        {
            Panel bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = HeaderBg,
                Padding = new Padding(12, 0, 12, 0)
            };
            Label lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 220,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblHint = new Label
            {
                Text = hint,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleRight
            };
            bar.Controls.Add(lblHint);
            bar.Controls.Add(lblTitle);
            return bar;
        }

        private static Panel CreateStatCard(string caption, Label valueLabel, Color bg, Color accent)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bg,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12, 8, 12, 8)
            };
            card.Paint += (_, e) =>
            {
                if (card.Height <= 0)
                    return;
                using SolidBrush bar = new SolidBrush(accent);
                e.Graphics.FillRectangle(bar, 0, 0, 4, card.Height);
            };

            Label lblCap = new Label
            {
                Text = caption,
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Muted,
                BackColor = Color.Transparent
            };

            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            valueLabel.ForeColor = Slate;
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.BackColor = Color.Transparent;

            card.Controls.Add(valueLabel);
            card.Controls.Add(lblCap);
            return card;
        }
    }
}
