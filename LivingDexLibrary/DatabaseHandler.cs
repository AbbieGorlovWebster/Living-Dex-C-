using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Xml;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Xml.Linq;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

namespace LivingDexLibrary;

public class DatabaseHandler : DbContext
{
    public void EnsureCreated()
    {
        try
        {
            //Ensure the database and it's tables are created
            //SQL Table Creation Strings
            var sqlCreatePokedex = "CREATE TABLE IF NOT EXISTS Pokedex(name  TEXT NOT NULL UNIQUE, species  TEXT NOT NULL, variantSuffix TEXT, firstType TEXT, SecondType    TEXT, speciesID INTEGER NOT NULL, variantID INTEGER NOT NULL UNIQUE, caught   INTEGER NOT NULL DEFAULT 0, canBeMale INTEGER NOT NULL DEFAULT 0, canBeFemale  INTEGER NOT NULL DEFAULT 0, hasGenderDifferences  INTEGER NOT NULL DEFAULT 0, displayStatus INTEGER NOT NULL DEFAULT 1, displayImage  TEXT NOT NULL DEFAULT 'Fallback.png', CONSTRAINT PK_Pokedex PRIMARY KEY(name))";
            var sqlCreateAPICache = "CREATE TABLE IF NOT EXISTS APIResults (query TEXT NOT NULL,result TEXT NOT NULL, timeStamp TEXT NOT NULL, CONSTRAINT PK_APIResults PRIMARY KEY(query))";

            //Open Database
            using var connection = new SqliteConnection("Data Source=LivingDex.db");
            connection.Open();

            using var pokedexCommand = new SqliteCommand(sqlCreatePokedex, connection);
            pokedexCommand.ExecuteNonQuery();

            using var APICommand = new SqliteCommand(sqlCreateAPICache, connection);
            APICommand.ExecuteNonQuery();

            connection.Close();

            Trace.WriteLine($"Tables Ensured");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Ensure Failed : {ex}");
            Console.Error.WriteLine($"Ensure Failed : {ex}");
        }  
    }

    private void deleteTable(string tableName)
    {
        try
        {
            //Delete Table Instruction
            var sqlDelete = $"DROP TABLE IF EXISTS {tableName}";

            //Open Database
            using var connection = new SqliteConnection("Data Source=LivingDex.db");
            connection.Open();

            //Execute Command
            using var deleteCommand = new SqliteCommand(sqlDelete, connection);
            deleteCommand.ExecuteNonQuery();

            connection.Close();

            Trace.WriteLine($"Delete Command Executed : {sqlDelete}");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Delete Command Failed : {ex}");
            Console.Error.WriteLine($"Delete Command Failed : {ex}");
        }
    }

    public async Task Update(string tableName, (string name, object value)[] fields, (string name, object value) key)
    {
        try
        {
            //Open Database
            using var database = new SqliteConnection("Data Source=LivingDex.db");
            database.Open();

            //Update Instruction
            var sqlUpdate = $"UPDATE {tableName} SET ";

            for (int i = 0; i < fields.Length; i++)
            {
                sqlUpdate = sqlUpdate + $"{fields[i].name} = @{fields[i].name} ";

                if (i < fields.Length - 1)
                {
                    sqlUpdate = sqlUpdate + ", ";
                }
            }

            sqlUpdate = sqlUpdate + $"WHERE {key.name} = @keyValue";

            //Update Command
            using var updateCommand = new SqliteCommand(sqlUpdate, database);

            for (int i = 0; i < fields.Length; i++)
            {
                updateCommand.Parameters.AddWithValue($"@{fields[i].name}", fields[i].value);
            }

            updateCommand.Parameters.AddWithValue("@keyValue", key.value);

            await updateCommand.ExecuteNonQueryAsync();

            //Produce Trace Message
            for (int i = 0; i < updateCommand.Parameters.Count; i++)
            {
                sqlUpdate = Regex.Replace(sqlUpdate, updateCommand.Parameters[i].ParameterName.ToString() + @"\b", updateCommand.Parameters[i].Value.ToString());
            }

            Trace.WriteLine($"Update Command Executed : {sqlUpdate}");
        }
        catch (Exception ex) 
        {
            Trace.WriteLine($"Update Command Failed : {ex}");
            Console.Error.WriteLine($"Update Command Failed : {ex}");
        }
    }

