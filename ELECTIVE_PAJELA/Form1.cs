using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ELECTIVE_PAJELA
{
    public partial class Form1 : Form
    {
        private string _connectionString = @"Data Source=LAPTOP-RF7MTOVT\SQLEXPRESS;Initial Catalog=InventoryDB;Integrated Security=True;TrustServerCertificate=True";
        private DataTable scannedProductsTable;

        public Form1()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = true;
            InitializeScannedProductsTable();
            SetControlGreenGrey(this); // <-- Apply UI color scheme to all controls
        }

        // Initialize the DataTable to hold scanned products (temporary, not saved to DB)
        private void InitializeScannedProductsTable()
        {
            scannedProductsTable = new DataTable();
            scannedProductsTable.Columns.Add("Barcode", typeof(string)); // <-- Add this line
            scannedProductsTable.Columns.Add("Product_Name", typeof(string));
            scannedProductsTable.Columns.Add("Product_Price", typeof(decimal));
            scannedProductsTable.Columns.Add("Stock_Quantity", typeof(int));
            scannedProductsTable.Columns.Add("PicPath", typeof(string)); // <-- Add this line
            scannedProductsTable.Columns.Add("Scan_Count", typeof(int)); // Track how many times scanned
            scannedProductsTable.Columns.Add("Discount_Amount", typeof(decimal)); // <-- Added column

            dataGridView1.DataSource = scannedProductsTable.DefaultView;
        }

        private void textBox8_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtProductID.Text))
            {
                string barcode = txtProductID.Text.Trim();
                AddOrIncrementProductByBarcode(barcode);
                txtProductID.Clear();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void AddOrIncrementProductByBarcode(string barcode)
        {
            DataRow[] foundRows = scannedProductsTable.Select($"Barcode = '{barcode.Replace("'", "''")}'");
            if (foundRows.Length > 0)
            {
                foundRows[0]["Scan_Count"] = Convert.ToInt32(foundRows[0]["Scan_Count"]) + 1;
                scannedProductsTable.AcceptChanges();

                txtProductName.Text = foundRows[0]["Product_Name"].ToString();
                txtProductPrice.Text = foundRows[0]["Product_Price"].ToString();
                txtProductQuantity.Text = foundRows[0]["Stock_Quantity"].ToString();

                // Show image from PicPath
                ShowProductImage(foundRows[0]["PicPath"]?.ToString());
            }
            else
            {
                try
                {
                    using (SqlConnection cn = new SqlConnection(_connectionString))
                    {
                        cn.Open();
                        string sql = @"SELECT Product_Name, Product_Price, Stock_Quantity, Barcode, PicPath
                                       FROM PRODUCTS
                                       WHERE Barcode = @barcode";
                        using (SqlCommand cmd = new SqlCommand(sql, cn))
                        {
                            cmd.Parameters.AddWithValue("@barcode", barcode);
                            using (SqlDataReader dr = cmd.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    DataRow newRow = scannedProductsTable.NewRow();
                                    newRow["Product_Name"] = dr["Product_Name"];
                                    newRow["Product_Price"] = dr["Product_Price"];
                                    newRow["Stock_Quantity"] = dr["Stock_Quantity"];
                                    newRow["Barcode"] = dr["Barcode"];
                                    newRow["PicPath"] = dr["PicPath"];
                                    newRow["Scan_Count"] = 1;
                                    scannedProductsTable.Rows.Add(newRow);
                                    scannedProductsTable.AcceptChanges();

                                    txtProductName.Text = dr["Product_Name"].ToString();
                                    txtProductPrice.Text = dr["Product_Price"].ToString();
                                    txtProductQuantity.Text = dr["Stock_Quantity"].ToString();

                                    // Show image from PicPath
                                    ShowProductImage(dr["PicPath"] != DBNull.Value ? dr["PicPath"].ToString() : null);
                                }
                                else
                                {
                                    MessageBox.Show("Product not found for barcode: " + barcode, "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    txtProductName.Clear();
                                    txtProductPrice.Clear();
                                    txtProductQuantity.Clear();
                                    pictureBox1.Image = null;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading product: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtProductName.Clear();
                    txtProductPrice.Clear();
                    txtProductQuantity.Clear();
                    pictureBox1.Image = null;
                }
            }
        }

        // Helper method to show the product image in pictureBox1
        private void ShowProductImage(string picPath)
        {
            if (!string.IsNullOrEmpty(picPath) && System.IO.File.Exists(picPath))
            {
                try
                {
                    if (pictureBox1.Image != null)
                    {
                        var oldImage = pictureBox1.Image;
                        pictureBox1.Image = null;
                        oldImage.Dispose();
                    }
                    pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                    pictureBox1.Image = Image.FromFile(picPath);
                }
                catch
                {
                    pictureBox1.Image = null;
                }
            }
            else
            {
                pictureBox1.Image = null;
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Basic Math Logic
                decimal price = decimal.Parse(txtProductPrice.Text);
                int qty = int.Parse(txtProductQuantity.Text);
                decimal subtotal = price * qty;
                decimal discount = 0;

                // Apply Discount Logic (Senior/PWD = 20%)
                if (radioSenior.Checked || radioPWD.Checked)
                {
                    discount = subtotal * 0.20m;
                }

                decimal totalAmount = subtotal - discount;
                decimal cashGiven = decimal.Parse(txtCashGiven.Text);
                decimal change = cashGiven - totalAmount;

                // Display results in UI
                txtDiscount.Text = discount.ToString("N2");
                txtTotalAmount.Text = totalAmount.ToString("N2");
                txtChange.Text = change.ToString("N2");

                // --- SET Discount_Amount in the DataTable for the current product ---
                DataRow[] foundRows = scannedProductsTable.Select($"Barcode = '{txtProductID.Text.Replace("'", "''")}'");
                if (foundRows.Length > 0)
                {
                    foundRows[0]["Discount_Amount"] = discount;
                    scannedProductsTable.AcceptChanges();
                }

                if (change < 0)
                {
                    MessageBox.Show("Insufficient Cash!");
                    return;
                }

                // 2. Save to Database (Direct to Sales Report)
                using (SqlConnection cn = new SqlConnection(_connectionString))
                {
                    cn.Open();

                    // Save all scanned products as separate sales report entries
                    foreach (DataRow row in scannedProductsTable.Rows)
                    {
                        string sql = @"INSERT INTO SALES_REPORT 
                            (Product_ID, Product_Name, Price, Quantity, Discount_Amount, Total_Amount) 
                            VALUES (@pid, @name, @price, @qty, @disc, @total)";

                        using (SqlCommand cm = new SqlCommand(sql, cn))
                        {
                            cm.Parameters.AddWithValue("@pid", row["Barcode"]);
                            cm.Parameters.AddWithValue("@name", row["Product_Name"]);
                            cm.Parameters.AddWithValue("@price", row["Product_Price"]);
                            cm.Parameters.AddWithValue("@qty", row["Scan_Count"]);
                            cm.Parameters.AddWithValue("@disc", row["Discount_Amount"]);
                            // Calculate total for each product
                            decimal rowSubtotal = Convert.ToDecimal(row["Product_Price"]) * Convert.ToInt32(row["Scan_Count"]);
                            decimal rowDiscount = row["Discount_Amount"] != DBNull.Value ? Convert.ToDecimal(row["Discount_Amount"]) : 0;
                            decimal rowTotal = rowSubtotal - rowDiscount;
                            cm.Parameters.AddWithValue("@total", rowTotal);

                            cm.ExecuteNonQuery();
                        }

                        // Subtract stock for each product
                        string updateStock = "UPDATE PRODUCTS SET Stock_Quantity = Stock_Quantity - @qty WHERE Barcode = @pid";
                        using (SqlCommand cm2 = new SqlCommand(updateStock, cn))
                        {
                            cm2.Parameters.AddWithValue("@qty", row["Scan_Count"]);
                            cm2.Parameters.AddWithValue("@pid", row["Barcode"]);
                            cm2.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Transaction Successful! Records updated in Sales Report.");

                // Optionally clear scanned products after transaction
                scannedProductsTable.Clear();
                txtProductID.Clear();
                txtProductName.Clear();
                txtProductPrice.Clear();
                txtProductQuantity.Clear();
                txtDiscount.Clear();
                txtTotalAmount.Clear();
                txtCashGiven.Clear();
                txtChange.Clear();
                pictureBox1.Image = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            // Clears all textboxes
            txtProductName.Clear();
            txtProductPrice.Clear();
            txtProductQuantity.Clear();
            txtProductID.Clear();
            txtCashGiven.Clear();
            txtChange.Clear();
            txtDiscount.Clear();
            txtTotalAmount.Clear();
            radioSenior.Checked = false;
            radioPWD.Checked = false;
            pictureBox1.Image = null; // Clears the product image
                                      // Reset Controls
            radioSenior.Checked = false;
            radioPWD.Checked = false;
            pictureBox1.Image = null;

            // Clear the DataGrid Table
            scannedProductsTable.Clear();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Place your SetControlGreenGrey method in Form1.cs
        private void SetControlGreenGrey(Control ctrl)
        {
            Color darkGrey = Color.FromArgb(40, 40, 40);
            Color lightGrey = Color.FromArgb(200, 200, 200);
            Color accentGreen = Color.FromArgb(0, 192, 0);

            if (ctrl is Button btn)
            {
                btn.BackColor = accentGreen;
                btn.ForeColor = Color.Black;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = lightGrey;
            }
            else if (ctrl is TextBox || ctrl is MaskedTextBox)
            {
                ctrl.BackColor = lightGrey;
                ctrl.ForeColor = Color.Black;
                (ctrl as TextBoxBase).BorderStyle = BorderStyle.FixedSingle;
            }
            else if (ctrl is DataGridView dgv)
            {
                dgv.BackgroundColor = darkGrey;
                dgv.DefaultCellStyle.BackColor = lightGrey;
                dgv.DefaultCellStyle.ForeColor = Color.Black;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = accentGreen;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv.EnableHeadersVisualStyles = false;
                dgv.GridColor = accentGreen;
            }
            else if (ctrl is Label)
            {
                ctrl.ForeColor = Color.Black;
                ctrl.BackColor = Color.Transparent;
            }
            else if (ctrl is ComboBox)
            {
                ctrl.BackColor = lightGrey;
                ctrl.ForeColor = Color.Black;
            }
            else if (ctrl is PictureBox)
            {
                ctrl.BackColor = darkGrey;
            }

            // Recursively set for child controls (e.g., panels, groupboxes)
            foreach (Control child in ctrl.Controls)
            {
                SetControlGreenGrey(child);
            }
        }
    }
}


