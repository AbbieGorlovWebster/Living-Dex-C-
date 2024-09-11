using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LivingDexLibrary;

public class DatabaseHandler : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source = LivingDex.db");
    }

    public DbSet<Pokemon> Pokedex { get; set; }

    private readonly APIAccess ApiCaller = new APIAccess();

    public async void constructPokedex()
    {
        //Get list of all pokemon variants
        var allVariantsList = ApiCaller.pokeAPICall("pokemon-form?limit=100000&offset=0");

        addMale();
        addGenderless();
    }

    private async void addMale()
    {
        //Get Species That Can Be Male
        var pokemonList = await ApiCaller.pokeAPICall("gender/2");

        foreach (JObject pokemon in JObject.Parse(pokemonList)["pokemon_species_details"])
        {
            var id = pokemon["pokemon_species"]["url"].ToString().Split("/")[6];
            string name = pokemon["pokemon_species"]["name"].ToString();

            //Open Database
            using var database = new SqliteConnection("Data Source=LivingDex.db");
            database.Open();

            //SQL Insert Command
            var sqlInsert = "INSERT INTO Pokedex (name, species, speciesID, variantID, canBeMale) VALUES (@name, @name, @id, @id, @CanBeMale)";

            using var insertCommand = new SqliteCommand(sqlInsert, database);
            insertCommand.Parameters.AddWithValue("@name", name);
            insertCommand.Parameters.AddWithValue("@id", id);
            insertCommand.Parameters.AddWithValue("@CanBeMale", true);

            //Execute Command
            var rowInserted = insertCommand.ExecuteNonQuery();
        } ;
    }

    private async void addFemale()
    {
        //Get Species That Can Be Male
        var pokemonList = await ApiCaller.pokeAPICall("gender/1");

        foreach (JObject pokemon in JObject.Parse(pokemonList)["pokemon_species_details"])
        {
            var id = pokemon["pokemon_species"]["url"].ToString().Split("/")[6];
            string name = pokemon["pokemon_species"]["name"].ToString();

            //Open Database
            using var database = new SqliteConnection("Data Source=LivingDex.db");
            database.Open();

            //SQL Insert Command
            var sqlInsert = "INSERT INTO Pokedex (name, species, speciesID, variantID, canBeMale) VALUES (@name, @name, @id, @id, @CanBeMale)";

            using var insertCommand = new SqliteCommand(sqlInsert, database);
            insertCommand.Parameters.AddWithValue("@name", name);
            insertCommand.Parameters.AddWithValue("@id", id);
            insertCommand.Parameters.AddWithValue("@CanBeMale", true);

            //Execute Command
            var rowInserted = insertCommand.ExecuteNonQuery();
        };
    }

    private async void addGenderless()
    {
        //Get Species That Can Be Male
        var pokemonList = await ApiCaller.pokeAPICall("gender/3");

        foreach (JObject pokemon in JObject.Parse(pokemonList)["pokemon_species_details"])
        {
            var id = pokemon["pokemon_species"]["url"].ToString().Split("/")[6];
            string name = pokemon["pokemon_species"]["name"].ToString();

            //Open Database
            using var database = new SqliteConnection("Data Source=LivingDex.db");
            database.Open();

            //SQL Insert Command
            var sqlInsert = "INSERT INTO Pokedex (name, species, speciesID, variantID, canBeMale, canBeFemale) VALUES (@name, @name, @id, @id, @CanBeMale, @CanBeFemale)";

            using var insertCommand = new SqliteCommand(sqlInsert, database);
            insertCommand.Parameters.AddWithValue("@name", name);
            insertCommand.Parameters.AddWithValue("@id", id);
            insertCommand.Parameters.AddWithValue("@CanBeMale", false);
            insertCommand.Parameters.AddWithValue("@CanBeFemale", false);

            //Execute Command
            var rowInserted = insertCommand.ExecuteNonQuery();
        };
    }
}
