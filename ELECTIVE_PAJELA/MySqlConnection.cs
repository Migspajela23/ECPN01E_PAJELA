namespace ELECTIVE_PAJELA
{
    internal class MySqlConnection
    {
        public MySqlConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }
    }
}