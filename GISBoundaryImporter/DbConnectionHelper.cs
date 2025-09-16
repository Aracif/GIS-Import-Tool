using Microsoft.Data.SqlClient;

namespace GISBoundaryImporter
{
    public static class DbConnectionHelper
    {
        public static (bool ok, string message) TestConnection(
            string server,
            string database,
            bool integratedSecurity = true,
            string? userId = null,
            string? password = null,
            bool encrypt = true,
            bool trustServerCertificate = true,
            int timeoutSeconds = 5)
        {
            try
            {
                var csb = new SqlConnectionStringBuilder
                {
                    DataSource = server,
                    InitialCatalog = database,
                    Encrypt = encrypt,
                    TrustServerCertificate = trustServerCertificate,
                    ConnectTimeout = timeoutSeconds
                };

                if (integratedSecurity)
                {
                    csb.IntegratedSecurity = true;
                }
                else
                {
                    csb.IntegratedSecurity = false;
                    csb.UserID = userId ?? "";
                    csb.Password = password ?? "";
                }

                using var conn = new SqlConnection(csb.ConnectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT @@SERVERNAME AS ServerName, @@VERSION AS ServerVersion";
                using var r = cmd.ExecuteReader();
                string serverName = "", serverVersion = "";
                if (r.Read())
                {
                    serverName = r["ServerName"]?.ToString() ?? "";
                    serverVersion = r["ServerVersion"]?.ToString() ?? "";
                }

                return (true, $"Connected to '{serverName}' ({database}). OK.\n{serverVersion}");
            }
            catch (SqlException ex)
            {
                return (false, $"SQL error connecting to '{server}\\{database}': {ex.Message}");
            }
            catch (System.Exception ex)
            {
                return (false, $"Error connecting to '{server}\\{database}': {ex.Message}");
            }
        }

        public static async Task<(bool ok, string message)> TestConnectionAsync(
            string server,
            string database,
            bool integratedSecurity = true,
            string? userId = null,
            string? password = null,
            bool encrypt = true,
            bool trustServerCertificate = true,
            int timeoutSeconds = 5)
        {
            try
            {
                var csb = new SqlConnectionStringBuilder
                {
                    DataSource = server,
                    InitialCatalog = database,
                    Encrypt = encrypt,
                    TrustServerCertificate = trustServerCertificate,
                    ConnectTimeout = timeoutSeconds
                };

                if (integratedSecurity)
                {
                    csb.IntegratedSecurity = true;
                }
                else
                {
                    csb.IntegratedSecurity = false;
                    csb.UserID = userId ?? "";
                    csb.Password = password ?? "";
                }

                await using var conn = new SqlConnection(csb.ConnectionString);
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT @@SERVERNAME AS ServerName, @@VERSION AS ServerVersion";
                await using var r = await cmd.ExecuteReaderAsync();
                string serverName = "", serverVersion = "";
                if (await r.ReadAsync())
                {
                    serverName = r["ServerName"]?.ToString() ?? "";
                    serverVersion = r["ServerVersion"]?.ToString() ?? "";
                }

                return (true, $"Connected to '{serverName}' ({database}). OK.\n{serverVersion}");
            }
            catch (SqlException ex)
            {
                return (false, $"SQL error connecting to '{server}\\{database}': {ex.Message}");
            }
            catch (System.Exception ex)
            {
                return (false, $"Error connecting to '{server}\\{database}': {ex.Message}");
            }
        }
    }
}
