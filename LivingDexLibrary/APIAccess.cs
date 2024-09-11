using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace LivingDexLibrary;

public class APIAccess : DbContext
{
    //Create HTTP Client
    private readonly HttpClient client = new HttpClient();

    public DbSet<APICallResult> APIResults { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source = APICache.db");
    }

    public async Task<string> pokeAPICall(string query)
    {
        //initialise variables
        string data = null;
        DateTime timeStamp = new DateTime(0);

        //Attempt To Get Cached API Call
        var sqlRequest = "Select * FROM APIResults WHERE query = @query";

        //Open Database
        using var database = new SqliteConnection("Data Source=APICache.db");
        database.Open();

        using var requestCommand = new SqliteCommand(sqlRequest, database);
        requestCommand.Parameters.AddWithValue("@query", query);

        using var reader = requestCommand.ExecuteReader();

        Console.WriteLine(reader.HasRows);
        if (reader.Read()) {
            data = reader.GetString(1);
            timeStamp = reader.GetDateTime(2);
        }

        //data not found or out of date
        if (data == null || DateTime.Now.Subtract(timeStamp) > new TimeSpan(7, 0, 0, 0)) 
        {
            //Get From API

            //Base API URL
            const string baseAPI = "https://pokeapi.co/api/v2/";

            var response = await client.GetAsync(baseAPI + query);

            if (response.IsSuccessStatusCode)
            {
                data = response.Content.ReadAsStringAsync().Result;

                //SQL Insert Command
                var sqlInsert = "INSERT INTO APIResults (query, result, timestamp) VALUES (@query, @result, @timeStamp) ON CONFLICT(query) DO UPDATE SET result = @result";

                using var insertCommand = new SqliteCommand(sqlInsert, database);
                insertCommand.Parameters.AddWithValue("@query", query);
                insertCommand.Parameters.AddWithValue("@result", data);
                insertCommand.Parameters.AddWithValue("@timeStamp", DateTime.Now);

                //Execute Command
                var rowInserted = insertCommand.ExecuteNonQuery();

                //Return Data
                return data;
            }
            else
            {
                return $"Error: {response.StatusCode}";
            }
        } 
        else
        {
            return data;
        }
    }
}

public class APICallResult
{
    [Key]
    public string query { get; set; }
    public string result { get; set; }
    public DateTime timeStamp { get; set; }
}
