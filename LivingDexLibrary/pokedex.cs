using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Formats.Asn1.AsnWriter;

namespace LivingDexLibrary;

public class Pokedex
{
    private readonly APIAccess ApiCaller = new APIAccess();
    private readonly DatabaseHandler dbHandler = new DatabaseHandler();

    //Pokemon Variants to not track
    private readonly string[] variantBlacklist = [
    "-mega", "-mega-x", "-mega-y", "-primal", //Megas
    "-totem", "-totem-alola", "-totem-disguised", "-totem-busted",  //Totems
    "-origin", //Origin Forms for Palkia, Giratina and Dialga
    "-normal", "-fighting", "-flying", "-poison", "-ground", "-rock", "-bug", "-ghost", "-steel", "-fire", "-water", "-grass", "-electric", "-psychic", "-ice", "-dragon", "-dark", "-fairy", "-unknown", //Type Forms for Arceus and Silvally
    ];

    //Specific Forms to not include, used for unique variants of species
    private readonly string[] formBlacklist = [
    "castform-sunny", "castform-rainy", "castform-snowy", //Castform
    "cherrim-overcast", "cherrim-sunshine", //Cherrim
    "darmanitan-zen", "darmanitan-galar-zen", //Darmanitan
    "kyurem-black", "kyurem-white", //Kyurem
    "meloetta-aria", "meloetta-pirouette", //Melloetta
    "giratina-altered", //Giratina
    "genesect-chill", "genesect-burn", "genesect-douse", "genesect-shock", //Genesct
    "greninja-ash", "greninja-battle-bond", //Greninja
    "aegislash-shield", "aegislash-blade", //Aegislash
    "xerneas-active", "xerneas-neutral", //Xerneas
    "zygarde-10-power-construct", "zygarde-50-power-construct", "zygarde-complete", //Zygarde
    "wishiwashi-solo", "wishiwashi-school", //Wishiwashi
    "mimikyu-disguised", "mimikyu-busted",  //Mimikyu
    "necrozma-dusk", "necrozma-dawn", "necrozma-ultra", //Necrozma
    "cramorant-gulping", "cramorant-gorging", //Cramorant
    "eiscue-noice", //Eiscue
    "morpeko-full-belly", "morpeko-hangry", //Morpeko
    "zacian-crowned", //Zacian
    "zamazenta-crowned", //Zamazenta
    "eternatus-eternamax", //Eternatus
    "calyrex-ice", "calyrex-shadow", //Calyrex
    "palafin-zero", "palafin-hero", //Palafin
    "ogerpon-wellspring-mask", "ogerpon-hearthflame-mask", "ogerpon-cornerstone-mask", //Ogerpon
    "terapagos-terastal", "terapagos-stellar", //Terapogos
    "pikachu-cosplay", "pikachu-libre", "pikachu-phd", "pikachu-pop-star", "pikachu-belle", "pikachu-rock-star", "pikachu-starter", //Pikachu
    "miraidon-ultimate-mode", "miraidon-aquatic-mode", "miraidon-drive-mode", "miraidon-glide-mode","miraidon-low-power-mode", //Miraidon
    "koraidon-apex-build", "koraidon-gliding-build", "koraidon-limited-build", "koraidon-sprinting-build", "koraidon-swimming-build", //Koraidon
    "spewpa-icy-snow", "spewpa-archipelago", "spewpa-continental", "spewpa-elegant", "spewpa-fancy", "spewpa-garden", "spewpa-high-plains", "spewpa-jungle" ,"spewpa-marine", "spewpa-meadow", "spewpa-modern", "spewpa-monsoon", "spewpa-ocean", "spewpa-poke-ball", "spewpa-polar", "spewpa-river", "spewpa-sandstorm", "spewpa-savanna", "spewpa-sun", "spewpa-tundra", //Spewpa
    "eevee-starter", //Eevee
    "mothim-sandy", "mothim-trash", //Mothim
    "pichu-spiky-eared", //Pichu
    "scatterbug-icy-snow", "scatterbug-archipelago", "scatterbug-continental", "scatterbug-elegant", "scatterbug-fancy", "scatterbug-garden", "scatterbug-high-plains", "scatterbug-jungle", "scatterbug-marine", "scatterbug-meadow", "scatterbug-modern", "scatterbug-monsoon", "scatterbug-ocean", "scatterbug-poke-ball", "scatterbug-polar", "scatterbug-river", "scatterbug-sandstorm", "scatterbug-savanna", "scatterbug-sun", "scatterbug-tundra", //Scatterbug
    "minior-red-meteor", "minior-blue-meteor", "minior-green-meteor", "minior-indigo-meteor", "minior-orange-meteor", "minior-violet-meteor", "minior-yellow-meteor", //Minior
    "floette-eternal", //Floette
    "rockruff-own-tempo", //Rockruff
    "indeedee-male", //Indeedee
    "meowstic-male", //Meowstic
    "mothim-plant", //Mothim
    "basculegion-male", //Basculegion
    ];

