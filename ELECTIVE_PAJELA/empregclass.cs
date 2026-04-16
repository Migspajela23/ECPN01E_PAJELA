using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libzkfpcsharp; // SDK Namespace

namespace ELECTIVE_PAJELA
{
    internal class EMPLOYEEE_REGDB
    {
        private string connectionString = @"Data Source=LAPTOP-RF7MTOVT\SQLEXPRESS;Initial Catalog=EMPLOYEEE_REGDB;Integrated Security=True;TrustServerCertificate=True";

        // --- ZK9500 Hardware Variables ---
        public IntPtr mDevHandle = IntPtr.Zero;
        public IntPtr mDBHandle = IntPtr.Zero;
        public byte[] FPBuffer; // Raw image buffer
        public byte[] CapTmp = new byte[2048]; // Fingerprint template buffer
        public int cbCapTmp = 2048;
        public bool isDeviceOpen = false;

        public string MyConnection()
        {
            return connectionString;
        }

        // --- Employee Properties ---
        public string EmployeeID { get; set; }
        public string Surname { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string Nationality { get; set; }
        public string Status { get; set; }
        public string Religion { get; set; }
        public string EmailAddress { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public DateTime DateOfHired { get; set; }
        public string PhotoPath { get; set; }

        // New property to store the fingerprint template in the DB
        public byte[] FingerprintTemplate { get; set; }

        public string SEARCH_EMPLOYEE { get; set; }

        public EMPLOYEEE_REGDB GetEmployeeByID(string id)
        {
            EMPLOYEEE_REGDB employee = null;

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                // Added FingerprintTemplate to the SELECT query
                string sql = "SELECT * FROM Employees WHERE EmployeeID = @id";

                using (SqlCommand cm = new SqlCommand(sql, cn))
                {
                    cm.Parameters.AddWithValue("@id", id);

                    using (SqlDataReader dr = cm.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            employee = new EMPLOYEEE_REGDB
                            {
                                EmployeeID = dr["EmployeeID"].ToString(),
                                Surname = dr["Surname"].ToString(),
                                FirstName = dr["FirstName"].ToString(),
                                MiddleName = dr["MiddleName"].ToString(),
                                Address = dr["Address"].ToString(),
                                ContactNumber = dr["ContactNumber"].ToString(),
                                DateOfBirth = Convert.ToDateTime(dr["DateOfBirth"]),
                                Gender = dr["Gender"].ToString(),
                                Age = Convert.ToInt32(dr["Age"]),
                                Nationality = dr["Nationality"].ToString(),
                                Status = dr["Status"].ToString(),
                                Religion = dr["Religion"].ToString(),
                                EmailAddress = dr["EmailAddress"].ToString(),
                                Department = dr["Department"].ToString(),
                                Position = dr["Position"].ToString(),
                                DateOfHired = Convert.ToDateTime(dr["DateOfHired"]),
                                PhotoPath = dr["PhotoPath"].ToString(),
                                SEARCH_EMPLOYEE = dr["EmployeeID"].ToString(),

                                // Retrieve the fingerprint blob if it exists
                                FingerprintTemplate = dr["FingerprintTemplate"] as byte[]
                            };
                        }
                    }
                }
            }
            return employee;
        }
    }
}