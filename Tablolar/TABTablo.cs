using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using System.Reflection.Metadata.Ecma335;

namespace TarimDonusum.Tablolar
{
    public abstract class TABTablo
    {
        protected SqlConnection Connection { get; }
        protected IStringLocalizer<SharedResource>? L { get; }
        protected SqlTransaction? Transaction { get; }

        protected TABTablo(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        {
            Connection = connection;
            L = localizer;
            Transaction = transaction;
        }

        protected SqlCommand KomutOlustur(string sql)
        {
            return new SqlCommand(sql, Connection, Transaction);
        }

        protected static decimal? NullOkuDecimal(SqlDataReader reader, int kolNo)
        {
            return reader.IsDBNull(kolNo) ? null : reader.GetDecimal(kolNo);
        }

        protected static int? NullOkuInt(SqlDataReader reader, int kolNo)
        {
            return reader.IsDBNull(kolNo) ? null : reader.GetInt32(kolNo);
        }

        protected static int NullDuzeltInt(SqlDataReader reader, int kolNo, int varsayilanDeger = 0)
        {
            return reader.IsDBNull(kolNo) ? varsayilanDeger : reader.GetInt32(kolNo);
        }

        protected static bool BoolYap(int? deger)
        {
            if (deger == null) return false;
            if (deger.Value == 0) return false;
            return true;
        }

        protected static string? NullOkuString(SqlDataReader reader, int kolNo)
        {
            return reader.IsDBNull(kolNo) ? null : reader.GetString(kolNo);
        }
    }
}
