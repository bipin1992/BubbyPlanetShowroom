using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Printing;
using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class AddItem : UserControl
    {
        TextBox txtCode = new TextBox();
        TextBox txtName = new TextBox();
        TextBox txtBrand = new TextBox();
        TextBox txtSupplier = new TextBox();
        //TextBox txtColor = new TextBox();

        TextBox txtCost = new TextBox();
        TextBox txtBeforeTax = new TextBox();
        TextBox txtGST = new TextBox();
        TextBox txtSell = new TextBox();
        TextBox txtCalcFirst = new TextBox();
        TextBox txtCalcSecond = new TextBox();
        ComboBox cmbCalcOperator = new ComboBox();
        TextBox txtCalcResult = new TextBox();

        ComboBox cmbMain = new ComboBox();
        ComboBox cmbSub = new ComboBox();
        ComboBox cmbGender = new ComboBox();
        ComboBox cmbItemType = new ComboBox();
        ComboBox cmbActual = new ComboBox();
        ComboBox cmbSize = new ComboBox();
        Button btnPrint = new Button();
        Button btnInStock = new Button();
        bool printDoneForCurrentItem = false;
        bool stockInDoneForCurrentItem = false;

        static string lastBrand = "";
        static string lastSupplier = "";
        string lastSavedItemCode = "";
        string lastSavedItemName = "";
        string lastSavedSize = "";
        string lastSavedPrice = "0";

        PrintDocument printDoc = new PrintDocument();
        Bitmap barcodeImage;
        Label lblLastAdded = new Label();

        public AddItem()
        {
            Dock = DockStyle.Fill;

            // Layout
            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.IsSplitterFixed = false;
            split.Panel1MinSize = 0;
            split.Panel2MinSize = 0;
            split.BackColor = Color.White;
            split.FixedPanel = FixedPanel.None;
            split.SizeChanged += (s, e) =>
            {
                ApplySafeSplitter(split);
            };

            split.Panel1.Controls.Add(CreateLeftUI());
            split.Panel2.Controls.Add(CreateRightUI());

            Controls.Add(split);
            split.HandleCreated += (s, e) => ApplySafeSplitter(split);

            // Load main categories
            LoadMainCategories();

            // Price calculation
            txtBeforeTax.TextChanged += CalcPrice;
            txtGST.TextChanged += CalcPrice;

            // Main category change
            cmbMain.SelectedIndexChanged += (s, e) =>
            {
                HandleMainCategoryUI();

                ClearAfterMainCategory();
                LoadSubCategory();
                ClearGeneratedItemIdentity();
                ClearPriceFields();
            };

            // Sub category change
            cmbSub.SelectedIndexChanged += (s, e) =>
            {
                ClearAfterSubCategory();

                if (IsToys())
                    LoadItemType();
                else
                    LoadGenderOptions();

                ClearGeneratedItemIdentity();
                ClearPriceFields();
            };

            // Item type change
            cmbItemType.SelectedIndexChanged += (s, e) =>
            {
                ClearAfterItemType();
                LoadActualItems();
                ClearGeneratedItemIdentity();
                ClearPriceFields();
            };

            cmbGender.SelectedIndexChanged += (s, e) =>
            {
                ClearAfterGender();
                LoadItemType();
                ClearGeneratedItemIdentity();
                ClearPriceFields();
            };

            cmbActual.SelectedIndexChanged += (s, e) =>
            {
                ClearAfterActualItem();
                LoadSizes();
                ClearGeneratedItemIdentity();
                ClearPriceFields();
            };

            cmbMain.SelectedIndexChanged += (s, e) => GenerateItemDetails();
            cmbSub.SelectedIndexChanged += (s, e) => GenerateItemDetails();
            cmbGender.SelectedIndexChanged += (s, e) => GenerateItemDetails();
            cmbActual.SelectedIndexChanged += (s, e) => GenerateItemDetails();
            cmbSize.SelectedIndexChanged += (s, e) => GenerateItemDetails();

            txtBrand.TextChanged += (s, e) => GenerateItemDetails();
            txtSupplier.TextChanged += (s, e) => GenerateItemDetails();
            //txtColor.TextChanged += (s, e) => GenerateItemDetails();
            cmbMain.TextChanged += (s, e) => GenerateItemDetails();
            cmbSub.TextChanged += (s, e) => GenerateItemDetails();
            cmbGender.TextChanged += (s, e) => GenerateItemDetails();
            cmbActual.TextChanged += (s, e) => GenerateItemDetails();
            cmbSize.TextChanged += (s, e) => GenerateItemDetails();
            txtSell.TextChanged += (s, e) =>
            {
                if (IsToys() || IsFootwears())
                    GenerateItemDetails();
            };

            cmbGender.Items.Clear();
            cmbGender.Items.Add("Boys");
            cmbGender.Items.Add("Girls");
            cmbGender.Items.Add("Unisex");
            cmbGender.Items.Add("NA");
            cmbGender.SelectedIndex = 0;

            //Label lblCenter = new Label();

            //lblCenter.Text =
            //"FINAL READY REFERENCE\n\n" +
            //"5% GST  → 20 ka multiple\n" +
            //"20, 40, 60, 80, 100...\n\n" +
            //"12% GST → 25 ka multiple\n" +
            //"25, 50, 75, 100, 125...\n\n" +
            //"18% GST → 50 ka multiple\n" +
            //"50, 100, 150, 200...\n\n" +
            //"Shortcut:\n" +
            //"5% → 20   |   12% → 25   |   18% → 50";

            //// 🔥 width chota (left-right margin milega)
            //lblCenter.Size = new Size(this.Width - 500, 180);

            //lblCenter.BackColor = Color.DarkRed;
            //lblCenter.ForeColor = Color.White;

            //// 🔥 multi-line center alignment
            //lblCenter.TextAlign = ContentAlignment.MiddleCenter;
            //lblCenter.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            //// 🔥 position
            //lblCenter.Left = (this.Width - lblCenter.Width) / 2;
            //lblCenter.Top = 20;

            //// 🔥 resize handling
            //this.Resize += (s, e) =>
            //{
            //    lblCenter.Width = this.Width - 500; // maintain margin
            //    lblCenter.Left = (this.Width - lblCenter.Width) / 2;
            //};

            //Controls.Add(lblCenter);
            //lblCenter.BringToFront();

            txtBrand.Text = lastBrand;
            txtSupplier.Text = lastSupplier;
            txtGST.Text = "0";
            printDoc.PrintPage += PrintDoc_PrintPage;
        }

        void ApplySafeSplitter(SplitContainer split)
        {
            try
            {
                if (split.Width <= 0)
                    return;

                int target = (int)(split.Width * 0.62);
                if (target < 220) target = 220;
                if (target > split.Width - 220) target = split.Width - 220;

                if (target > 0 && target < split.Width)
                    split.SplitterDistance = target;
            }
            catch { }
        }

        void ClearAfterMainCategory()
        {
            cmbSub.Items.Clear();
            cmbSub.Text = "";
            cmbSub.SelectedIndex = -1;

            cmbGender.Text = "";
            cmbGender.SelectedIndex = -1;

            ClearAfterSubCategory();
        }

        void ClearAfterSubCategory()
        {
            cmbItemType.Items.Clear();
            cmbItemType.Text = "";
            cmbItemType.SelectedIndex = -1;

            ClearAfterItemType();
        }

        void ClearAfterGender()
        {
            cmbItemType.Items.Clear();
            cmbItemType.Text = "";
            cmbItemType.SelectedIndex = -1;

            cmbActual.Items.Clear();
            cmbActual.Text = "";
            cmbActual.SelectedIndex = -1;

            ClearAfterActualItem();
        }

        void ClearAfterItemType()
        {
            cmbActual.Items.Clear();
            cmbActual.Text = "";
            cmbActual.SelectedIndex = -1;

            ClearAfterActualItem();
        }

        void ClearAfterActualItem()
        {
            cmbSize.Items.Clear();
            cmbSize.Text = "";
            cmbSize.SelectedIndex = -1;
        }

        void ClearGeneratedItemIdentity()
        {
            txtCode.Clear();
            txtName.Clear();
        }

        void ClearPriceFields()
        {
            txtCost.Clear();
            txtBeforeTax.Clear();
            txtGST.Text = "0";
            txtSell.Clear();
        }

        string SafeCode(string text, int len)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "NA";

            return text.Length >= len
                ? text.Substring(0, len).ToUpper()
                : text.ToUpper();
        }

        bool IsMainCategory(string category)
        {
            return string.Equals(cmbMain.Text.Trim(), category, StringComparison.OrdinalIgnoreCase);
        }

        bool IsToys() => IsMainCategory("Toys");
        bool IsClothes() => IsMainCategory("Clothes");
        bool IsFootwears() => IsMainCategory("Footwears");

        string SqlText(string text)
        {
            return text.Trim().Replace("'", "''");
        }

        string CiEquals(string column, string value)
        {
            return $"LOWER({column}) = LOWER('{SqlText(value)}')";
        }

        void HandleMainCategoryUI()
        {
            if (IsToys())
            {
                cmbSub.Enabled = true;
                cmbItemType.Enabled = true;
                cmbActual.Enabled = true;
                cmbSize.Enabled = false;
                cmbGender.Enabled = false;
                //txtColor.Enabled = false;
            }
            //else if (cmbMain.Text == "Footwears")
            //{
            //    cmbSub.Enabled = true;
            //    cmbItemType.Enabled = false;
            //    cmbSize.Enabled = true;
            //}
            else // Clothes and footwears
            {
                cmbSub.Enabled = true;
                cmbItemType.Enabled = true;
                cmbActual.Enabled = true;
                cmbSize.Enabled = true;
                cmbGender.Enabled = true;
                //txtColor.Enabled = true;
            }



        }

        Control CreateLeftUI()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
            panel.Padding = new Padding(12);
            panel.BackColor = Color.White;

            Label title = new Label();
            title.Text = "Add Item";
            title.Dock = DockStyle.Top;
            title.Height = 36;
            title.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(30, 41, 59);

            TableLayoutPanel itemGrid = CreateFormGrid();
            AddRow(itemGrid, "Item Code", txtCode);
            AddRow(itemGrid, "Item Name", txtName);
            AddRow(itemGrid, "Brand", txtBrand);
            AddRow(itemGrid, "Supplier", txtSupplier);

            TableLayoutPanel categoryGrid = CreateFormGrid();
            AddRow(categoryGrid, "Main Category", cmbMain);
            AddRow(categoryGrid, "Sub Category", cmbSub);
            AddRow(categoryGrid, "Gender", cmbGender);
            AddRow(categoryGrid, "Item Type", cmbItemType);
            AddRow(categoryGrid, "Actual Item", cmbActual);
            AddRow(categoryGrid, "Size", cmbSize);
            //AddRow(grid, "Color", txtColor);

            TableLayoutPanel priceGrid = CreateFormGrid();
            AddRow(priceGrid, "Cost Price", txtCost);
            AddRow(priceGrid, "Price Before Tax", txtBeforeTax);
            AddRow(priceGrid, "GST %", txtGST);
            AddRow(priceGrid, "Selling Price", txtSell);

            GroupBox calculatorBox = CreateCalculatorSection();

            Button btnSave = new Button();
            btnSave.Text = "Save Item";
            btnSave.Width = 130;
            btnSave.Height = 36;
            btnSave.BackColor = Color.FromArgb(34, 197, 94);
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += SaveItem;

            Button btnClear = new Button();
            btnClear.Text = "Reset";
            btnClear.Width = 100;
            btnClear.Height = 36;
            btnClear.BackColor = Color.FromArgb(100, 116, 139);
            btnClear.ForeColor = Color.White;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => ClearFields();

            btnPrint.Text = "Print Label";
            btnPrint.Width = 110;
            btnPrint.Height = 36;
            btnPrint.BackColor = Color.FromArgb(37, 99, 235);
            btnPrint.ForeColor = Color.White;
            btnPrint.FlatStyle = FlatStyle.Flat;
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Enabled = false;
            btnPrint.Click += (s, e) => PrintLabelFromAddItem();

            btnInStock.Text = "IN Stock";
            btnInStock.Width = 100;
            btnInStock.Height = 36;
            btnInStock.BackColor = Color.FromArgb(217, 119, 6);
            btnInStock.ForeColor = Color.White;
            btnInStock.FlatStyle = FlatStyle.Flat;
            btnInStock.FlatAppearance.BorderSize = 0;
            btnInStock.Enabled = false;
            btnInStock.Click += (s, e) => StockInFromAddItem();

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.AutoSize = true;
            actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            actions.WrapContents = true;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.Padding = new Padding(0, 8, 0, 0);
            actions.Controls.Add(btnSave);
            actions.Controls.Add(btnClear);
            actions.Controls.Add(btnPrint);
            actions.Controls.Add(btnInStock);

            GroupBox lastAddedBox = new GroupBox();
            lastAddedBox.Text = "Last Added Item";
            lastAddedBox.Dock = DockStyle.Top;
            lastAddedBox.Height = 95;
            lastAddedBox.Padding = new Padding(8);
            lastAddedBox.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            lblLastAdded.Dock = DockStyle.Fill;
            lblLastAdded.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblLastAdded.ForeColor = Color.FromArgb(30, 41, 59);
            lblLastAdded.Text = "No item saved yet.";
            lblLastAdded.AutoSize = false;

            lastAddedBox.Controls.Add(lblLastAdded);

            cmbMain.DropDownStyle = ComboBoxStyle.DropDown;
            cmbSub.DropDownStyle = ComboBoxStyle.DropDown;
            cmbGender.DropDownStyle = ComboBoxStyle.DropDown;
            cmbItemType.DropDownStyle = ComboBoxStyle.DropDown;
            cmbActual.DropDownStyle = ComboBoxStyle.DropDown;
            cmbSize.DropDownStyle = ComboBoxStyle.DropDown;

            panel.Controls.Add(actions);
            panel.Controls.Add(lastAddedBox);
            panel.Controls.Add(CreateSection("Price", priceGrid, 175));
            panel.Controls.Add(CreateSection("Category", categoryGrid, 245));
            panel.Controls.Add(CreateSection("Basic Details", itemGrid, 175));
            panel.Controls.Add(calculatorBox);
            panel.Controls.Add(title);

            return panel;
        }

        Control CreateRightUI()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(12);
            panel.BackColor = Color.FromArgb(248, 250, 252);

            Label title = new Label();
            title.Text = "Category Setup";
            title.Dock = DockStyle.Top;
            title.Height = 36;
            title.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(30, 41, 59);

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Font = new Font("Segoe UI", 9, FontStyle.Regular);

            AddCategoryTab(tabs, "Main", CreateMainCategoryManager());
            AddCategoryTab(tabs, "Clothes", CreateClothesManager());
            AddCategoryTab(tabs, "Footwears", CreateFootwearManager());
            AddCategoryTab(tabs, "Toys", CreateToyManager());

            panel.Controls.Add(tabs);
            panel.Controls.Add(title);

            return panel;
        }

        void AddCategoryTab(TabControl tabs, string title, Control content)
        {
            TabPage page = new TabPage(title);
            page.Padding = new Padding(10);
            page.BackColor = Color.White;
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            tabs.TabPages.Add(page);
        }

        TableLayoutPanel CreateFormGrid()
        {
            TableLayoutPanel grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 3;
            grid.RowCount = 0;
            grid.Padding = new Padding(8);
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return grid;
        }

        GroupBox CreateSection(string text, Control content, int height)
        {
            GroupBox box = new GroupBox();
            box.Text = text;
            box.Dock = DockStyle.Top;
            box.Height = height;
            box.Padding = new Padding(8);
            box.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            content.Dock = DockStyle.Fill;
            box.Controls.Add(content);

            return box;
        }

        GroupBox CreateCalculatorSection()
        {
            cmbCalcOperator.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCalcOperator.Items.AddRange(new object[] { "+", "-", "*", "/" });
            cmbCalcOperator.SelectedIndex = 0;
            cmbCalcOperator.Width = 60;

            txtCalcResult.ReadOnly = true;
            txtCalcResult.BackColor = Color.WhiteSmoke;
            txtCalcFirst.Width = 110;
            txtCalcSecond.Width = 110;
            txtCalcResult.Width = 130;

            Button btnCalculate = new Button();
            btnCalculate.Text = "Calculate";
            btnCalculate.Width = 110;
            btnCalculate.Height = 32;
            btnCalculate.BackColor = Color.FromArgb(14, 116, 144);
            btnCalculate.ForeColor = Color.White;
            btnCalculate.FlatStyle = FlatStyle.Flat;
            btnCalculate.FlatAppearance.BorderSize = 0;
            btnCalculate.Click += (s, e) => RunCalculator();

            Label lblEq = new Label();
            lblEq.Text = "=";
            lblEq.AutoSize = true;
            lblEq.TextAlign = ContentAlignment.MiddleCenter;
            lblEq.Margin = new Padding(0, 8, 0, 0);

            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.WrapContents = false;
            row.FlowDirection = FlowDirection.LeftToRight;
            row.Padding = new Padding(8, 10, 8, 8);
            row.AutoScroll = false;
            row.Controls.Add(txtCalcFirst);
            row.Controls.Add(cmbCalcOperator);
            row.Controls.Add(txtCalcSecond);
            row.Controls.Add(btnCalculate);
            row.Controls.Add(lblEq);
            row.Controls.Add(txtCalcResult);

            return CreateSection("Calculator", row, 88);
        }

        void RunCalculator()
        {
            try
            {
                decimal first = Convert.ToDecimal(txtCalcFirst.Text);
                decimal second = Convert.ToDecimal(txtCalcSecond.Text);
                string op = cmbCalcOperator.Text;
                decimal result = 0m;

                if (op == "+") result = first + second;
                else if (op == "-") result = first - second;
                else if (op == "*") result = first * second;
                else if (op == "/")
                {
                    if (second == 0m)
                    {
                        MessageBox.Show("Cannot divide by zero.");
                        return;
                    }
                    result = first / second;
                }
                else
                {
                    MessageBox.Show("Select a valid operator.");
                    return;
                }

                txtCalcResult.Text = result.ToString("0.##");
            }
            catch
            {
                MessageBox.Show("Enter valid numbers for calculator.");
            }
        }

        Control CreateFootwearManager()
        {
            GroupBox box = new GroupBox();
            box.Text = "Add Footwear Category";
            box.Dock = DockStyle.Fill;
            box.Padding = new Padding(10);

            TextBox txtSub = new TextBox();
            ComboBox cmbGender = new ComboBox();
            TextBox txtType = new TextBox();
            TextBox txtActual = new TextBox();

            cmbGender.Items.AddRange(new string[]
            {
                "Boys",
                "Girls",
                "Unisex",
                "NA"
            });
            cmbGender.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGender.SelectedIndex = 0;

            Button btnAdd = CreateManagerButton("Add Footwear");

            btnAdd.Click += (s, e) =>
            {
                bool saved = AddFootwearData(
                    txtSub.Text,
                    cmbGender.Text,
                    txtType.Text,
                    txtActual.Text
                );

                if (saved)
                {
                    txtSub.Clear();
                    txtType.Clear();
                    txtActual.Clear();
                    cmbGender.SelectedIndex = 0;
                }
            };

            box.Controls.Add(CreateManagerContent(btnAdd,
                ("Sub Category", txtSub),
                ("Gender", cmbGender),
                ("Item Type", txtType),
                ("Actual Item", txtActual)));

            return box;
        }

        Control CreateToyManager()
        {
            GroupBox box = new GroupBox();
            box.Text = "Add Toy Category";
            box.Dock = DockStyle.Fill;
            box.Padding = new Padding(10);

            TextBox txtSub = new TextBox();
            TextBox txtType = new TextBox();
            TextBox txtActual = new TextBox();

            Button btnAdd = CreateManagerButton("Add Toy");

            btnAdd.Click += (s, e) =>
            {
                bool saved = AddToyData(
                    txtSub.Text.Trim(),
                    txtType.Text.Trim(),
                    txtActual.Text.Trim()
                );

                if (saved)
                {
                    txtSub.Clear();
                    txtType.Clear();
                    txtActual.Clear();
                }
            };

            box.Controls.Add(CreateManagerContent(btnAdd,
                ("Sub Category", txtSub),
                ("Item Type", txtType),
                ("Actual Item", txtActual)));

            return box;
        }

        Control CreateClothesManager()
        {
            GroupBox box = new GroupBox();
            box.Text = "Add Clothes Category";
            box.Dock = DockStyle.Fill;
            box.Padding = new Padding(10);

            TextBox txtSub = new TextBox();
            TextBox txtType = new TextBox();
            TextBox txtActual = new TextBox();

            ComboBox cmbGenderRight = new ComboBox();
            cmbGenderRight.DropDownStyle = ComboBoxStyle.DropDownList;

            cmbGenderRight.Items.Add("Girls");
            cmbGenderRight.Items.Add("Boys");
            cmbGenderRight.Items.Add("Unisex");
            cmbGenderRight.Items.Add("NA");

            cmbGenderRight.SelectedIndex = 0;

            Button btnAdd = CreateManagerButton("Add Clothes");

            btnAdd.Click += (s, e) =>
            {
                bool saved = AddClothesData(
                    txtSub.Text,
                    txtType.Text,
                    txtActual.Text,
                    cmbGenderRight.Text
                );

                if (saved)
                {
                    txtSub.Clear();
                    txtType.Clear();
                    txtActual.Clear();
                    cmbGenderRight.SelectedIndex = 0;
                }
            };

            box.Controls.Add(CreateManagerContent(btnAdd,
                ("Sub Category", txtSub),
                ("Item Type", txtType),
                ("Gender", cmbGenderRight),
                ("Actual Item", txtActual)));

            return box;
        }

        Control CreateMainCategoryManager()
        {
            GroupBox box = new GroupBox();
            box.Text = "Add Main Category";
            box.Dock = DockStyle.Fill;
            box.Padding = new Padding(10);

            TextBox txtMain = new TextBox();

            Button btnAdd = CreateManagerButton("Add Category");

            btnAdd.Click += (s, e) =>
            {
                if (txtMain.Text.Trim() == "")
                {
                    MessageBox.Show("Enter category name");
                    return;
                }

                string check = $@"
        SELECT COUNT(*)
        FROM inv_item_category_master
        WHERE {CiEquals("main_category", txtMain.Text)}";

                var dt = DB.GetData(check);

                if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                {
                    MessageBox.Show("Category already exists");
                    return;
                }

                string q = $@"
        INSERT INTO inv_item_category_master (main_category)
        VALUES ('{txtMain.Text.Trim()}')";

                DB.Execute(q);

                MessageBox.Show("Main category added");

                txtMain.Clear();

                LoadMainCategories(); // refresh left dropdown
            };

            box.Controls.Add(CreateManagerContent(btnAdd,
                ("Main Category", txtMain)));

            return box;
        }

        Control CreateManagerContent(Button action, params (string Label, Control Control)[] fields)
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;

            TableLayoutPanel grid = CreateFormGrid();
            grid.Dock = DockStyle.Top;
            grid.Height = Math.Max(70, fields.Length * 34 + 14);

            foreach (var field in fields)
                AddRow(grid, field.Label, field.Control);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.Height = 48;
            actions.Padding = new Padding(8, 8, 0, 0);
            actions.Controls.Add(action);

            panel.Controls.Add(actions);
            panel.Controls.Add(grid);

            return panel;
        }

        Button CreateManagerButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 140;
            button.Height = 34;
            button.BackColor = Color.FromArgb(37, 99, 235);
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        bool AddClothesData(string sub, string type, string actual, string gender)
        {
            if (sub == "" || type == "" || actual == "")
            {
                MessageBox.Show("Fill all fields");
                return false;
            }

            string check = $@"
    SELECT COUNT(*)
    FROM inv_item_category_master
    WHERE {CiEquals("main_category", "Clothes")}
    AND {CiEquals("sub_category", sub)}
    AND {CiEquals("item_type", type)}
    AND {CiEquals("gender", gender)}
    AND {CiEquals("actual_item", actual)}";

            var dt = DB.GetData(check);

            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
            {
                MessageBox.Show("Item already exists");
                return false;
            }

            string q = $@"
    INSERT INTO inv_item_category_master
    (main_category,sub_category,item_type,gender,actual_item)
    VALUES
    ('Clothes','{sub}','{type}','{gender}','{actual}')";

            DB.Execute(q);

            MessageBox.Show("Saved");

            RefreshLeftDropdowns();
            return true;
        }

        bool AddFootwearData(string sub, string gender, string type, string actual)
        {
            if (sub == "" || gender == "" || type == "" || actual == "")
            {
                MessageBox.Show("Fill all fields");
                return false;
            }

            string check = $@"
    SELECT COUNT(*)
    FROM inv_item_category_master
    WHERE {CiEquals("main_category", "Footwears")}
    AND {CiEquals("sub_category", sub)}
    AND {CiEquals("gender", gender)}
    AND {CiEquals("item_type", type)}
    AND {CiEquals("actual_item", actual)}";

            var dt = DB.GetData(check);

            int count = Convert.ToInt32(dt.Rows[0][0]);

            if (count > 0)
            {
                MessageBox.Show("Footwear already exists");
                return false;
            }

            string q = $@"
    INSERT INTO inv_item_category_master
    (main_category,sub_category,gender,item_type,actual_item)
    VALUES
    ('Footwears','{sub}','{gender}','{type}','{actual}')";

            DB.Execute(q);

            MessageBox.Show("Footwear added successfully");

            RefreshLeftDropdowns();
            return true;
        }

        bool AddToyData(string sub, string type, string actual)
        {
            if (sub == "" || type == "" || actual == "")
            {
                MessageBox.Show("Fill all fields");
                return false;
            }

            string check = $@"
    SELECT COUNT(*)
    FROM inv_item_category_master
    WHERE {CiEquals("main_category", "Toys")}
    AND {CiEquals("sub_category", sub)}
    AND {CiEquals("item_type", type)}
    AND {CiEquals("actual_item", actual)}";

            var dt = DB.GetData(check);

            int count = Convert.ToInt32(dt.Rows[0][0]);

            if (count > 0)
            {
                MessageBox.Show("This toy already exists");
                return false;
            }

            string q = $@"
    INSERT INTO inv_item_category_master
    (main_category,sub_category,item_type,actual_item)
    VALUES
    ('Toys','{sub}','{type}','{actual}')";

            DB.Execute(q);

            MessageBox.Show("Toy added successfully");

            RefreshLeftDropdowns();
            return true;
        }

        void RefreshLeftDropdowns()
        {
            LoadSubCategory();
            LoadItemType();
            LoadActualItems();
            LoadSizes();

            cmbSub.Refresh();
            cmbItemType.Refresh();
            cmbActual.Refresh();
            cmbSize.Refresh();
        }

        void LoadSubCategory()
        {
            if (cmbMain.Text == "")
                return;

            string q = $@"
    SELECT DISTINCT sub_category
    FROM inv_item_category_master
    WHERE {CiEquals("main_category", cmbMain.Text)}
    AND sub_category <> 'NA'
    ORDER BY sub_category";

            var dt = DB.GetData(q);

            cmbSub.Items.Clear();

            foreach (DataRow r in dt.Rows)
            {
                cmbSub.Items.Add(r["sub_category"].ToString());
            }

            cmbSub.SelectedIndex = -1;
        }

        void LoadItemType()
        {
            if (cmbMain.Text == "" || cmbSub.Text == "")
                return;

            string q = $@"
    SELECT DISTINCT item_type
    FROM inv_item_category_master
    WHERE {CiEquals("main_category", cmbMain.Text)}
    AND {CiEquals("sub_category", cmbSub.Text)}
    AND item_type <> 'NA'";

            if (!IsToys())
            {
                if (cmbGender.Text == "")
                    return;

                q += $@"
    AND {CiEquals("gender", cmbGender.Text)}";
            }

            q += @"
    ORDER BY item_type";

            var dt = DB.GetData(q);

            cmbItemType.Items.Clear();

            foreach (DataRow r in dt.Rows)
            {
                cmbItemType.Items.Add(r["item_type"].ToString());
            }

            cmbItemType.SelectedIndex = -1;
        }

        void LoadGenderOptions()
        {
            if (cmbMain.Text == "" || cmbSub.Text == "")
                return;

            string q = $@"
    SELECT DISTINCT gender
    FROM inv_item_category_master
    WHERE {CiEquals("main_category", cmbMain.Text)}
    AND {CiEquals("sub_category", cmbSub.Text)}
    AND gender <> 'NA'
    ORDER BY gender";

            var dt = DB.GetData(q);

            cmbGender.Items.Clear();

            foreach (DataRow r in dt.Rows)
            {
                cmbGender.Items.Add(r["gender"].ToString());
            }

            cmbGender.SelectedIndex = -1;
        }

        void LoadSizes()
        {
            if (cmbMain.Text == "" || cmbSub.Text == "" || cmbItemType.Text == "")
                return;

            string q = $@"
    SELECT DISTINCT size
    FROM inv_item_category_master
    WHERE {CiEquals("main_category", cmbMain.Text)}
    AND {CiEquals("sub_category", cmbSub.Text)}
    AND {CiEquals("item_type", cmbItemType.Text)}
    AND size <> 'NA'
    ORDER BY size";

            var dt = DB.GetData(q);

            cmbSize.Items.Clear();

            foreach (DataRow r in dt.Rows)
            {
                cmbSize.Items.Add(r["size"].ToString());
            }

            cmbSize.SelectedIndex = -1;
        }

        void AddRow(TableLayoutPanel g, string label, Control ctrl)
        {
            g.RowCount++;
            g.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

            Label l = new Label();
            l.Text = label;
            l.Dock = DockStyle.Fill;

            // Compact width for better readability on large screens
            ctrl.Width = 250;
            ctrl.Height = 26;
            ctrl.MinimumSize = new Size(220, 26);
            ctrl.MaximumSize = new Size(320, 26);
            ctrl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            ctrl.Dock = DockStyle.None;

            g.Controls.Add(l, 0, g.RowCount - 1);
            g.Controls.Add(ctrl, 1, g.RowCount - 1);
        }

        void LoadMainCategories()
        {
            var dt = DB.GetData("SELECT DISTINCT main_category FROM inv_item_category_master");

            cmbMain.Items.Clear();

            foreach (DataRow r in dt.Rows)
                cmbMain.Items.Add(r[0].ToString());
        }

        void MainCategoryChanged(object s, EventArgs e)
        {
            string q = $@"
            SELECT DISTINCT sub_category
            FROM inv_item_category_master
            WHERE main_category='{cmbMain.Text}'";

            var dt = DB.GetData(q);

            cmbSub.Items.Clear();

            foreach (DataRow r in dt.Rows)
                cmbSub.Items.Add(r[0].ToString());
        }

        void LoadItemTypes(object s, EventArgs e)
        {
            string q = $@"
            SELECT DISTINCT item_type
            FROM inv_item_category_master
            WHERE main_category='{cmbMain.Text}'
            AND sub_category='{cmbSub.Text}'";

            var dt = DB.GetData(q);

            cmbItemType.Items.Clear();

            foreach (DataRow r in dt.Rows)
                cmbItemType.Items.Add(r[0].ToString());
        }

        void LoadActualItems()
        {
            if (string.IsNullOrWhiteSpace(cmbMain.Text))
                return;

            string q = "";

            // -------- CLOTHES --------
            if (IsClothes())
            {
                if (cmbSub.Text == "" || cmbItemType.Text == "")
                    return;

                q = $@"
                    SELECT DISTINCT actual_item
                    FROM inv_item_category_master
                    WHERE {CiEquals("main_category", cmbMain.Text)}
                    AND {CiEquals("sub_category", cmbSub.Text)}
                    AND {CiEquals("item_type", cmbItemType.Text)}
                    AND {CiEquals("gender", cmbGender.Text)}
                    ORDER BY actual_item";
            }

            // -------- FOOTWEAR --------
            else if (IsFootwears())
            {
                if (cmbSub.Text == "" || cmbItemType.Text == "")
                    return;

                //q = $@"
                //    SELECT DISTINCT actual_item
                //    FROM inv_item_category_master
                //    WHERE main_category='Footwears'
                //    AND sub_category='{cmbSub.Text}'
                //    AND item_type='{cmbItemType.Text}'
                //    ORDER BY actual_item";

                q = $@"
                    SELECT DISTINCT actual_item
                    FROM inv_item_category_master
                    WHERE {CiEquals("main_category", cmbMain.Text)}
                    AND {CiEquals("sub_category", cmbSub.Text)}
                    AND {CiEquals("item_type", cmbItemType.Text)}
                    AND {CiEquals("gender", cmbGender.Text)}
                    ORDER BY actual_item";
            }

            // -------- TOYS --------
            else if (IsToys())
            {
                if (cmbSub.Text == "" || cmbItemType.Text == "")
                    return;

                q = $@"
                    SELECT DISTINCT actual_item
                    FROM inv_item_category_master
                    WHERE {CiEquals("main_category", cmbMain.Text)}
                    AND {CiEquals("sub_category", cmbSub.Text)}
                    AND {CiEquals("item_type", cmbItemType.Text)}
                    ORDER BY actual_item";
            }

            var dt = DB.GetData(q);

            cmbActual.Items.Clear();

            foreach (DataRow r in dt.Rows)
            {
                cmbActual.Items.Add(r["actual_item"].ToString());
            }
        }

        //void GenerateItemDetails()
        //{
        //    if (string.IsNullOrWhiteSpace(cmbMain.Text) ||
        //        string.IsNullOrWhiteSpace(cmbActual.Text))
        //        return;

        //    // --- NEW FORMAT BASED ON YOUR REQUIREMENT ---

        //    string br = SafeCode(txtBrand.Text, 2);      // Brand
        //    string sp = SafeCode(txtSupplier.Text, 2);   // Supplier

        //    // Main category → 1 letter
        //    string mc = SafeCode(cmbMain.Text, 1);

        //    // Subcategory → 2 letter
        //    string sc = SafeCode(cmbSub.Text, 2);

        //    // Gender → 1 letter
        //    string g = SafeCode(cmbGender.Text, 1);

        //    // Item type → 1 letter
        //    string it = SafeCode(cmbItemType.Text, 1);

        //    // Actual item → 6 letter
        //    //string ai = SafeCode(cmbActual.Text, 6);
        //    string ai = GetActualItemCode(cmbActual.Text);

        //    // Size → full text
        //    string sz = string.IsNullOrWhiteSpace(cmbSize.Text) ? "NA" : cmbSize.Text.ToUpper();

        //    // FINAL CODE FORMAT
        //    //string code = $"{br}-{sp}-{mc}-{sc}-{g}-{it}-{ai}-{sz}";
        //    string code = $"{sc}{g}{it}-{ai}{sz}";

        //    // Name (optional better format)
        //    string name = $"{cmbMain.Text} {cmbSub.Text} {cmbGender.Text} {cmbActual.Text} {cmbSize.Text}";

        //    txtCode.Text = code;
        //    txtName.Text = name;
        //}

        void GenerateItemDetails()
        {
            if (string.IsNullOrWhiteSpace(cmbMain.Text) ||
                string.IsNullOrWhiteSpace(cmbActual.Text))
                return;

            if (IsToys())
            {
                GenerateToyCode();
            }
            else if (IsClothes())
            {
                GenerateClothesCode();
            }
            else if (IsFootwears())
            {
                GenerateFootwearCode(); // ✅ SAME STYLE AS CLOTHES
            }
        }

        void GenerateClothesCode()
        {
            string mc = SafeCode(cmbMain.Text, 1);   // C
            string sc = SafeCode(cmbSub.Text, 2);    // PA
            string g = SafeCode(cmbGender.Text, 1); // B
            string it = SafeCode(cmbItemType.Text, 1); // S

            string ai = GetActualItemCode(cmbActual.Text); // max 5

            string sz = string.IsNullOrWhiteSpace(cmbSize.Text)
                ? "NA"
                : cmbSize.Text.ToUpper();

            // 🔥 SHORT & CLEAN CODE
            string code = $"{sc}{g}{it}-{ai}{sz}";

            string name = $"{cmbMain.Text} {cmbSub.Text} {cmbGender.Text} {cmbActual.Text} {cmbSize.Text}";

            txtCode.Text = code;
            txtName.Text = name;
        }

        //void GenerateFootwearCode()
        //{
        //    // Same as clothes
        //    string mc = SafeCode(cmbMain.Text, 1);   // F
        //    string sc = SafeCode(cmbSub.Text, 2);    // SH
        //    string g = SafeCode(cmbGender.Text, 1); // B
        //    string it = SafeCode(cmbItemType.Text, 1); // S

        //    // Same actual logic (max 5)
        //    string ai = GetActualItemCode(cmbActual.Text);

        //    // Size full
        //    string sz = string.IsNullOrWhiteSpace(cmbSize.Text)
        //        ? "NA"
        //        : cmbSize.Text.ToUpper();

        //    // 🔥 SAME FORMAT AS CLOTHES
        //    string code = $"{mc}{sc}{g}{it}-{ai}{sz}";

        //    // 🔥 SAME NAME FORMAT AS CLOTHES
        //    string name = $"{cmbMain.Text} {cmbSub.Text} {cmbGender.Text} {cmbActual.Text} {cmbSize.Text}";

        //    txtCode.Text = code;
        //    txtName.Text = name;
        //}

        void GenerateFootwearCode()
        {
            string code = "";
            string name = $"{cmbMain.Text} {cmbSub.Text} {cmbGender.Text} {cmbItemType.Text} {cmbActual.Text} {cmbSize.Text}";

            // Footwears + Item type "Item" => F + sub(2) + gender(1) + price
            if (string.Equals(cmbItemType.Text.Trim(), "Item", StringComparison.OrdinalIgnoreCase))
            {
                string sc = SafeCode(cmbSub.Text, 2);
                string g = SafeCode(cmbGender.Text, 1);
                string pricePart = "";
                if (decimal.TryParse(txtSell.Text, out decimal sellPrice) && sellPrice > 0)
                {
                    int wholePrice = (int)Math.Round(sellPrice, MidpointRounding.AwayFromZero);
                    pricePart = wholePrice.ToString();
                }

                code = "F" + sc + g + pricePart;
                txtCode.Text = code;
                txtName.Text = name;
                return;
            }

            // 🔥 MIX CASE (SPECIAL)
            if (cmbSub.Text.Trim().ToLower() == "mix")
            {
                string sc = GetFixed(cmbSub.Text, 3);          // MIX
                string it = GetFixed(cmbItemType.Text, 4);     // ITEM
                string g = SafeCode(cmbGender.Text, 1);
                string ai = string.IsNullOrWhiteSpace(cmbActual.Text)
                    ? "NA"
                    : cmbActual.Text.Replace(" ", "").ToUpper(); // FULL

                code = $"{sc}-{g}-{ai}";
            }
            else
            {
                // 🔥 NORMAL CASE (same as before)
                string mc = SafeCode(cmbMain.Text, 1);
                string sc = SafeCode(cmbSub.Text, 2);
                string g = SafeCode(cmbGender.Text, 1);
                string it = SafeCode(cmbItemType.Text, 1);

                string ai = GetActualItemCode(cmbActual.Text);

                string sz = string.IsNullOrWhiteSpace(cmbSize.Text)
                    ? "NA"
                    : cmbSize.Text.ToUpper();

                code = $"{sc}{g}{it}-{ai}{sz}";
            }

            txtCode.Text = code;
            txtName.Text = name;
        }

        void GenerateToyCode()
        {
            string toyPrefix = "T";
            string pricePart = "";
            if (decimal.TryParse(txtSell.Text, out decimal sellPrice) && sellPrice > 0)
            {
                int wholePrice = (int)Math.Round(sellPrice, MidpointRounding.AwayFromZero);
                pricePart = wholePrice.ToString();
            }

            string code = toyPrefix + pricePart;

            string name = $"{cmbMain.Text} {cmbSub.Text} {cmbItemType.Text} {cmbActual.Text}";

            txtCode.Text = code;
            txtName.Text = name;
        }

        string GetActualItemCode(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "NA";

            string val = text.ToUpper();

            if (val.Length >= 6)
                return val.Substring(0, 5); // max 5

            return val; // jitna hai utna hi
        }

        string GetFixed(string text, int len)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "NA";

            string val = text.Replace(" ", "").ToUpper();

            return val.Length >= len ? val.Substring(0, len) : val;
        }

        // Actual item special rule (max 3)
        string GetActual3(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "NA";

            string val = text.Replace(" ", "").ToUpper();

            return val.Length >= 3 ? val.Substring(0, 3) : val;
        }

        //void GenerateItemDetails()
        //{
        //    if (string.IsNullOrWhiteSpace(cmbMain.Text) ||
        //        string.IsNullOrWhiteSpace(cmbActual.Text))
        //        return;

        //    string mc = SafeCode(cmbMain.Text, 2);
        //    string sc = SafeCode(cmbSub.Text, 2);
        //    string g = SafeCode(cmbGender.Text, 1);
        //    string it = SafeCode(cmbActual.Text, 3);
        //    string sz = string.IsNullOrWhiteSpace(cmbSize.Text) ? "" : cmbSize.Text;

        //    string br = SafeCode(txtBrand.Text, 2);
        //    string sp = SafeCode(txtSupplier.Text, 2);
        //    string cl = SafeCode(txtColor.Text, 2);

        //    string code = "";
        //    string name = "";

        //    // ---------------- CLOTHES ----------------
        //    if (cmbMain.Text == "Clothes")
        //    {
        //        code = $"{mc}-{sc}-{g}-{it}-{sz}-{br}-{cl}";
        //        name = $"{cmbMain.Text} {cmbSub.Text} {cmbGender.Text} {cmbActual.Text}";
        //    }

        //    // ---------------- FOOTWEAR ----------------

        //    else if (cmbMain.Text == "Footwears")
        //    {
        //        code = $"{mc}-{sc}-{it}-{sz}-{br}";
        //        name = $"{cmbMain.Text} {cmbSub.Text} {cmbActual.Text}";
        //    }

        //    // ---------------- TOYS ----------------
        //    else if (cmbMain.Text == "Toys")
        //    {
        //        code = $"{mc}-{sc}-{it}";
        //        name = $"{cmbMain.Text} {cmbSub.Text} {cmbActual.Text}";
        //    }

        //    txtCode.Text = code;
        //    txtName.Text = name;
        //}

        //void CalcPrice(object s, EventArgs e)
        //{
        //    try
        //    {
        //        decimal price = Convert.ToDecimal(txtBeforeTax.Text);
        //        decimal gst = Convert.ToDecimal(txtGST.Text);

        //        txtSell.Text = (price + (price * gst / 100)).ToString("0.00");
        //    }
        //    catch { }
        //}

        //void CalcPrice(object s, EventArgs e)
        //{
        //    try
        //    {
        //        decimal price = Convert.ToDecimal(txtBeforeTax.Text);
        //        decimal gst = Convert.ToDecimal(txtGST.Text);

        //        // Step 1: normal final
        //        decimal final = price + (price * gst / 100);

        //        // Step 2: round to nearest 9 ending
        //        decimal rounded = Math.Floor(final / 10) * 10 - 1;

        //        // Safety (negative na ho)
        //        if (rounded <= 0)
        //            rounded = final;

        //        txtSell.Text = Math.Round(rounded).ToString();
        //    }
        //    catch { }
        //}

        void CalcPrice(object s, EventArgs e)
        {
            try
            {
                int before = Convert.ToInt32(txtBeforeTax.Text);
                int gst = Convert.ToInt32(txtGST.Text);

                // 🔥 Exact integer calculation
                int gstAmount = (before * gst) / 100;

                int finalPrice = before + gstAmount;

                txtSell.Text = finalPrice.ToString();
            }
            catch { }
        }

        void SaveItem(object sender, EventArgs e)
        {
            try
            {
                // -------- VALIDATION --------
                if (string.IsNullOrWhiteSpace(txtCode.Text))
                {
                    MessageBox.Show("Item code missing");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Item name missing");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtCost.Text) ||
                    string.IsNullOrWhiteSpace(txtBeforeTax.Text) ||
                    string.IsNullOrWhiteSpace(txtGST.Text))
                {
                    MessageBox.Show("Enter price details");
                    return;
                }

                decimal cost = Convert.ToDecimal(txtCost.Text);
                decimal beforeTax = Convert.ToDecimal(txtBeforeTax.Text);
                decimal gst = Convert.ToDecimal(txtGST.Text);
                Int64 sell = Convert.ToInt64(txtSell.Text);

                string baseCode = txtCode.Text.Trim();
                string finalCode = baseCode;
                bool isToyItemTypeItem =
                    IsToys() &&
                    string.Equals(cmbItemType.Text.Trim(), "Item", StringComparison.OrdinalIgnoreCase);
                bool isFootwearItemTypeItem =
                    IsFootwears() &&
                    string.Equals(cmbItemType.Text.Trim(), "Item", StringComparison.OrdinalIgnoreCase);
                bool isStrictDuplicateCase = isToyItemTypeItem || isFootwearItemTypeItem;

                // Toys/Footwears + Item type "Item" => strict duplicate block (no serial child code)
                if (isStrictDuplicateCase)
                {
                    string qStrictDup = $@"
        SELECT COUNT(*) 
        FROM inv_items_master 
        WHERE item_code = '{baseCode}'";

                    var dtStrictDup = DB.GetData(qStrictDup);
                    if (Convert.ToInt32(dtStrictDup.Rows[0][0]) > 0)
                    {
                        MessageBox.Show($"Error: Code already exists ({baseCode}). Duplicate not allowed.");
                        return;
                    }
                }

                // -------- CHECK IF BASE (PARENT) EXISTS --------
                string qCheck = $@"
        SELECT serial_no 
        FROM inv_items_master 
        WHERE item_code = '{baseCode}'";

                var dt = DB.GetData(qCheck);

                if (!isStrictDuplicateCase && dt.Rows.Count > 0)
                {
                    // -------- DUPLICATE CASE --------
                    int parentSerial = Convert.ToInt32(dt.Rows[0]["serial_no"]);

                    int newSerial = parentSerial + 1;

                    // UPDATE parent counter
                    string qUpdate = $@"
            UPDATE inv_items_master 
            SET serial_no = {newSerial}
            WHERE item_code = '{baseCode}'";

                    DB.Execute(qUpdate);

                    // NEW CHILD CODE
                    finalCode = baseCode + "-" + newSerial;
                }
                else
                {
                    // -------- FIRST TIME --------
                    finalCode = baseCode;
                }

                // -------- SAFETY CHECK (UNIQUE) --------
                string qDup = $@"
        SELECT COUNT(*) 
        FROM inv_items_master 
        WHERE item_code = '{finalCode}'";

                var dtDup = DB.GetData(qDup);

                if (Convert.ToInt32(dtDup.Rows[0][0]) > 0)
                {
                    MessageBox.Show("Error: Duplicate code conflict");
                    return;
                }

                // -------- INSERT --------
                string qInsert = $@"
        INSERT INTO inv_items_master
        (
            item_code,
            serial_no,
            item_name,
            main_category,
            sub_category,
            gender,
            item_type,
            actual_item,
            size,
            color,
            cost_price,
            price_before_tax,
            selling_price,
            brand,
            supplier_name,
            GST
        )
        VALUES
        (
            '{finalCode}',
            0,
            '{txtName.Text}',
            '{cmbMain.Text}',
            '{cmbSub.Text}',
            '{cmbGender.Text}',
            '{cmbItemType.Text}',
            '{cmbActual.Text}',
            '{cmbSize.Text}',
            '{""}',
            {cost},
            {beforeTax},
            {sell},
            '{txtBrand.Text}',
            '{txtSupplier.Text}',
            {gst}
        )";

                DB.Execute(qInsert);

                lastSavedItemCode = finalCode;
                lastSavedItemName = txtName.Text.Trim();
                lastSavedSize = cmbSize.Text.Trim();
                lastSavedPrice = txtSell.Text.Trim();
                UpdateLastAddedSummary();
                printDoneForCurrentItem = false;
                stockInDoneForCurrentItem = false;
                btnPrint.Enabled = true;
                btnInStock.Enabled = true;

                MessageBox.Show($"Item saved: {finalCode}");

                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            lastBrand = txtBrand.Text;
            lastSupplier = txtSupplier.Text;
        }

        void ClearFields()
        {
            txtCode.Clear();
            txtName.Clear();
            //txtBrand.Clear();
            //txtSupplier.Clear();
            //txtColor.Clear();

            txtCost.Clear();
            txtBeforeTax.Clear();
            txtGST.Text = "0";
            txtSell.Clear();

            cmbMain.SelectedIndex = -1;
            cmbSub.SelectedIndex = -1;
            cmbItemType.SelectedIndex = -1;
            cmbActual.SelectedIndex = -1;
            cmbSize.SelectedIndex = -1;
        }

        bool TryGetItemContext(out string code, out string name, out string size, out string price)
        {
            code = lastSavedItemCode;
            if (string.IsNullOrWhiteSpace(code))
                code = txtCode.Text.Trim();

            name = txtName.Text.Trim();
            size = cmbSize.Text.Trim();
            price = txtSell.Text.Trim();

            if (string.IsNullOrWhiteSpace(code))
                return false;

            if (string.IsNullOrWhiteSpace(name))
                name = lastSavedItemName;

            if (string.IsNullOrWhiteSpace(size))
                size = lastSavedSize;

            if (string.IsNullOrWhiteSpace(price))
                price = lastSavedPrice;

            return true;
        }

        void StockInFromAddItem()
        {
            try
            {
                if (!TryGetItemContext(out string code, out _, out _, out _))
                {
                    MessageBox.Show("Save item first or select item details.");
                    return;
                }

                if (!TryPromptQuantity("Enter IN quantity", "Stock IN", out int qty))
                    return;

                using (MySqlConnection con = DB.GetConnection())
                {
                    con.Open();

                    MySqlCommand itemCmd = new MySqlCommand(
                        "SELECT item_name FROM inv_items_master WHERE item_code=@code LIMIT 1", con);
                    itemCmd.Parameters.AddWithValue("@code", code);

                    object itemResult = itemCmd.ExecuteScalar();
                    string itemName = itemResult == null ? "" : itemResult.ToString();

                    if (string.IsNullOrWhiteSpace(itemName))
                    {
                        MessageBox.Show($"Item code not found in master: {code}\nPlease save item first, then do IN.");
                        return;
                    }

                    MySqlCommand stockCmd = new MySqlCommand(
                        "SELECT quantity FROM inv_stock WHERE item_code=@code LIMIT 1", con);
                    stockCmd.Parameters.AddWithValue("@code", code);

                    object stockResult = stockCmd.ExecuteScalar();

                    if (stockResult == null)
                    {
                        MySqlCommand insert = new MySqlCommand(
                            "INSERT INTO inv_stock (item_code, item_name, quantity) VALUES (@code,@name,@qty)", con);
                        insert.Parameters.AddWithValue("@code", code);
                        insert.Parameters.AddWithValue("@name", itemName);
                        insert.Parameters.AddWithValue("@qty", qty);
                        insert.ExecuteNonQuery();
                    }
                    else
                    {
                        int current = Convert.ToInt32(stockResult);
                        int finalQty = current + qty;

                        MySqlCommand update = new MySqlCommand(
                            "UPDATE inv_stock SET quantity=@qty WHERE item_code=@code", con);
                        update.Parameters.AddWithValue("@qty", finalQty);
                        update.Parameters.AddWithValue("@code", code);
                        update.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Stock IN done: {code} (+{qty})");
                stockInDoneForCurrentItem = true;
                btnInStock.Enabled = false;
                if (printDoneForCurrentItem)
                    btnPrint.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Stock IN failed: " + ex.Message);
            }
        }

        bool TryPromptQuantity(string prompt, string title, out int qty)
        {
            qty = 0;
            string input = Interaction.InputBox(prompt, title, "1");
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (!int.TryParse(input, out qty) || qty <= 0)
            {
                MessageBox.Show("Enter valid quantity.");
                return false;
            }

            return true;
        }

        void PrintLabelFromAddItem()
        {
            try
            {
                if (!TryGetItemContext(out string code, out _, out string size, out string price))
                {
                    MessageBox.Show("Save item first or select item details.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(size))
                    size = "NA";

                if (string.IsNullOrWhiteSpace(price))
                    price = "0";

                lastSavedItemCode = code;
                lastSavedSize = size;
                lastSavedPrice = price;

                GenerateBarcode(code);
                PrintLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Print failed: " + ex.Message);
            }
        }

        void GenerateBarcode(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    barcodeImage?.Dispose();
                    barcodeImage = null;
                    return;
                }

                var writer = new ZXing.BarcodeWriter<Bitmap>
                {
                    Format = ZXing.BarcodeFormat.CODE_128,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = 180,
                        Height = 50,
                        Margin = 1
                    },
                    Renderer = new ZXing.Windows.Compatibility.BitmapRenderer()
                };

                barcodeImage?.Dispose();
                barcodeImage = writer.Write(text.Trim());
            }
            catch
            {
                barcodeImage = null;
                MessageBox.Show("Barcode generation failed");
            }
        }

        void PrintLabel()
        {
            if (barcodeImage == null)
            {
                MessageBox.Show("Nothing to print");
                return;
            }

            if (!TryPromptQuantity("Enter label quantity", "Print Labels", out int qty))
                return;
            if (qty > short.MaxValue)
            {
                MessageBox.Show("Quantity too large.");
                return;
            }

            PrinterRouting.ApplyLabelPrinter(printDoc);
            printDoc.PrinterSettings.Copies = (short)qty;

            PaperSize labelSize = new PaperSize("Custom", 216, 98);
            printDoc.DefaultPageSettings.PaperSize = labelSize;
            printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            printDoc.Print();
            printDoneForCurrentItem = true;
            btnPrint.Enabled = false;
            if (stockInDoneForCurrentItem)
                btnInStock.Enabled = false;
        }

        void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            e.Graphics.TranslateTransform(
                -e.PageSettings.HardMarginX,
                -e.PageSettings.HardMarginY);

            Rectangle printArea = new Rectangle(
                0,
                0,
                e.PageSettings.PaperSize.Width,
                e.PageSettings.PaperSize.Height);

            DrawLabel(e.Graphics, printArea);
            e.HasMorePages = false;
        }

        void DrawLabel(Graphics g, Rectangle bounds)
        {
            g.Clear(Color.White);

            int pageWidth = bounds.Width;
            int topMargin = 20;

            Font textFont = new Font("Arial", 8, FontStyle.Bold);
            Brush brush = Brushes.Black;

            DateTime dt = DateTime.Now;
            string formattedDate = (dt.Year / 100).ToString("00") + dt.ToString("MMddyy");
            string firstLine = $"{formattedDate}  Size:{lastSavedSize}  ₹{lastSavedPrice}";

            SizeF textSize = g.MeasureString(firstLine, textFont);
            float textX = (pageWidth - textSize.Width) / 2;
            float textY = topMargin;

            g.DrawString(firstLine, textFont, brush, textX, textY);

            if (barcodeImage != null)
            {
                int barcodeWidth = 180;
                int barcodeHeight = 45;

                float barcodeX = (pageWidth - barcodeWidth) / 2;
                float barcodeY = textY + textSize.Height + 4;

                g.DrawImage(barcodeImage, barcodeX, barcodeY, barcodeWidth, barcodeHeight);
            }
        }

        void UpdateLastAddedSummary()
        {
            string size = string.IsNullOrWhiteSpace(lastSavedSize) ? "NA" : lastSavedSize;
            lblLastAdded.Text =
                $"Code: {lastSavedItemCode}\n" +
                $"Name: {lastSavedItemName}\n" +
                $"Size: {size}   Price: {lastSavedPrice}";
        }

    }
}