    //Pokemon Forms That Are Gender Locked If Species Otherwise Isn't
    private readonly string[] genderFormBlacklist = [
    "pikachu-alola-cap", "pikachu-hoenn-cap", "pikachu-kalos-cap", "pikachu-original-cap", "pikachu-partner-cap", "pikachu-sinnoh-cap", "pikachu-unova-cap", "pikachu-world-cap", //Cap pikachu
    ];

    public async Task constructPokedex()
    {

        //Start API Calls
        //Gender Lists
        var femaleList = ApiCaller.pokeAPICall("gender/1");
        var maleList = ApiCaller.pokeAPICall("gender/2");
        var genderlessList = ApiCaller.pokeAPICall("gender/3");

        //Get list of all pokemon variants
        var allVariantsList = ApiCaller.pokeAPICall("pokemon-form?limit=100000&offset=0");

        //Add Genderless Pokemon, Add Male Pokemon
        await Task.Run(async () => Task.WhenAll(addPokemonFromGender(JObject.Parse(await genderlessList)), addPokemonFromGender(JObject.Parse(await maleList), "canBeMale")));

        //Add Female Pokemon
        await Task.Run(async () => addPokemonFromGender(JObject.Parse(await femaleList), "canBeFemale"));

        //Add Additional Variants
        await Task.Run(async () => addAdditionalForms(JObject.Parse(await allVariantsList)));

        //Add Female Variants That Are Missing
        await Task.Run(() => addAllGenderForms());

        Trace.WriteLine("Pokedex Built");
    }

    private async Task addPokemonFromGender(JObject pokemonList, string? genderField = null)
    {
        var tasks = new List<Task>();

        //Get Species That Can Be Gender
        foreach (JObject pokemon in pokemonList["pokemon_species_details"])
        {
            var id = pokemon["pokemon_species"]["url"].ToString().Split("/")[6];
            string name = pokemon["pokemon_species"]["name"].ToString();

            List<(string, object)> fields = [("name", name), ("species", name), ("speciesID", id), ("variantID", id), ("displayImage", name + ".png")];

            if (genderField != null)
            {
                fields.Add((genderField, true));
            }

            tasks.Add(dbHandler.Upsert("Pokedex", fields.ToArray(), ["name", "variantID"]));
        };

        await Task.WhenAll(tasks);
    }

