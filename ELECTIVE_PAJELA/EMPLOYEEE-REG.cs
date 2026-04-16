using libzkfpcsharp;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ELECTIVE_PAJELA
{
    public partial class EMPLOYEEE_REG : Form
    {
        EMPLOYEEE_REGDB empDB = new EMPLOYEEE_REGDB();
        string picPath = "";

        // Fingerprint device handle and buffers
        private IntPtr mDevHandle = IntPtr.Zero;
        private byte[] FPBuffer;
        private byte[] CapTmp = new byte[2048];
        private int cbCapTmp = 2048;
        private int mWidth = 0;
        private int mHeight = 0;

        private bool isFingerprintScanned = false;
        private string connectionString;
        private string _connectionString;

        public EMPLOYEEE_REG()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(120, 134, 107);
            InitializeComboBoxes();

            foreach (Control c in this.Controls)
            {
                if (c is Label || c is GroupBox) c.BackColor = Color.Transparent;
            }
        }

        private void InitializeComboBoxes()
        {
            cmbGender.Items.AddRange(new string[] { "Male", "Female", "Other" });
            cmbStatus.Items.AddRange(new string[] { "Single", "Married" });
            cmbNationality.Items.AddRange(new string[] { "Filipino", "Others" });
            cmbGender.SelectedIndex = -1;
        }

        private void EMPLOYEEE_REG_Load(object sender, EventArgs e)
        {
            LoadData();
            InitFingerprintDevice();
        }

        private void LoadData()
        {
            using (SqlConnection conn = new SqlConnection(empDB.MyConnection()))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM Employees";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgvEmployees.DataSource = dt;

                    // Hide binary/path columns from view
                    if (dgvEmployees.Columns.Contains("txtFingerprintData")) dgvEmployees.Columns["txtFingerprintData"].Visible = false;
                    if (dgvEmployees.Columns.Contains("picpath")) dgvEmployees.Columns["picpath"].Visible = false;
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        // SAVE BUTTON
        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFingerprintData.Text))
            {
                MessageBox.Show("Please scan and accept a fingerprint first.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(empDB.MyConnection()))
            {
                try
                {
                    // FIXED: Changed column name to 'Fingerprint' to match your SQL table
                    string query = @"INSERT INTO Employees (EmployeeID, Surname, FirstName, MiddleName, Address, 
                                     ContactNumber, DateOfBirth, Gender, Age, Nationality, Status, Religion, 
                                     EmailAddress, Department, Position, DateOfHired, txtFingerprintData, picpath) 
                                     VALUES (@id, @surname, @firstname, @middlename, @address, @contact, 
                                     @dob, @gender, @age, @nationality, @status, @religion, @email, 
                                     @dept, @position, @doh, @fingerprint, @photo)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SetParameters(cmd);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Employee saved successfully!");
                    ClearFields();
                    LoadData();
                }
                catch (Exception ex) { MessageBox.Show("Error saving: " + ex.Message); }
            }
        }

        // UPDATE BUTTON
        private void button2_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(empDB.MyConnection()))
            {
                try
                {
                    // FIXED: Changed column name to 'Fingerprint'
                    string query = @"UPDATE Employees SET 
                                     Surname=@surname, FirstName=@firstname, MiddleName=@middlename, 
                                     Address=@address, ContactNumber=@contact, DateOfBirth=@dob, 
                                     Gender=@gender, Age=@age, Nationality=@nationality, 
                                     Status=@status, Religion=@religion, EmailAddress=@email, 
                                     Department=@dept, Position=@position, DateOfHired=@doh, 
                                     txtFingerprintData=@fingerprint, picpath=@photo 
                                     WHERE EmployeeID=@id";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SetParameters(cmd);

                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0) MessageBox.Show("Updated successfully!");
                    LoadData();
                }
                catch (Exception ex) { MessageBox.Show("Error updating: " + ex.Message); }
            }
        }

        private void SetParameters(SqlCommand cmd)
        {
            cmd.Parameters.AddWithValue("@id", txtEmployeeID.Text);
            cmd.Parameters.AddWithValue("@surname", txtSurname.Text);
            cmd.Parameters.AddWithValue("@firstname", txtFirstName.Text);
            cmd.Parameters.AddWithValue("@middlename", txtMiddleName.Text);
            cmd.Parameters.AddWithValue("@address", txtAddress.Text);
            cmd.Parameters.AddWithValue("@contact", txtContactNumber.Text);
            cmd.Parameters.AddWithValue("@dob", dtpDateOfBirth.Value.Date);
            cmd.Parameters.AddWithValue("@gender", cmbGender.Text);

            int age = 0;
            int.TryParse(txtAge.Text, out age);
            cmd.Parameters.AddWithValue("@age", age);

            cmd.Parameters.AddWithValue("@nationality", cmbNationality.Text);
            cmd.Parameters.AddWithValue("@status", cmbStatus.Text);
            cmd.Parameters.AddWithValue("@religion", txtReligion.Text);
            cmd.Parameters.AddWithValue("@email", txtEmailAddress.Text);
            cmd.Parameters.AddWithValue("@dept", txtDepartment.Text);
            cmd.Parameters.AddWithValue("@position", txtPosition.Text);
            cmd.Parameters.AddWithValue("@doh", dtpDateOfHired.Value.Date);

            // CRITICAL FIX: Convert Base64 string to byte array for VARBINARY column
            if (!string.IsNullOrEmpty(txtFingerprintData.Text))
            {
                byte[] fpBytes = Convert.FromBase64String(txtFingerprintData.Text);
                cmd.Parameters.Add("@fingerprint", SqlDbType.VarBinary).Value = fpBytes;
            }
            else { cmd.Parameters.Add("@fingerprint", SqlDbType.VarBinary).Value = DBNull.Value; }

            // IMAGE FIX: Convert photo to byte array
            if (!string.IsNullOrEmpty(picPath) && File.Exists(picPath))
            {
                cmd.Parameters.Add("@photo", SqlDbType.VarBinary).Value = File.ReadAllBytes(picPath);
            }
            else { cmd.Parameters.Add("@photo", SqlDbType.VarBinary).Value = DBNull.Value; }
        }

        private void scanbtn_Click(object sender, EventArgs e)
        {
            if (mDevHandle == IntPtr.Zero || FPBuffer == null) return;

            lblStatus.Text = "Status: Scanning...";
            int ret = zkfp2.AcquireFingerprint(mDevHandle, FPBuffer, CapTmp, ref cbCapTmp);

            if (ret == zkfp.ZKFP_ERR_OK)
            {
                using (MemoryStream ms = RawToBitmapStream(FPBuffer, mWidth, mHeight))
                {
                    pbFingerprint.Image = new Bitmap(ms);
                }
                btnAccept.Enabled = true;
                lblStatus.Text = "Status: Scan Successful!";
            }
            else { lblStatus.Text = "Status: Scan failed."; }
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            if (cbCapTmp > 0 && CapTmp[0] != 0)
            {
                txtFingerprintData.Text = zkfp2.BlobToBase64(CapTmp, cbCapTmp);
                MessageBox.Show("Fingerprint accepted.");
                button1.BackColor = Color.LightGreen;
            }
        }

        private void InitFingerprintDevice()
        {
            if (zkfp2.Init() == zkfperrdef.ZKFP_ERR_OK)
            {
                if (zkfp2.GetDeviceCount() > 0)
                {
                    mDevHandle = zkfp2.OpenDevice(0);
                    if (mDevHandle != IntPtr.Zero)
                    {
                        byte[] paramValue = new byte[4];
                        int size = 4;
                        zkfp2.GetParameters(mDevHandle, 1, paramValue, ref size);
                        zkfp2.ByteArray2Int(paramValue, ref mWidth);
                        size = 4;
                        zkfp2.GetParameters(mDevHandle, 2, paramValue, ref size);
                        zkfp2.ByteArray2Int(paramValue, ref mHeight);
                        FPBuffer = new byte[mWidth * mHeight];
                    }
                }
            }
        }

        private MemoryStream RawToBitmapStream(byte[] buffer, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            ColorPalette palette = bmp.Palette;
            for (int i = 0; i < 256; i++) palette.Entries[i] = Color.FromArgb(i, i, i);
            bmp.Palette = palette;

            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, width * height);
            bmp.UnlockBits(bmpData);

            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Bmp);
            ms.Position = 0;
            return ms;
        }

        private void button7_Click(object sender, EventArgs e) // Browse Photo
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Images|*.jpg;*.png" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                picPath = ofd.FileName;
                pictureBox1.Image = new Bitmap(picPath);
            }
        }

        private void ClearFields()
        {
            // Clear TextBoxes
            txtEmployeeID.Clear();
            txtSurname.Clear();
            txtFirstName.Clear();
            txtMiddleName.Clear();
            txtAddress.Clear();
            txtContactNumber.Clear();
            txtAge.Clear();
            txtReligion.Clear();
            txtEmailAddress.Clear();
            txtDepartment.Clear();
            txtPosition.Clear();
            searchtxtbox.Clear();
            txtFingerprintData.Clear(); // Hidden data field

            // Reset ComboBoxes
            cmbGender.SelectedIndex = -1;
            cmbNationality.SelectedIndex = -1;
            cmbStatus.SelectedIndex = -1;

            // Reset DatePickers to Current Date
            dtpDateOfBirth.Value = DateTime.Now;
            dtpDateOfHired.Value = DateTime.Now;

            // Clear Images
            if (pbFingerprint.Image != null)
            {
                pbFingerprint.Image.Dispose();
                pbFingerprint.Image = null;
            }
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }

            // Reset variables and visual cues
            picPath = "";
            button1.BackColor = SystemColors.Control; // Reset Save button color
            lblStatus.Text = "Status: Ready";
            btnAccept.Enabled = false;
        }

        private void button6_Click(object sender, EventArgs e) => Application.Exit();

        private void button5_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(searchtxtbox.Text))
            {
                MessageBox.Show("Please enter an Employee ID to search.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection conn = new SqlConnection(empDB.MyConnection()))
            {
                try
                {
                    // Query to find the employee by ID
                    string query = "SELECT * FROM Employees WHERE EmployeeID = @id";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", searchtxtbox.Text.Trim());

                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        // 1. Populate Text Fields
                        txtEmployeeID.Text = reader["EmployeeID"].ToString();
                        txtSurname.Text = reader["Surname"].ToString();
                        txtFirstName.Text = reader["FirstName"].ToString();
                        txtMiddleName.Text = reader["MiddleName"].ToString();
                        txtAddress.Text = reader["Address"].ToString();
                        txtContactNumber.Text = reader["ContactNumber"].ToString();
                        txtAge.Text = reader["Age"].ToString();
                        txtReligion.Text = reader["Religion"].ToString();
                        txtEmailAddress.Text = reader["EmailAddress"].ToString();
                        txtDepartment.Text = reader["Department"].ToString();
                        txtPosition.Text = reader["Position"].ToString();

                        // 2. Populate Dropdowns and Dates
                        cmbGender.Text = reader["Gender"].ToString();
                        cmbNationality.Text = reader["Nationality"].ToString();
                        cmbStatus.Text = reader["Status"].ToString();

                        if (reader["DateOfBirth"] != DBNull.Value)
                            dtpDateOfBirth.Value = Convert.ToDateTime(reader["DateOfBirth"]);

                        if (reader["DateOfHired"] != DBNull.Value)
                            dtpDateOfHired.Value = Convert.ToDateTime(reader["DateOfHired"]);

                        // 3. Display the Fingerprint Template String
                        txtFingerprintData.Text = reader["txtFingerprintData"].ToString();

                        // 4. Display the Profile Photo (picpath is VARBINARY)
                        if (reader["picpath"] != DBNull.Value)
                        {
                            byte[] imgData = (byte[])reader["picpath"];
                            using (MemoryStream ms = new MemoryStream(imgData))
                            {
                                pictureBox1.Image = Image.FromStream(ms);
                            }
                        }
                        else
                        {
                            pictureBox1.Image = null; // Clear if no photo exists
                        }

                        // Note: Fingerprint SDKs store a 'Template' (math), not the actual image.
                        // To show the fingerprint picture again, you would have needed to save 
                        // the raw bitmap bytes during the Save process.
                        pbFingerprint.Image = null;

                        MessageBox.Show("Employee record loaded.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Employee ID not found.", "Search Result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ClearFields();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error searching: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ClearFields();
            lblStatus.Text = "Status: Fields cleared.";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtEmployeeID.Text))
            {
                MessageBox.Show("Please select or search for an employee record to delete.", "ID Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Confirmation dialog to prevent accidental deletion
            DialogResult result = MessageBox.Show("Are you sure you want to delete Employee ID: " + txtEmployeeID.Text + "?",
                                                "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                using (SqlConnection conn = new SqlConnection(empDB.MyConnection()))
                {
                    try
                    {
                        string query = "DELETE FROM Employees WHERE EmployeeID = @id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", txtEmployeeID.Text);

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            MessageBox.Show("Employee record deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ClearFields();
                            LoadData(); // Refresh the DataGridView
                        }
                        else
                        {
                            MessageBox.Show("Record not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting record: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            ClearFields();
            lblStatus.Text = "Status: Fields cleared.";
        }

        private void GENERATEIDBTN_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Strictly enforce: only 5-digit IDs at or above 10001
                    string query = @"SELECT MAX(CAST(EmployeeID AS BIGINT)) 
                             FROM Employees 
                             WHERE ISNUMERIC(EmployeeID) = 1 
                             AND LEN(EmployeeID) = 5
                             AND CAST(EmployeeID AS BIGINT) >= 10001";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();

                        if (result == DBNull.Value || result == null)
                        {
                            // No valid 10001-format IDs found — start fresh
                            txtEmployeeID.Text = "10001";
                        }
                        else
                        {
                            long nextId = Convert.ToInt64(result) + 1;

                            // Safety format: always ensure it stays 5 digits (10001–99999)
                            if (nextId > 99999)
                            {
                                MessageBox.Show("Warning: Employee ID has exceeded the 5-digit limit (99999).");
                                txtEmployeeID.Text = "ERROR";
                                return;
                            }

                            txtEmployeeID.Text = nextId.ToString("D5"); // Always outputs 5-digit format
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Connection Error: " + ex.Message);
                txtEmployeeID.Text = "ERROR";
            }
        }
        private void retrybtn_Click(object sender, EventArgs e)
        {
            if(pbFingerprint.Image != null)
            {
                pbFingerprint.Image.Dispose();
                pbFingerprint.Image = null;
            }
            Array.Clear(CapTmp, 0, CapTmp.Length);
            cbCapTmp = 2048;
            txtFingerprintData.Clear(); // Clear the hidden data field too
            lblStatus.Text = "Please place your finger on the scanner again...";
            btnAccept.Enabled = false;
        }
    }
}
    
