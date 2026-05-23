using Npgsql;

namespace DoAnSE104.Helpers
{
    public static class ConnectionStringHelper
    {
        public static string NormalizePostgresConnectionString(string connectionString)
        {
            if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return connectionString;
            }

            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':', 2);
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Database = uri.AbsolutePath.TrimStart('/'),
                Username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty),
                Password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty),
                SslMode = SslMode.Require
            };

            if (!uri.IsDefaultPort)
            {
                builder.Port = uri.Port;
            }

            var query = uri.Query.TrimStart('?');
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                var key = Uri.UnescapeDataString(pair[0]);
                var value = Uri.UnescapeDataString(pair.ElementAtOrDefault(1) ?? string.Empty);

                if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                {
                    builder.SslMode = value.Equals("disable", StringComparison.OrdinalIgnoreCase)
                        ? SslMode.Disable
                        : SslMode.Require;
                }
            }

            return builder.ConnectionString;
        }
    }
}