    private async Task addAdditionalForms(JObject allVariantsList)
    {
        var tasks = new List<Task>();

        foreach (JObject pokemon in allVariantsList["results"])
        {
            var id = pokemon["url"].ToString().Split("/")[6];

            //Not Explicitly Blacklisted
            if (!Array.Exists(formBlacklist, element => element == pokemon["name"].ToString()))
            {
                string[] nameArray = pokemon["name"].ToString().Split("-");
                string species;
                string variantSuffix;

                //Check If Hyphenated Species Name By Seeing If In Database
                if(await dbHandler.Exists("Pokedex", "species", nameArray[0]))
                {
                    //Not Hyphenated
                    species = nameArray[0];
                    variantSuffix = "-" + String.Join("-", nameArray.Skip(1).ToArray());
                } 
                else
                {
                    //Hyphenated
                    species = nameArray[0] + "-" + nameArray[1];
                    variantSuffix = "-" + String.Join("-", nameArray.Skip(2).ToArray());
                }

                //Edge Case To Catch Female Variants With Unique ID's
                if (variantSuffix.Contains("-female"))
                {
                    //Get Species Data
                    object[] speciesData = await dbHandler.GetEntry("Pokedex", ("species", species), ["speciesID", "variantID"]);

                    //Update Species
                    await dbHandler.Upsert("Pokedex", [("name", species), ("species", species), ("speciesID", speciesData[0]), ("variantID", speciesData[1]), ("canBeFemale", true)], ["name", "variantID"]);
                }

                //Check Variant Isn't Blacklisted
                if (!Array.Exists(variantBlacklist, element => element == variantSuffix) && variantSuffix != "-")
                {
                    //Get Species Data
                    object[] speciesData = await dbHandler.GetEntry("Pokedex", ("species", species), ["speciesID", "canBeMale", "canBeFemale"]);

                    //Upsert Variant
                    tasks.Add(dbHandler.Upsert("Pokedex", [("name", pokemon["name"].ToString()), ("species", species), ("variantSuffix", variantSuffix), ("speciesID", speciesData[0]), ("VariantID", id), ("canBeMale", speciesData[1]), ("canBeFemale", speciesData[2]), ("displayImage", pokemon["name"] + ".png")], ["name", "variantID"]));
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    private async Task addAllGenderForms()
    {
        var tasks = new List<Task>();

        List<object[]> dualGenderedForms = await dbHandler.FilterEntries("Pokedex", [("canBeMale", 1), ("canBeFemale", 1)], ["name", "species", "speciesID", "variantID", "variantSuffix", "displayImage"]);

        foreach (object[] pokemon in dualGenderedForms)
        {
            tasks.Add(addGenderForm(pokemon));
        }

        await Task.WhenAll(tasks);
    }

    private async Task addGenderForm(object[] pokemonDetails)
    {
        //Check Form Isn't Blacklisted From having Female Variant
        if (!Array.Exists(genderFormBlacklist, element => element == pokemonDetails[0].ToString()))
        {
            var tasks = new List<Task>();

            //Initialise variable to false
            bool hasGenderDifferences = false;
            var femaleName = pokemonDetails[0].ToString();
            var femaleVariantSuffix = pokemonDetails[4].ToString();
            var femaleDisplayImage = pokemonDetails[5].ToString();

            //Check If Base Variant As Otherwise API Cannot Detail If Gender Differenes Exist
            if ((Int64)pokemonDetails[2] == (Int64)pokemonDetails[3])
            {
                var apiSpeciesData = ApiCaller.pokeAPICall("pokemon-species/" + pokemonDetails[2]);
                hasGenderDifferences = (bool)JObject.Parse(await apiSpeciesData)["has_gender_differences"];
            }

            
            if (hasGenderDifferences)
            {
                //Update Male If Required
                tasks.Add(dbHandler.Update("Pokedex", [("hasGenderDifferences", true)], ("name", pokemonDetails[0])));

                //Update displayImage If Required
                femaleDisplayImage = femaleDisplayImage.Replace(".png", "-female.png");
            }

            if (!pokemonDetails[4].ToString().Contains("-female"))
            {
                femaleName = femaleName + "-female";
                femaleVariantSuffix = femaleVariantSuffix + "-female";
            }

            //Add Female
            tasks.Add(dbHandler.Upsert("Pokedex", [("name", femaleName), ("species", pokemonDetails[1].ToString()), ("variantSuffix", femaleVariantSuffix), ("speciesID", int.Parse(pokemonDetails[2].ToString())), ("variantID", (int.Parse(pokemonDetails[3].ToString()) % 20000) + 20000), ("canBeMale", true), ("canBeFemale", true), ("hasGenderDifferences", hasGenderDifferences), ("displayImage", femaleDisplayImage)], ["name", "variantID"]));

            await Task.WhenAll(tasks);
        }
    }

    public async Task<Pokemon[]> getMultiplePokemonByFilter(int numberOfPokemon, int offset, (string, object)[] filters)
    {
        //Initialise Box Container
        List<Pokemon> pokemonList = new List<Pokemon>();

        //Get Pokemon That Match Filter
        List<object[]> matchFilter = await dbHandler.FilterEntries("Pokedex", filters, ["name", "species", "variantSuffix", "firstType", "secondType", "displayImage", "speciesID", "variantID", "caught", "canBeMale", "canBeFemale", "hasGenderDifferences"], [("speciesID", "ASC"), ("variantID", "ASC")], numberOfPokemon, offset);

        for (int i = 0; i < matchFilter.Count; i++) 
        {
            var pokemonDetails = matchFilter[i];
            pokemonList.Add(new Pokemon(pokemonDetails[0].ToString(), pokemonDetails[1].ToString(), pokemonDetails[2].ToString(), pokemonDetails[3].ToString(), pokemonDetails[4].ToString(), pokemonDetails[5].ToString(), int.Parse(pokemonDetails[6].ToString()), int.Parse(pokemonDetails[7].ToString()), (int.Parse(pokemonDetails[8].ToString()) == 1), (int.Parse(pokemonDetails[9].ToString()) == 1), (int.Parse(pokemonDetails[10].ToString()) == 1), (int.Parse(pokemonDetails[11].ToString()) == 1)));
        }

        //Return output
        return pokemonList.ToArray();
    }

    public async Task<int> getPokedexSize()
    {
        var size = (await dbHandler.CountEntries("Pokedex", [("displayStatus", true)], "name"));

        return size;
    }

    public async Task toggleCaught(string pokemonName) 
    {
        bool currentlyCaught = int.Parse((await dbHandler.GetEntry("Pokedex", ("name", pokemonName), ["caught"]))[0].ToString()) == 1;

        dbHandler.Update("Pokedex", [("caught", !currentlyCaught)], ("name", pokemonName));
    }
}

public class Pokemon
{
    public string name { get; set; }
    public string species { get; set; }
    public string? variantSuffix { get; set; }
    public string? firstType { get; set; }
    public string? secondType { get; set; }
    public string? displayImage { get; set; }

    public int speciesID { get; set; }
    public int variantID { get; set; }

    public bool? caught { get; set; } = false;
    public bool? canBeMale { get; set; } = false;
    public bool? canBeFemale { get; set; } = false;
    public bool? hasGenderDifferences { get; set; } = false;

    public Pokemon(string nameInput, string speciesInput, string? variantSuffixInput, string? firstTypeInput, string? secondTypeInput, string? displayImageInput, int speciesIDInput, int variantIDInput, bool? caughtInput, bool? canBeMaleInput, bool? canBeFemaleInput, bool? hasGenderDifferencesInput)
    {
        name = nameInput;
        species = speciesInput;
        variantSuffix = variantSuffixInput;
        firstType = firstTypeInput;
        secondType = secondTypeInput;
        displayImage = displayImageInput;

        speciesID = speciesIDInput;
        variantID = variantIDInput;

        caught = caughtInput;
        canBeMale = canBeMaleInput;
        canBeFemale = canBeFemaleInput;
        hasGenderDifferences = hasGenderDifferencesInput;
    }
}
