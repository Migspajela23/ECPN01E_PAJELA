using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ELECTIVE_PAJELA
{
    internal class inventoryDBcs
    {
        private string connectionString = @"Data Source=LAPTOP-RF7MTOVT\SQLEXPRESS;Initial Catalog=InventoryDB;Integrated Security=True;TrustServerCertificate=True";

        public string MyConnection()
        {
            return connectionString;
        }

        // Properties for product details
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Barcode { get; set; }
        public string PicPath { get; set; }
        public string CategoryName { get; set; }

        public inventoryDBcs GetProductByID(int id)
        {
            inventoryDBcs inventoryDBcs = null;

            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                string sql = "SELECT p.*, c.Category_Name FROM PRODUCTS p " +
                             "JOIN CATEGORY c ON p.Category_ID = c.Category_ID " +
                             "WHERE p.Product_ID = @id";

                using (SqlCommand cm = new SqlCommand(sql, cn))
                {
                    cm.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader dr = cm.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            inventoryDBcs = new inventoryDBcs
                            {
                                ProductID = (int)dr["Product_ID"],
                                ProductName = dr["Product_Name"].ToString(),
                                Price = (decimal)dr["Product_Price"],
                                Quantity = (int)dr["Stock_Quantity"],
                                Barcode = dr["Barcode"].ToString(),
                                PicPath = dr["PicPath"].ToString(),
                                CategoryName = dr["Category_Name"].ToString()
                            };
                        }
                    }
                }
            }
            return inventoryDBcs;
        }
    }
}



