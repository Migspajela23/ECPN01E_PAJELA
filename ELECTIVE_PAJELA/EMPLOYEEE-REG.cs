using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace ELECTIVE_PAJELA
{
    public partial class EMPLOYEEE_REG : Form
    {
        // Instantiate your class to get access to the Connection String and Search Method
        EMPLOYEEE_REGDB empDB = new EMPLOYEEE_REGDB();
        string picPath = "";
        private string connectionString;

        public EMPLOYEEE_REG()
        {
            InitializeComponent();

            // Set the Sage Green-Grey Background
            this.BackColor = Color.FromArgb(120, 134, 107);

            // Populate the ComboBoxes with options
            InitializeComboBoxes();

            // Make labels and groupboxes transparent to show the background color
            foreach (Control c in this.Controls)
            {
                if (c is Label || c is GroupBox)
                {
                    c.BackColor = Color.Transparent;
                }
            }
        }

        // Method to setup the ComboBox "Picks"
        private void InitializeComboBoxes()
        {
            try
            {
                // Test popup
                MessageBox.Show("Populating ComboBoxes now...");

                cmbGender.Items.Clear();
                cmbGender.Items.AddRange(new string[] { "Male", "Female", "Other" });

                cmbStatus.Items.Clear();
                cmbStatus.Items.AddRange(new string[] { "Single", "Married" });

                cmbNationality.Items.Clear();
                cmbNationality.Items.AddRange(new string[] { "Filipino", "Others" });

                // This ensures the first item isn't blank
                cmbGender.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing boxes: " + ex.Message);
            }
        }

        private void EMPLOYEEE_REG_Load(object sender, EventArgs e)
        {
            LoadData();
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading data: " + ex.Message);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(empDB.MyConnection()))
            {
                try
                {
                    string query = @"INSERT INTO Employees (EmployeeID, Surname, FirstName, MiddleName, Address, 
                                     ContactNumber, DateOfBirth, Gender, Age, Nationality, Status, Religion, 
                                     EmailAddress, Department, Position, DateOfHired, PhotoPath) 
                                     VALUES (@id, @surname, @firstname, @middlename, @address, @contact, 
                                     @dob, @gender, @age, @nationality, @status, @religion, @email, 
                                     @dept, @position, @doh, @photo)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SetParameters(cmd);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Employee saved successfully!");
                    ClearFields();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving data: " + ex.Message);
                }
            }
        }

        // UPDATE BUTTON
        private void button2_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(empDB.MyConnection()))
            {
                try
                {
                    string query = @"UPDATE Employees SET Surname=@surname, FirstName=@firstname, MiddleName=@middlename, 
                                     Address=@address, ContactNumber=@contact, DateOfBirth=@dob, Gender=@gender, 
                                     Age=@age, Nationality=@nationality, Status=@status, Religion=@religion, 
                                     EmailAddress=@email, Department=@dept, Position=@position, 
                                     DateOfHired=@doh, PhotoPath=@photo WHERE EmployeeID=@id";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SetParameters(cmd);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Employee updated successfully!");
                    ClearFields();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating data: " + ex.Message);
                }
            }
        }

        // DELETE BUTTON
        private void button3_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this employee?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                using (SqlConnection conn = new SqlConnection(empDB.MyConnection()))
                {
                    try
                    {
                        string query = "DELETE FROM Employees WHERE EmployeeID=@id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", txtEmployeeID.Text);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Employee deleted successfully!");
                        ClearFields();
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting data: " + ex.Message);
                    }
                }
            }
        }

        // SEARCH BUTTON
        private void button5_Click(object sender, EventArgs e)
        {
        
            try
            {
                // Use the text from the dedicated Search Employee box
                string searchID = searchtxtbox.Text.Trim();

                if (string.IsNullOrEmpty(searchID))
                {
                    MessageBox.Show("Please enter an Employee ID to search.");
                    return;
                }

                EMPLOYEEE_REGDB foundEmployee = empDB.GetEmployeeByID(searchID);

                if (foundEmployee != null)
                {
                    // Map the database fields to your UI controls
                    txtEmployeeID.Text = foundEmployee.EmployeeID; // Fill the ID field too
                    txtSurname.Text = foundEmployee.Surname;
                    txtFirstName.Text = foundEmployee.FirstName;
                    txtMiddleName.Text = foundEmployee.MiddleName;
                    txtAddress.Text = foundEmployee.Address;
                    txtContactNumber.Text = foundEmployee.ContactNumber;
                    dtpDateOfBirth.Value = foundEmployee.DateOfBirth;
                    cmbGender.Text = foundEmployee.Gender;
                    txtAge.Text = foundEmployee.Age.ToString();
                    cmbNationality.Text = foundEmployee.Nationality;
                    cmbStatus.Text = foundEmployee.Status;
                    txtReligion.Text = foundEmployee.Religion;
                    txtEmailAddress.Text = foundEmployee.EmailAddress;
                    txtDepartment.Text = foundEmployee.Department;
                    txtPosition.Text = foundEmployee.Position;
                    dtpDateOfHired.Value = foundEmployee.DateOfHired;

                    picPath = foundEmployee.PhotoPath;
                    if (!string.IsNullOrEmpty(picPath) && File.Exists(picPath))
                    {
                        pictureBox1.Image = Image.FromFile(picPath);
                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                    }
                    else { pictureBox1.Image = null; }
                }
                else
                {
                    MessageBox.Show("Employee ID " + searchID + " not found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search Error: " + ex.Message);
            }
        }
        

        // BROWSE BUTTON
        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files(*.jpg; *.jpeg; *.png)|*.jpg; *.jpeg; *.png";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                picPath = ofd.FileName;
                pictureBox1.Image = new Bitmap(picPath);
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
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
            cmd.Parameters.AddWithValue("@photo", picPath);
        }

        private void ClearFields()
        {
            txtEmployeeID.Text = string.Empty;
            txtSurname.Clear();
            txtFirstName.Clear();
            txtMiddleName.Clear();
            txtAddress.Clear();
            txtContactNumber.Clear();
            dtpDateOfBirth.Value = DateTime.Now;
            cmbGender.SelectedIndex = -1;
            txtAge.Clear();
            cmbNationality.SelectedIndex = -1;
            cmbStatus.SelectedIndex = -1;
            txtReligion.Clear();
            txtEmailAddress.Clear();
            txtDepartment.Clear();
            txtPosition.Clear();
            dtpDateOfHired.Value = DateTime.Now;
            pictureBox1.Image = null;
            picPath = "";
            txtEmployeeID.Focus();
        }

        // EXIT BUTTON
        private void button6_Click(object sender, EventArgs e) { Application.Exit(); }

        // CLEAR BUTTON
        private void button4_Click(object sender, EventArgs e) { ClearFields(); }

        private void dgvEmployees_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                txtEmployeeID.Text = dgvEmployees.Rows[e.RowIndex].Cells["EmployeeID"].Value.ToString();
                button5_Click(sender, e);
            }
        }

        private void GENERATEIDBTN_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(empDB.MyConnection()))
                {
                    conn.Open();

                    string query = @"SELECT MAX(CAST(EmployeeID AS BIGINT)) 
                             FROM Employees 
                             WHERE EmployeeID NOT LIKE '%[^0-9]%'"; 

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();

                        if (result != DBNull.Value && result != null)
                        {
                            long maxId = Convert.ToInt64(result);
                            txtEmployeeID.Text = (maxId + 1).ToString();
                        }
                        else
                        {
                            txtEmployeeID.Text = "10001";
                        }
                    }
                }
            }
            catch
            {
                txtEmployeeID.Text = "10001";
            }
        }
    }
}