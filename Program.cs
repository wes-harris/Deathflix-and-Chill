using System;
using Npgsql; // PostgreSQL library
using dotenv.net; // Library to load .env file

class Program
{
    static void Main(string[] args)
    {
        // Load environment variables from .env file
        DotEnv.Load();

        // Get the connection string from the environment variable
        string? connString = Environment.GetEnvironmentVariable("DATABASE_URL");

        // Check if the connection string was loaded
        if (string.IsNullOrEmpty(connString))
        {
            Console.WriteLine("Connection string is missing. Check your .env file.");
            return;
        }

        // Connect to PostgreSQL
        using (var conn = new NpgsqlConnection(connString))
        {
            try
            {
                // Open the connection
                conn.Open();
                Console.WriteLine("Connected to PostgreSQL!");

                // Execute a simple query
                using (var cmd = new NpgsqlCommand("SELECT version();", conn))
                {
                    object? result = cmd.ExecuteScalar();

                    // Safely handle null or unexpected results
                    if (result is null)
                    {
                        Console.WriteLine("Query did not return a result.");
                    }
                    else
                    {
                        string version = result.ToString()!;
                        Console.WriteLine($"PostgreSQL version: {version}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