    public async Task Upsert(string tableName, (string name, object value)[] fields, string[] uniqueFields = null)
    {
        try
        {
            string fieldNamesJoined = "";
            string fieldNamesJoinedSymbol = "";
            string updateLine = "";

            //Open Database
            using var database = new SqliteConnection("Data Source=LivingDex.db");
            database.Open();

            for (int i = 0; i < fields.Length; i++)
            {
                updateLine += $" {fields[i].name} = @{fields[i].name}";
                fieldNamesJoined += fields[i].name;
                fieldNamesJoinedSymbol += $"@{fields[i].name} ";

                if (i != fields.Length - 1)
                {
                    updateLine += ", ";
                    fieldNamesJoined += ", ";
                    fieldNamesJoinedSymbol += ", ";
                }
            }

            //SQL Upsert Instruction
            var sqlUpsert = $"INSERT INTO {tableName} ({fieldNamesJoined}) VALUES ({fieldNamesJoinedSymbol}) ";

            foreach (string uniqueKey in uniqueFields)
            {
                sqlUpsert += $" ON CONFLICT({uniqueKey}) DO UPDATE SET {updateLine}";
            }

            //Upsert Command
            using var upsertCommand = new SqliteCommand(sqlUpsert, database);
            for (int i = 0; i < fields.Length; i++)
            {
                upsertCommand.Parameters.AddWithValue($"@{fields[i].name}", fields[i].value);
            }

            await upsertCommand.ExecuteNonQueryAsync();

            //Produce Trace Message
            for (int i = 0; i < upsertCommand.Parameters.Count; i++) 
            {
                sqlUpsert = Regex.Replace(sqlUpsert, upsertCommand.Parameters[i].ParameterName.ToString() + @"\b", upsertCommand.Parameters[i].Value.ToString());
            }

            Trace.WriteLine($"Upsert Command Executed : {sqlUpsert}");
        } 
        catch (Exception ex) 
        {
            Trace.WriteLine($"Upsert Command Failed : {ex}");
            Console.Error.WriteLine($"Upsert Command Failed : {ex}");
        }
    }

    public async Task<bool> Exists(string tableName, string fieldName, string fieldValue)
    {
        try
        {
            //Open Database
            using var database = new SqliteConnection("Data Source=LivingDex.db");
            database.Open();

            //Exists Instruction
            var sqlExists = $"SELECT EXISTS(SELECT * FROM {tableName} WHERE {fieldName} = @fieldValue)";

            //Exists Command
            using var existsCommand = new SqliteCommand(sqlExists, database);
            existsCommand.Parameters.AddWithValue("@fieldValue", fieldValue);

            var response = await existsCommand.ExecuteScalarAsync();

            //Produce Trace Message
            for (int i = 0; i < existsCommand.Parameters.Count; i++)
            {
                sqlExists = Regex.Replace(sqlExists, existsCommand.Parameters[i].ParameterName.ToString() + @"\b", existsCommand.Parameters[i].Value.ToString());
            }

            //Decode response
            if ((Int64)response == 0)
            {
                Trace.WriteLine($"Update Command Executed : {sqlExists}");
                return false;
            }
            else
            {
                Trace.WriteLine($"Update Command Executed : {sqlExists}");
                return true;
            } 
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Update Command Failed : {ex}");
            Console.Error.WriteLine($"Update Command Failed : {ex}");

            //Return False as a fail safe
            return false;
        }
    }

    public async Task<object[]> GetEntry(string tableName, (string name, string value) key, string[] columnNames)
    {
        try
        {
            //Open Database
            using var database = new SqliteConnection("Data Source=LivingDex.db");
            database.Open();

            //Get Instruction
            var sqlGet = $"SELECT {string.Join(",", columnNames)} FROM {tableName} WHERE {key.name} = @keyValue";

            //Get Command
            using var getCommand = new SqliteCommand(sqlGet, database);
            getCommand.Parameters.AddWithValue("@keyValue", key.value);

            var response = await getCommand.ExecuteReaderAsync();

            Object[] values = new Object[response.FieldCount];

            if (response.Read())
            {
                int fieldCount = response.GetValues(values);
            }

            //Produce Trace Message
            for (int i = 0; i < getCommand.Parameters.Count; i++)
            {
                sqlGet = Regex.Replace(sqlGet, getCommand.Parameters[i].ParameterName.ToString() + @"\b", getCommand.Parameters[i].Value.ToString());
            }

            Trace.WriteLine($"Get Command Executed : {sqlGet}");

            return values;
        }
        catch (Exception ex) 
        {
            Trace.WriteLine($"Get Command Failed : {ex}");
            Console.Error.WriteLine($"Get Command Failed : {ex}");

            //Return empty object as fallback
            return new Object[0];
        }
    }

