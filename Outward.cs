
using System;
using System.Windows.Forms;

namespace BubbyPlanetShowroom
{
    public class Outward : UserControl
    {
        TextBox txtCode = new TextBox();

        public Outward()
        {
            txtCode.Dock = DockStyle.Top;
            txtCode.PlaceholderText = "Scan Barcode Here";
            txtCode.KeyDown += Scan;

            this.Controls.Add(txtCode);
        }

        void Scan(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DB.Execute($"UPDATE inv_stock SET quantity = quantity - 1 WHERE item_code='{txtCode.Text}' AND quantity > 0");
                DB.Execute($"INSERT INTO inv_transactions(item_code,type,qty) VALUES('{txtCode.Text}','OUT',1)");
                MessageBox.Show("Stock Decreased");
                txtCode.Clear();
            }
        }
    }
}
