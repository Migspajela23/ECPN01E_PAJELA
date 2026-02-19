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
        }

        // Initialize the DataTable to hold scanned products (temporary, not saved to DB)
        private void InitializeScannedProductsTable()
        {
            scannedProductsTable = new DataTable();
            scannedProductsTable.Columns.Add("Product_Name", typeof(string));
            scannedProductsTable.Columns.Add("Product_Price", typeof(decimal));
            scannedProductsTable.Columns.Add("Stock_Quantity", typeof(int));
            scannedProductsTable.Columns.Add("Scan_Count", typeof(int)); // Track how many times scanned

            dataGridView1.DataSource = scannedProductsTable.DefaultView;
        }

        private void textBox8_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(textBox8.Text))
            {
                string barcode = textBox8.Text.Trim();
                AddOrIncrementProductByBarcode(barcode);
                textBox8.Clear();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void AddOrIncrementProductByBarcode(string barcode)
        {
            // Check if barcode already exists in the DataTable
            DataRow[] foundRows = scannedProductsTable.Select($"Barcode = '{barcode.Replace("'", "''")}'");
            if (foundRows.Length > 0)
            {
                // Increment the scan count
                foundRows[0]["Scan_Count"] = Convert.ToInt32(foundRows[0]["Scan_Count"]) + 1;
                scannedProductsTable.AcceptChanges();

                // Update textboxes with existing row data
                txtProductName.Text = foundRows[0]["Product_Name"].ToString();
                txtProductPrice.Text = foundRows[0]["Product_Price"].ToString();
                txtProductQuantity.Text = foundRows[0]["Stock_Quantity"].ToString();
            }
            else
            {
                // Load product from database and add to DataTable (temporary, not saved to DB)
                try
                {
                    using (SqlConnection cn = new SqlConnection(_connectionString))
                    {
                        cn.Open();
                        string sql = @"SELECT Product_Name, Product_Price, Stock_Quantity, Barcode
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
                                    newRow["Scan_Count"] = 1;
                                    scannedProductsTable.Rows.Add(newRow);
                                    scannedProductsTable.AcceptChanges();

                                    // Update textboxes with new data
                                    txtProductName.Text = dr["Product_Name"].ToString();
                                    txtProductPrice.Text = dr["Product_Price"].ToString();
                                    txtProductQuantity.Text = dr["Stock_Quantity"].ToString();
                                }
                                else
                                {
                                    MessageBox.Show("Product not found for barcode: " + barcode, "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    txtProductName.Clear();
                                    txtProductPrice.Clear();
                                    txtProductQuantity.Clear();
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
                }
            }
        }
    }
}
