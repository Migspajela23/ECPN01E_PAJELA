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
using libzkfpcsharp; // For the fingerprint SDK
using ZXing;

namespace ELECTIVE_PAJELA
{
    public partial class EMPLOYEE_REPORTS : Form
    {
      
        private string _connectionString = @"Data Source=LAPTOP-RF7MTOVT\SQLEXPRESS;Initial Catalog=EMPLOYEEE_REGDB;Integrated Security=True;TrustServerCertificate=True";
        // --- MISSING HARDWARE VARIABLES ---
        IntPtr mDevHandle = IntPtr.Zero;
        IntPtr mDBHandle = IntPtr.Zero;
        byte[] FPBuffer;
        int mWidth = 0;
        int mHeight = 0;
        bool bIsScanning = false;
        private bool deviceReady = false;
        public EMPLOYEE_REPORTS()
        {
            InitializeComponent();
        }
        private void EMPLOYEE_REPORTS_Load(object sender, EventArgs e)
        {
            // 1. Initialize the Hardware on Load
            InitScanner();
            LoadEmployeeData();
        }
        private void InitScanner()
        {
            int ret = zkfperrdef.ZKFP_ERR_OK;
            if ((ret = zkfp2.Init()) == zkfperrdef.ZKFP_ERR_OK)
            {
                int nCount = zkfp2.GetDeviceCount();
                if (nCount > 0)
                {
                    // Open device 0
                    mDevHandle = zkfp2.OpenDevice(0);
                    if (IntPtr.Zero != mDevHandle)
                    {
                        // Get device parameters
                        byte[] paramValue = new byte[4];
                        int size = 4;
                        zkfp2.GetParameters(mDevHandle, 1, paramValue, ref size);
                        zkfp2.ByteArray2Int(paramValue, ref mWidth);

                        size = 4;
                        zkfp2.GetParameters(mDevHandle, 2, paramValue, ref size);
                        zkfp2.ByteArray2Int(paramValue, ref mHeight);

                        FPBuffer = new byte[mWidth * mHeight];
                        deviceReady = true;
                    }
                }
                else
                {
                    zkfp2.Terminate();
                    MessageBox.Show("No fingerprint device connected.");
                }
            }
        }

        private void scanbtn_Click(object sender, EventArgs e)
        {
            if (!deviceReady)
            {
                MessageBox.Show("Fingerprint device not initialized.");
                return;
            }

            byte[] CapTmp = new byte[2048];
            int cbCapTmp = 2048;

            // Try to acquire fingerprint
            int ret = zkfp2.AcquireFingerprint(mDevHandle, FPBuffer, CapTmp, ref cbCapTmp);

            if (ret == zkfp.ZKFP_ERR_OK)
            {
                byte[] finalTemplate = new byte[cbCapTmp];
                Array.Copy(CapTmp, finalTemplate, cbCapTmp);
                IdentifyEmployee(finalTemplate);
                MessageBox.Show("Fingerprint scanned and searched.");
            }
            else
            {
                MessageBox.Show("Fingerprint scan failed. Error code: " + ret);
            }
        }

        private void IdentifyEmployee(byte[] template)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connectionString))
                {
                    cn.Open();

                    string sql = "SELECT EmployeeID FROM Employees WHERE txtFingerprintData = @fp";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.Add("@fp", SqlDbType.VarBinary).Value = template;

                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            txtSearchID.Text = result.ToString();
                            LoadEmployeeData(result.ToString());
                        }
                        else
                        {
                            MessageBox.Show("Employee not found.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        private void LoadEmployeeData(string filterID = "")
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connectionString))
                {
                    cn.Open();

                    string sql = @"
            SELECT 
                e.EmployeeID,
                e.FirstName + ' ' + e.Surname AS FullName,
                e.Department,
                a.LogDate,
                a.TimeIn,
                a.TimeOut
            FROM Attendance a
            INNER JOIN Employees e ON a.EmployeeID = e.EmployeeID
            ";

                    if (!string.IsNullOrEmpty(filterID))
                    {
                        sql += " WHERE e.EmployeeID = @eid";
                    }

                    sql += " ORDER BY a.LogDate DESC, a.TimeIn DESC";

                    using (SqlCommand cm = new SqlCommand(sql, cn))
                    {
                        if (!string.IsNullOrEmpty(filterID))
                            cm.Parameters.AddWithValue("@eid", filterID);

                        SqlDataAdapter da = new SqlDataAdapter(cm);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        dataGridView1.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading reports: " + ex.Message);
            }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            bIsScanning = false;
            if (mDevHandle != IntPtr.Zero)
            {
                zkfp2.CloseDevice(mDevHandle);
            }
            zkfp2.Terminate();
            base.OnFormClosing(e);
        }
        private void searchbtn_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSearchID.Text))
                LoadEmployeeData(txtSearchID.Text.Trim());
            else
                MessageBox.Show("Please enter an Employee ID.");
        }

        // SHOW ALL BUTTON
        private void btnShowAll_Click(object sender, EventArgs e)
        {
            txtSearchID.Clear();
            LoadEmployeeData();
        }

        private void deletebtn_Click(object sender, EventArgs e)
        {
            string empID = txtSearchID.Text.Trim();
            if (string.IsNullOrWhiteSpace(empID))
            {
                MessageBox.Show("Please enter the Employee ID you wish to delete.");
                return;
            }

            var confirm = MessageBox.Show($"Are you sure you want to PERMANENTLY delete Employee: {empID}?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection cn = new SqlConnection(_connectionString))
                    {
                        cn.Open();
                        string sql = "DELETE FROM Employees WHERE EmployeeID = @eid";
                        using (SqlCommand cm = new SqlCommand(sql, cn))
                        {
                            cm.Parameters.AddWithValue("@eid", empID);
                            int rows = cm.ExecuteNonQuery();

                            if (rows > 0)
                                MessageBox.Show("Employee deleted successfully.");
                            else
                                MessageBox.Show("Employee ID not found.");
                        }
                    }
                    LoadEmployeeData(); // Refresh the grid
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void showallbtn_Click(object sender, EventArgs e)
        {
            txtSearchID.Clear();
            LoadEmployeeData();
        }
        private void SetControlGreenGrey(Control ctrl)
        {
            // Custom colors based on your IPON.INC theme
            Color darkGreen = Color.FromArgb(0, 100, 0); // For the header bar
            Color softGrey = Color.FromArgb(220, 220, 220);

            if (ctrl is Button btn)
            {
                btn.BackColor = Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = Color.Gray;
            }
            else if (ctrl is DataGridView dgv)
            {
                dgv.BackgroundColor = Color.DarkGray;
                dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;
            }
            else if (ctrl is Panel && ctrl.Name == "panelHeader") // Assuming you have a top panel
            {
                ctrl.BackColor = darkGreen;
            }

            foreach (Control child in ctrl.Controls)
            {
                SetControlGreenGrey(child);
            }
        }
    }
}

