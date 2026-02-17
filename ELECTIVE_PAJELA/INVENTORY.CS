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
                cm = new SqlCommand("SELECT * FROM PRODUCTS WHERE Product_ID = @id", cn);
                cm.Parameters.AddWithValue("@id", txtSearchID.Text);
                SqlDataReader dr = cm.ExecuteReader();

                if (dr.Read())
                {
                    productid_txtbox.Text = dr["Product_ID"].ToString();
                    txtNameTxtbox.Text = dr["Product_Name"].ToString();
                    txtPriceTxtbox.Text = dr["Product_Price"].ToString();
                    txtStocksTxtbox.Text = dr["Stock_Quantity"].ToString();
                    barcodeTxt.Text = dr["Barcode"].ToString();

                    // Load Image if path exists
                    if (dr["PicPath"] != DBNull.Value)
                        productPicturebox.ImageLocation = dr["PicPath"].ToString();
                }
                else
                {
                    MessageBox.Show("Record not found.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                dr.Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Update this product?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cm = new SqlCommand("UPDATE PRODUCTS SET Product_Name=@name, Product_Price=@price, Stock_Quantity=@qty, Barcode=@barcode WHERE Product_ID=@id", cn);
                    cm.Parameters.AddWithValue("@name", txtNameTxtbox.Text);
                    cm.Parameters.AddWithValue("@price", decimal.Parse(txtPriceTxtbox.Text));
                    cm.Parameters.AddWithValue("@qty", int.Parse(txtStocksTxtbox.Text));
                    cm.Parameters.AddWithValue("@barcode", barcodeTxt.Text);
                    cm.Parameters.AddWithValue("@id", productid_txtbox.Text);
                    cm.ExecuteNonQuery();

                    MessageBox.Show("Product updated successfully!");
                    LoadRecords();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void LoadRecords()
        {
            // TODO: Implement loading records from the database and updating the UI.
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to delete this product?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    cm = new SqlCommand("DELETE FROM PRODUCTS WHERE Product_ID = @id", cn);
                    cm.Parameters.AddWithValue("@id", productid_txtbox.Text);
                    cm.ExecuteNonQuery();

                    MessageBox.Show("Product deleted.");
                    ClearFields();
                    LoadRecords();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
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
            // Let the user pick an image file.
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select an Image";
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                openFileDialog.Multiselect = false;

                // If a file was chosen, load it into the PictureBox.
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Image selectedImage = Image.FromFile(openFileDialog.FileName);
                    productPicturebox.Image = selectedImage;
                    productPicturebox.SizeMode = PictureBoxSizeMode.StretchImage; // Fit image to box.
                    // Optional: store path for DB
                    productpicpathTextbox.Text = openFileDialog.FileName;
                }
            }
        } // Browse button
    }
    }
}

