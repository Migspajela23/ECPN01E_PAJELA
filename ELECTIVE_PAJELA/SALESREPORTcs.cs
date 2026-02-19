using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Drawing; // <-- Add this using directive for Color

namespace ELECTIVE_PAJELA
{
    public partial class SALESREPORTcs : Form
    {
        private string _connectionString = @"Data Source=LAPTOP-RF7MTOVT\SQLEXPRESS;Initial Catalog=InventoryDB;Integrated Security=True;TrustServerCertificate=True";

        public SALESREPORTcs()
        {
            InitializeComponent();
            SetControlGreenGrey(this); // <-- Apply color scheme to all controls
            LoadSalesData();
        }

        private void LoadSalesData(string filter = "")
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connectionString))
                {
                    cn.Open();

                    // FIX: Changed FROM TRANSACTIONS → FROM SALES_REPORT
                    string sql = "SELECT * FROM SALES_REPORT";

                    if (!string.IsNullOrEmpty(filter))
                        sql += " WHERE Transaction_ID = @tid";

                    sql += " ORDER BY Sale_Date DESC";

                    using (SqlCommand cm = new SqlCommand(sql, cn))
                    {
                        if (!string.IsNullOrEmpty(filter))
                            cm.Parameters.AddWithValue("@tid", filter);

                        SqlDataAdapter da = new SqlDataAdapter(cm);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        dataGridView1.DataSource = dt;
                        UpdateSummary(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void UpdateSummary(DataTable dt)
        {
            decimal totalSales = 0;
            int totalItems = 0;

            foreach (DataRow row in dt.Rows)
            {
                totalSales += Convert.ToDecimal(row["Total_Amount"]);
                // FIX: Changed Quantity_Sold → Quantity to match your table column name
                totalItems += Convert.ToInt32(row["Quantity"]);
            }

            // Optional: display these somewhere, e.g.:
            // lblTotalSales.Text = totalSales.ToString("N2");
            // lblTotalItems.Text = totalItems.ToString();
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSearchID.Text))
                LoadSalesData(txtSearchID.Text.Trim());
        }

        private void buttonShowAll_Click(object sender, EventArgs e)
        {
            txtSearchID.Clear();
            LoadSalesData();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string transactionId = txtSearchID.Text.Trim();
            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                try
                {
                    using (SqlConnection cn = new SqlConnection(_connectionString))
                    {
                        cn.Open();
                        string sql = "SELECT * FROM SALES_REPORT WHERE Transaction_ID = @tid ORDER BY Sale_Date DESC";
                        using (SqlCommand cm = new SqlCommand(sql, cn))
                        {
                            cm.Parameters.AddWithValue("@tid", transactionId);
                            SqlDataAdapter da = new SqlDataAdapter(cm);
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            dataGridView1.DataSource = dt;
                            UpdateSummary(dt);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please enter a Transaction ID to search.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string transactionId = txtSearchID.Text.Trim();
            try
            {
                using (SqlConnection cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    string sql;
                    SqlCommand cm;

                    if (!string.IsNullOrWhiteSpace(transactionId))
                    {
                        sql = "SELECT * FROM SALES_REPORT WHERE Transaction_ID = @tid ORDER BY Sale_Date DESC";
                        cm = new SqlCommand(sql, cn);
                        cm.Parameters.AddWithValue("@tid", transactionId);
                    }
                    else
                    {
                        sql = "SELECT * FROM SALES_REPORT ORDER BY Sale_Date DESC";
                        cm = new SqlCommand(sql, cn);
                    }

                    SqlDataAdapter da = new SqlDataAdapter(cm);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dataGridView1.DataSource = dt;
                    UpdateSummary(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }

        }

        // Call this method in your SALESREPORTcs constructor after InitializeComponent()
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

        private void button3_Click(object sender, EventArgs e)
        {
            string transactionId = txtSearchID.Text.Trim();
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                MessageBox.Show("Please enter a Transaction ID to delete.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete records with Transaction ID: {transactionId}?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection cn = new SqlConnection(_connectionString))
                    {
                        cn.Open();
                        string sql = "DELETE FROM SALES_REPORT WHERE Transaction_ID = @tid";
                        using (SqlCommand cm = new SqlCommand(sql, cn))
                        {
                            cm.Parameters.AddWithValue("@tid", transactionId);
                            int rowsAffected = cm.ExecuteNonQuery();
                            if (rowsAffected > 0)
                                MessageBox.Show("Record(s) deleted successfully.");
                            else
                                MessageBox.Show("No records found for the specified Transaction ID.");
                        }
                    }
                    // Refresh the grid after delete
                    LoadSalesData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }
    }
}
    
