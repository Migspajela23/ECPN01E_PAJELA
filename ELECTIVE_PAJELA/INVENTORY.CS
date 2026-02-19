using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace ELECTIVE_PAJELA
{
    public partial class INVENTORY : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        inventoryDB dbcon = new inventoryDB();
        private string _connectionString = @"Data Source=LAPTOP-RF7MTOVT\SQLEXPRESS;Initial Catalog=InventoryDB;Integrated Security=True;TrustServerCertificate=True";

        public INVENTORY()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSearchID.Text)) return;

            try
            {
                using (SqlConnection cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    using (SqlCommand cm = new SqlCommand("SELECT * FROM PRODUCTS WHERE Product_ID = @id", cn))
                    {
                        cm.Parameters.AddWithValue("@id", txtSearchID.Text);
                        using (SqlDataReader dr = cm.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                productid_txtbox.Text = dr["Product_ID"].ToString();
                                txtNameTxtbox.Text = dr["Product_Name"].ToString();
                                txtPriceTxtbox.Text = dr["Product_Price"].ToString();
                                txtStocksTxtbox.Text = dr["Stock_Quantity"].ToString();
                                barcodeTxt.Text = dr["Barcode"].ToString();

                                // 1. Correctly handle the path
                                string path = dr["PicPath"] != DBNull.Value ? dr["PicPath"].ToString() : "";
                                productpicpathTextbox.Text = path;

                                // 2. Load the image with a UI Refresh
                                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                                {
                                    // Setting SizeMode ensures it fits the box you drew on the UI
                                    productPicturebox.SizeMode = PictureBoxSizeMode.StretchImage;
                                    productPicturebox.ImageLocation = path;
                                    productPicturebox.Load(); // Forces the image to load synchronously
                                }
                                else
                                {
                                    productPicturebox.Image = null;
                                    productPicturebox.ImageLocation = null;
                                }

                                // 3. Load the barcode image from the folder
                                string barcode = dr["Barcode"].ToString();
                                string barcodeFolder = @"C:\Users\Miguel Pajela\source\repos\ELECTIVE_PAJELA\barcode";
                                string barcodeImagePath = System.IO.Path.Combine(barcodeFolder, barcode + ".png");
                                if (System.IO.File.Exists(barcodeImagePath))
                                {
                                    pictureBox3.Image = Image.FromFile(barcodeImagePath);
                                }
                                else
                                {
                                    pictureBox3.Image = null;
                                }
                            }
                            else
                            {
                                MessageBox.Show("Record not found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Update this product?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    using (SqlConnection cn = new SqlConnection(_connectionString))
                    {
                        cn.Open();
                        using (SqlCommand cm = new SqlCommand("UPDATE PRODUCTS SET Product_Name=@name, Product_Price=@price, Stock_Quantity=@qty, Barcode=@barcode WHERE Product_ID=@id", cn))
                        {
                            cm.Parameters.AddWithValue("@name", txtNameTxtbox.Text);
                            cm.Parameters.AddWithValue("@price", decimal.Parse(txtPriceTxtbox.Text));
                            cm.Parameters.AddWithValue("@qty", int.Parse(txtStocksTxtbox.Text));
                            cm.Parameters.AddWithValue("@barcode", barcodeTxt.Text);
                            cm.Parameters.AddWithValue("@id", productid_txtbox.Text);
                            cm.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Product updated successfully!");
                    LoadRecords();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadRecords()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(MyConnection()))
                {
                    cn.Open();
                    string sql = @"SELECT p.Product_ID, c.Category_Name, p.Barcode, p.Product_Name, p.Product_Price, 
                                      p.Product_Description, p.Stock_Quantity, p.PicPath, p.Manufacture_Date, p.Expiration_Date
                               FROM PRODUCTS p
                               LEFT JOIN CATEGORY c ON p.Category_ID = c.Category_ID";
                    using (SqlDataAdapter da = new SqlDataAdapter(sql, cn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dataGridView1.DataSource = dt.DefaultView; // Use DefaultView for sorting/filtering
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading records: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productid_txtbox.Text))
                {
                    MessageBox.Show("Please select a product to delete.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("Are you sure you want to delete this product?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    using (SqlConnection cn = new SqlConnection(_connectionString))
                    {
                        cn.Open();
                        using (SqlCommand cm = new SqlCommand("DELETE FROM PRODUCTS WHERE Product_ID = @id", cn))
                        {
                            cm.Parameters.AddWithValue("@id", productid_txtbox.Text);
                            int rowsAffected = cm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Product deleted.");
                                ClearFields();
                                LoadRecords();
                            }
                            else
                            {
                                MessageBox.Show("No product found with the specified ID.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting product: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            productid_txtbox.Clear();
            txtNameTxtbox.Clear();
            txtPriceTxtbox.Clear();
            txtStocksTxtbox.Clear();
            barcodeTxt.Clear();
            productPicturebox.Image = null;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Close application?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string barcode = barcodeTxt.Text;
            if (string.IsNullOrEmpty(barcode))
            {
                MessageBox.Show("Please enter a product ID.");
                return;
            }
            try
            {
                var writer = new ZXing.BarcodeWriter
                {
                    Format = ZXing.BarcodeFormat.CODE_128,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Height = 50,
                        Width = 300
                    }
                };
                Bitmap bardcodeBitmap = writer.Write(barcode);
                pictureBox3.Image = bardcodeBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating barcode: " + ex.Message);
            }
        }

        public string GenerateUniqueBarcode()
        {
            const int maxAttempts = 20;
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    string candidate = GenerateNumericBarcodeWithChecksum();

                    // Check uniqueness in DB
                    using (var cmd = new SqlCommand("SELECT COUNT(1) FROM PRODUCTS WHERE Barcode = @bc;", cn))
                    {
                        cmd.Parameters.AddWithValue("@bc", candidate);

                        int count = (int)cmd.ExecuteScalar();
                        if (count == 0)
                        {
                            return candidate;
                        }
                    }
                }
            }
            throw new InvalidOperationException("Unable to generate a unique barcode after multiple attempts.");
        }

        private string GenerateNumericBarcodeWithChecksum()
        {
            // 1. Generate 12 random digits
            var rng = new Random();
            var digits = new char[12];
            for (int i = 0; i < digits.Length; i++)
            {
                digits[i] = (char)('0' + rng.Next(0, 10));
            }

            // 2. Compute Mod 10 Checksum (Luhn-like)
            int sum = 0;
            for (int i = digits.Length - 1, pos = 0; i >= 0; i--, pos++)
            {
                int d = digits[i] - '0';
                if (pos % 2 == 1) // Double every second digit
                {
                    d *= 2;
                    if (d > 9) d -= 9;
                }
                sum += d;
            }
            int checkDigit = (10 - (sum % 10)) % 10;

            // 3. Return 13-digit string
            return new string(digits) + checkDigit.ToString();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Set the path to the textbox
                    productpicpathTextbox.Text = openFileDialog.FileName;
                    // Load the image into the PictureBox immediately
                    productPicturebox.ImageLocation = openFileDialog.FileName;
                    productPicturebox.SizeMode = PictureBoxSizeMode.StretchImage;
                }
            }
        } // Browse button

        // --- FIXED: Added missing method and variable definitions for button2_Click ---

        // Dummy product class for demonstration. Replace with your actual product class.
        private class Product
        {
            public string Barcode { get; set; }
            public string ProductName { get; set; }
            public decimal Price { get; set; }
            public string Description { get; set; }
            public int Quantity { get; set; }
            public string PicPath { get; set; }
            public DateTime ManufactureDate { get; set; }
            public DateTime ExpirationDate { get; set; }
        }

        // These should be replaced with your actual data sources/UI controls.
        private string categoryName => categoryNameTxtbox?.Text ?? "DefaultCategory";
        private Product p => new Product
        {
            Barcode = barcodeTxt.Text,
            ProductName = txtNameTxtbox.Text,
            Price = decimal.TryParse(txtPriceTxtbox.Text, out var price) ? price : 0,
            Description = null,
            Quantity = int.TryParse(txtStocksTxtbox.Text, out var qty) ? qty : 0,
            PicPath = productpicpathTextbox.Text,
            ManufactureDate = DateTime.Now,
            ExpirationDate = DateTime.Now.AddYears(1)
        };

        private string MyConnection()
        {
            return _connectionString;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (SqlConnection cn = new SqlConnection(MyConnection()))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Get or Create Category using the value from categoryNameTxtbox
                        string categoryText = categoryNameTxtbox.Text.Trim();
                        if (string.IsNullOrEmpty(categoryText))
                        {
                            MessageBox.Show("Please enter a category.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        int categoryId;
                        string catSql = @"IF EXISTS (SELECT 1 FROM CATEGORY WHERE Category_Name = @cat)
                                    SELECT Category_ID FROM CATEGORY WHERE Category_Name = @cat
                                  ELSE
                                    BEGIN
                                        INSERT INTO CATEGORY (Category_Name) VALUES (@cat);
                                        SELECT SCOPE_IDENTITY();
                                    END";

                        using (SqlCommand cmd = new SqlCommand(catSql, cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@cat", categoryText);
                            categoryId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. Ensure barcode is present (generate if empty)
                        string barcode = barcodeTxt.Text.Trim();
                        if (string.IsNullOrEmpty(barcode))
                        {
                            barcode = GenerateUniqueBarcode();
                            barcodeTxt.Text = barcode;
                        }

                        // 3. Save barcode image to folder
                        string barcodeFolder = @"C:\Users\Miguel Pajela\source\repos\ELECTIVE_PAJELA\barcode";
                        if (!System.IO.Directory.Exists(barcodeFolder))
                            System.IO.Directory.CreateDirectory(barcodeFolder);

                        string barcodeImagePath = System.IO.Path.Combine(barcodeFolder, barcode + ".png");

                        var writer = new ZXing.BarcodeWriter
                        {
                            Format = ZXing.BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Height = 50,
                                Width = 300
                            }
                        };
                        using (Bitmap barcodeBitmap = writer.Write(barcode))
                        {
                            barcodeBitmap.Save(barcodeImagePath, System.Drawing.Imaging.ImageFormat.Png);
                        }

                        // 4. Insert Product (barcode and image path included)
                        string prodSql = @"INSERT INTO PRODUCTS (Category_ID, Barcode, Product_Name, Product_Price, 
                                   Product_Description, Stock_Quantity, PicPath, Manufacture_Date, Expiration_Date)
                                   VALUES (@cid, @bc, @name, @price, @desc, @qty, @path, @mfg, @exp)";

                        using (SqlCommand cmd = new SqlCommand(prodSql, cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@cid", categoryId);
                            cmd.Parameters.AddWithValue("@bc", barcode);
                            cmd.Parameters.AddWithValue("@name", txtNameTxtbox.Text);
                            cmd.Parameters.AddWithValue("@price", decimal.Parse(txtPriceTxtbox.Text));
                            cmd.Parameters.AddWithValue("@desc", DBNull.Value); // or your description field
                            cmd.Parameters.AddWithValue("@qty", int.Parse(txtStocksTxtbox.Text));
                            cmd.Parameters.AddWithValue("@path", (object)productpicpathTextbox.Text ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@mfg", DateTime.Now);
                            cmd.Parameters.AddWithValue("@exp", DateTime.Now.AddYears(1));
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        MessageBox.Show("Product and barcode saved successfully!", "Success");
                        LoadRecords();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        MessageBox.Show("Error adding product: " + ex.Message);
                    }
                }
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 1. Check if the user clicked a valid row (not the header)
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

                // 2. Map the data from the Grid to your Textboxes
                // Note: Check your SQL 'LoadRecords' method to ensure these column names match!
                productid_txtbox.Text = row.Cells["Product_ID"].Value.ToString();
                txtNameTxtbox.Text = row.Cells["Product_Name"].Value.ToString();
                txtPriceTxtbox.Text = row.Cells["Product_Price"].Value.ToString();
                txtStocksTxtbox.Text = row.Cells["Stock_Quantity"].Value.ToString();
                barcodeTxt.Text = row.Cells["Barcode"].Value.ToString();

                // Handle the Picture Path
                string path = row.Cells["PicPath"].Value != DBNull.Value ? row.Cells["PicPath"].Value.ToString() : "";
                productpicpathTextbox.Text = path;

                // 3. AUTOMATICALLY SHOW THE PRODUCT IMAGE
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    productPicturebox.SizeMode = PictureBoxSizeMode.StretchImage;
                    productPicturebox.ImageLocation = path;
                    productPicturebox.Load(); // Force the image to appear
                }
                else
                {
                    productPicturebox.Image = null; // Clear if no image found
                }

                // 4. AUTOMATICALLY SHOW THE BARCODE IMAGE
                // We call the button6_Click method manually to generate the barcode preview
                if (!string.IsNullOrEmpty(barcodeTxt.Text))
                {
                    button6_Click(null, null);
                }
                else
                {
                    pictureBox3.Image = null; // Clear barcode if empty
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Set form background to dark grey, foreground (text) to white
            this.BackColor = Color.FromArgb(40, 40, 40); // dark grey
            this.ForeColor = Color.White;

            // Apply green/grey theme to all controls
            foreach (Control ctrl in this.Controls)
            {
                SetControlGreenGrey(ctrl);
            }
        }

        private void SetControlGreenGrey(Control ctrl)
        {
            Color darkGrey = Color.FromArgb(40, 40, 40);
            Color lightGrey = Color.FromArgb(200, 200, 200);
            Color accentGreen = Color.FromArgb(0, 192, 0);

            if (ctrl is Button btn)
            {
                btn.BackColor = accentGreen;
                btn.ForeColor = Color.White;
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
                ctrl.ForeColor = Color.White;
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

        private void button8_Click(object sender, EventArgs e)
        {
            // Clear all input fields
            productid_txtbox.Clear();
            txtNameTxtbox.Clear();
            txtPriceTxtbox.Clear();
            txtStocksTxtbox.Clear();
            barcodeTxt.Clear();
            productpicpathTextbox.Clear();
            txtSearchID.Clear();
            categoryNameTxtbox.Clear(); // Clear the category textbox

            // Clear image previews
            productPicturebox.Image = null;
            productPicturebox.ImageLocation = null;
            pictureBox3.Image = null; // Clear the barcode picture box

            // Optionally, clear DataGridView selection
            if (dataGridView1.DataSource != null)
            {
                dataGridView1.ClearSelection();
            }
        }

        
    }
}


