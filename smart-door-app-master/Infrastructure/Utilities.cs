using Npgsql;

namespace Infrastructure;

public static class Utilities
{
    public static readonly Uri Uri;
    public static readonly string ProperlyFormattedConnectionString;

    static Utilities()
    {
        const string envVarKeyName = "pgconn";
        var rawConnectionString = Environment.GetEnvironmentVariable(envVarKeyName);

        if (string.IsNullOrEmpty(rawConnectionString))
        {
            throw new ArgumentNullException(envVarKeyName, "Connection string is not set in environment variables.");
        }

        try
        {
            Uri = new Uri(rawConnectionString);

            var userInfo = Uri.UserInfo.Split(':');
            var defaultPort = Uri.Port > 0 ? Uri.Port : 5432;

            ProperlyFormattedConnectionString = 
                $"Server={Uri.Host};" +
                $"Database={Uri.AbsolutePath.Trim('/')};" +
                $"User Id={userInfo[0]};" +
                $"Password={userInfo[1]};" +
                $"Port={defaultPort};" +
                $"Pooling=true;MaxPoolSize=3";

            using var connection = new NpgsqlConnection(ProperlyFormattedConnectionString);
            connection.Open();    
        }
        catch (UriFormatException ex)
        {
            throw new FormatException("Invalid Postgres connection string format.", ex);
        }
        catch (NpgsqlException ex)
        {
            throw new Exception("Failed to open Postgres connection with provided connection string.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred while initializing the Utilities class.", ex);
        }
    }
}