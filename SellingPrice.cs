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

        private readonly bool canEditSettings;
        private PricingSettings settings = PricingSettings.CreateDefaults();
        private bool suppressCalc;

        private NumericUpDown nudPurchase;
        private NumericUpDown nudQty;
        private NumericUpDown nudTransport;
        private NumericUpDown nudParcelQty;

        private Label lblFinalPrice;
        private Label lblCustomerPays;
        private Label lblActualProfit;
        private Label lblStatus;
        private TableLayoutPanel breakdownGrid;

        private NumericUpDown nudRent;
        private NumericUpDown nudSalary;
        private NumericUpDown nudExpectedSales;
        private NumericUpDown nudDiscount;
        private NumericUpDown nudEnding;
        private DataGridView dgvSlabs;

        public SellingPrice(string role = "")
        {
            canEditSettings = string.Equals((role ?? "").Trim(), "Master Admin", StringComparison.OrdinalIgnoreCase);
            InitializeUI();
            Load += (_, _) => LoadSettingsFromDb();
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
                TextRenderer.DrawText(e.Graphics, "Purchase + profit + overheads  ·  discount absorbed into marked price  ·  round up to ending digit", hintFont,
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
            Panel inputHeader = CreateSectionHeader("INPUTS", "Invoice / parcel figures");
            TableLayoutPanel form = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.White
            };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            for (int i = 0; i < 8; i++)
                form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));

            nudPurchase = MoneyBox(0m, 99999999.99m);
            nudQty = IntBox(1, 1000000);
            nudTransport = MoneyBox(0m, 99999999.99m);
            nudParcelQty = IntBox(1, 1000000);

            AddFormRow(form, 0, "Total Purchase Cost (₹)", nudPurchase);
            AddFormRow(form, 1, "Item Quantity", nudQty);
            AddFormRow(form, 2, "Total Transport Cost (₹)", nudTransport);
            AddFormRow(form, 3, "Total Parcel Quantity", nudParcelQty);

            Label hint = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Purchase is divided by item qty. Transport is divided by whole-parcel qty — not by this item's cost.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Muted,
                TextAlign = ContentAlignment.TopLeft
            };
            form.Controls.Add(hint, 0, 4);
            form.SetColumnSpan(hint, 2);

            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            Button btnCalc = ActionButton("Calculate", Teal, 110);
            Button btnCopy = ActionButton("Copy Price", PrimaryBlue, 120);
            Button btnReset = ActionButton("Reset", Color.FromArgb(100, 116, 139), 90);
            btnCalc.Click += (_, _) => Recalculate(true);
            btnCopy.Click += (_, _) => CopyFinalPrice();
            btnReset.Click += (_, _) => ResetInputs();
            actions.Controls.Add(btnCalc);
            actions.Controls.Add(btnCopy);
            actions.Controls.Add(btnReset);
            form.Controls.Add(actions, 0, 5);
            form.SetColumnSpan(actions, 2);

            Label settingsHint = new Label
            {
                Dock = DockStyle.Fill,
                Name = "lblSettingsHint",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Muted,
                TextAlign = ContentAlignment.TopLeft
            };
            form.Controls.Add(settingsHint, 0, 6);
            form.SetColumnSpan(settingsHint, 2);

            inputCard.Controls.Add(form);
            inputCard.Controls.Add(inputHeader);

            Panel resultCard = CreateCard();
            resultCard.Padding = new Padding(1);
            resultCard.Margin = new Padding(0);
            Panel resultHeader = CreateSectionHeader("RESULT", "Marked price after discount absorption + ₹9 ending");

            TableLayoutPanel resultBody = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            resultBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 92f));
            resultBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            TableLayoutPanel summary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));

            lblFinalPrice = new Label { Text = "₹0" };
            lblCustomerPays = new Label { Text = "₹0.00" };
            lblActualProfit = new Label { Text = "₹0.00" };
            summary.Controls.Add(CreateStatCard("MARKED PRICE", lblFinalPrice, Color.FromArgb(254, 243, 199), Gold), 0, 0);
            summary.Controls.Add(CreateStatCard("CUSTOMER PAYS (after discount)", lblCustomerPays, Color.FromArgb(167, 243, 208), Teal), 1, 0);
            summary.Controls.Add(CreateStatCard("ACTUAL PROFIT / PIECE", lblActualProfit, Color.FromArgb(186, 230, 253), Sky), 2, 0);

            breakdownGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoScroll = true,
                Padding = new Padding(8, 8, 8, 8),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            breakdownGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            breakdownGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            resultBody.Controls.Add(summary, 0, 0);
            resultBody.Controls.Add(breakdownGrid, 0, 1);

            resultCard.Controls.Add(resultBody);
            resultCard.Controls.Add(resultHeader);

            split.Controls.Add(inputCard, 0, 0);
            split.Controls.Add(resultCard, 1, 0);

            nudPurchase.ValueChanged += (_, _) => Recalculate(false);
            nudQty.ValueChanged += (_, _) => Recalculate(false);
            nudTransport.ValueChanged += (_, _) => Recalculate(false);
            nudParcelQty.ValueChanged += (_, _) => Recalculate(false);

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
            nudDiscount = MoneyBox(15m, 99.99m);
            nudDiscount.Maximum = 99.99m;
            nudEnding = IntBox(9, 9);
            nudEnding.Minimum = 0;
            nudEnding.Maximum = 9;

            AddLabeled(fields, 0, 0, "Monthly Rent (₹)", nudRent);
            AddLabeled(fields, 2, 0, "Monthly Salary (₹)", nudSalary);
            AddLabeled(fields, 0, 1, "Expected Monthly Sales (pcs)", nudExpectedSales);
            AddLabeled(fields, 2, 1, "Customer Discount %", nudDiscount);
            AddLabeled(fields, 0, 2, "Price Ending Digit", nudEnding);

            Label slabHint = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Profit % applies only to purchase cost / piece. Min is inclusive, Max is exclusive (leave Max blank for no upper limit).",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            fields.Controls.Add(slabHint, 2, 2);
            fields.SetColumnSpan(slabHint, 2);

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
            nudDiscount.Enabled = enabled;
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
                SetNud(nudDiscount, settings.DiscountPercent);
                SetNud(nudEnding, settings.PriceEndingDigit);
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
            if (hint is Label lbl)
            {
                decimal rentEach = settings.ExpectedMonthlySales > 0 ? settings.MonthlyRent / settings.ExpectedMonthlySales : 0;
                decimal salaryEach = settings.ExpectedMonthlySales > 0 ? settings.MonthlySalary / settings.ExpectedMonthlySales : 0;
                lbl.Text = $"Current settings: rent ₹{rentEach:0.##}/pc  ·  salary ₹{salaryEach:0.##}/pc  ·  discount {settings.DiscountPercent:0.##}%  ·  ending {settings.PriceEndingDigit}";
            }
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
                    "Restore default rent, salary, sales, discount, ending digit and profit slabs?",
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
                MonthlyRent = nudRent.Value,
                MonthlySalary = nudSalary.Value,
                ExpectedMonthlySales = nudExpectedSales.Value,
                DiscountPercent = nudDiscount.Value,
                PriceEndingDigit = (int)nudEnding.Value,
                Slabs = slabs
            };
        }

        private void Recalculate(bool showError)
        {
            if (suppressCalc)
                return;

            try
            {
                SellingPriceResult result = SellingPriceCalculations.Calculate(
                    nudPurchase.Value,
                    (int)nudQty.Value,
                    nudTransport.Value,
                    (int)nudParcelQty.Value,
                    settings);

                lblFinalPrice.Text = "₹" + result.FinalSellingPrice.ToString("0");
                lblCustomerPays.Text = "₹" + result.CustomerPayable.ToString("0.00");
                lblActualProfit.Text = "₹" + result.ActualProfit.ToString("0.00");
                RenderBreakdown(result);
                lblStatus.Text = showError ? "Calculated." : lblStatus.Text;
            }
            catch (Exception ex)
            {
                lblFinalPrice.Text = "₹—";
                lblCustomerPays.Text = "₹—";
                lblActualProfit.Text = "₹—";
                breakdownGrid.Controls.Clear();
                if (showError)
                    MessageBox.Show(ex.Message, "Selling Price", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RenderBreakdown(SellingPriceResult result)
        {
            breakdownGrid.Controls.Clear();
            breakdownGrid.RowStyles.Clear();
            breakdownGrid.RowCount = 0;

            AddBreak("Purchase cost / piece", "₹" + result.PurchaseCostPerPiece.ToString("0.00"));
            AddBreak($"Profit ({result.ProfitMarginPercent:0.##}% of purchase only)", "₹" + result.ProfitPerPiece.ToString("0.00"));
            AddBreak("Transport / piece (parcel qty)", "₹" + result.TransportPerPiece.ToString("0.00"));
            AddBreak("Rent / piece", "₹" + result.RentPerPiece.ToString("0.00"));
            AddBreak("Salary / piece", "₹" + result.SalaryPerPiece.ToString("0.00"));
            AddBreak("Required net price", "₹" + result.RequiredNetPrice.ToString("0.00"), true);
            AddBreak($"÷ {(result.DiscountKeepRatio):0.00}  (absorb {settings.DiscountPercent:0.##}% discount)", "₹" + result.PriceBeforeRounding.ToString("0.00"));
            AddBreak($"Round up to ending {settings.PriceEndingDigit}", "₹" + result.FinalSellingPrice.ToString("0"), true);
            AddBreak($"{settings.DiscountPercent:0.##}% discount amount", "₹" + result.DiscountAmount.ToString("0.00"));
            AddBreak("Customer payable", "₹" + result.CustomerPayable.ToString("0.00"));
            AddBreak("Actual cost (no profit)", "₹" + result.ActualTotalCost.ToString("0.00"));
            AddBreak("Actual profit / piece", "₹" + result.ActualProfit.ToString("0.00"), true);
        }

        private void AddBreak(string label, string value, bool emphasize = false)
        {
            int row = breakdownGrid.RowCount++;
            breakdownGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            Font font = new Font("Segoe UI", 9f, emphasize ? FontStyle.Bold : FontStyle.Regular);
            Color color = emphasize ? Slate : Color.FromArgb(51, 65, 85);
            breakdownGrid.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                Font = font,
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, row);
            breakdownGrid.Controls.Add(new Label
            {
                Text = value,
                Dock = DockStyle.Fill,
                Font = font,
                ForeColor = emphasize ? Teal : color,
                TextAlign = ContentAlignment.MiddleRight
            }, 1, row);
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
            nudPurchase.Value = 0;
            nudQty.Value = 1;
            nudTransport.Value = 0;
            nudParcelQty.Value = 1;
            suppressCalc = false;
            Recalculate(false);
            lblStatus.Text = "Inputs cleared.";
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
                Width = 170,
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
            valueLabel.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            valueLabel.ForeColor = Slate;
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.BackColor = Color.Transparent;

            card.Controls.Add(valueLabel);
            card.Controls.Add(lblCap);
            return card;
        }
    }
}
