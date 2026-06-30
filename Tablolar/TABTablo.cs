using Microsoft.Data.SqlClient;

namespace TarimDonusum.Tablolar
{
    public abstract class TABTablo
    {
        protected SqlConnection Connection { get; }
        protected SqlTransaction? Transaction { get; }

        protected TABTablo(SqlConnection connection, SqlTransaction? transaction = null)
        {
            Connection = connection;
            Transaction = transaction;
        }

        protected SqlCommand KomutOlustur(string sql)
        {
            return new SqlCommand(sql, Connection, Transaction);
        }
    }
}