    public async Task<List<object[]>> FilterEntries(string tableName, (string field, object value)[] filters, string[] requestedFields, (string field, string direction)[] orderFields = null, int limit = -1, int offset = 0)
    {
        try
        {
            //Open Database
            using var database = new SqliteConnection("Data Source=LivingDex.db");
            database.Open();

            //Filter Instruction
            //Base
            var sqlFilter = $"SELECT {string.Join(",", requestedFields)} FROM {tableName} WHERE ";

            //Filter Parameters
            for (int i = 0; i < filters.Length; i++)
            {
                sqlFilter += $"{filters[i].field} = @{filters[i].field}";

                if (i < filters.Length - 1)
                {
                    sqlFilter += " AND ";
                }
            }

            //Order Paramaters
            if(orderFields != null)
            {
                for (int i = 0; i < orderFields.Length; i++)
                {
                    if (i == 0)
                    {
                        sqlFilter += " ORDER BY ";
                    }

                    sqlFilter += orderFields[i].field + " " + orderFields[i].direction + " ";

                    if(i < orderFields.Length - 1)
                    {
                        sqlFilter += ", ";
                    }
                }
            }

            //Limit and Offset
            sqlFilter += $" LIMIT {limit} OFFSET {offset}";

            //Filter Command
            using var filterCommand = new SqliteCommand(sqlFilter, database);
            for (int i = 0; i < filters.Length; i++)
            {
                filterCommand.Parameters.AddWithValue($"@{filters[i].field}", filters[i].value);
            }

            var response = await filterCommand.ExecuteReaderAsync();

            List<object[]> returnList = new List<object[]>();


            while (response.Read())
            {
                Object[] values = new Object[response.FieldCount];
                int fieldCount = response.GetValues(values);
                returnList.Add(values);
            }

            //Produce Trace Message
            for (int i = 0; i < filterCommand.Parameters.Count; i++)
            {
                sqlFilter = Regex.Replace(sqlFilter, filterCommand.Parameters[i].ParameterName.ToString() + @"\b", filterCommand.Parameters[i].Value.ToString());
            }

            Trace.WriteLine($"Filter Command Executed : {sqlFilter}");

            return returnList;
        } 
        catch(Exception ex) 
        {
            Trace.WriteLine($"Filter Command Failed : {ex}");
            Console.Error.WriteLine($"Filter Command Failed : {ex}");

            //return empty list as fallback
            return new List<object[]>();
        }
    }

    public async Task<int> CountEntries(string tableName, (string field, object value)[] filters, string countField)
    {
        try
        {
            //Open Database
            using var database = new SqliteConnection("Data Source=LivingDex.db");
            database.Open();

            //Count Instruction
            //Base
            var sqlCount = $"SELECT COUNT({countField}) FROM {tableName} WHERE ";

            //Count Parameters
            for (int i = 0; i < filters.Length; i++)
            {
                sqlCount += $"{filters[i].field} = @{filters[i].field}";

                if (i < filters.Length - 1)
                {
                    sqlCount += " AND ";
                }
            }

            //Count Command
            using var countCommand = new SqliteCommand(sqlCount, database);
            for (int i = 0; i < filters.Length; i++)
            {
                countCommand.Parameters.AddWithValue($"@{filters[i].field}", filters[i].value);
            }

            var response = await countCommand.ExecuteReaderAsync();

            Object[] values = new Object[response.FieldCount];

            if (response.Read())
            {
                int fieldCount = response.GetValues(values);
            }

            //Produce Trace Message
            for (int i = 0; i < countCommand.Parameters.Count; i++)
            {
                sqlCount = Regex.Replace(sqlCount, countCommand.Parameters[i].ParameterName.ToString() + @"\b", countCommand.Parameters[i].Value.ToString());
            }

            Trace.WriteLine($"Count Command Executed : {sqlCount}");

            return int.Parse(values[0].ToString());
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Count Command Failed : {ex}");
            Console.Error.WriteLine($"Count Command Failed : {ex}");

            //return empty list as fallback
            return 0;
        }
    }
}
