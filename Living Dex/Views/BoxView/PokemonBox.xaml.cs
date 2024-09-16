using LivingDexLibrary;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Living_Dex.Views.BoxView
{
    /// <summary>
    /// Interaction logic for PokemonBox.xaml
    /// </summary>
    public partial class PokemonBox : UserControl
    {
        //Create Pokedex for Access
        private readonly Pokedex pokedex = new Pokedex();

        //Api for calls
        private readonly APIAccess api = new();

        //Tracker For Current Box
        int currentBoxNumber = 0;

        //Array of image elements
        Image[] imageElements = new Image[30];

        //Task List
        List<Task> boxTasks = new List<Task>();

        //Pokemon in currentBox
        Pokemon[] currentBox = new Pokemon[30];

        public PokemonBox()
        {
            InitializeComponent();
        }

        private async Task displayBox(int boxNumber)
        {
            //await existing Tasks
            await Task.WhenAll(new List<Task>(boxTasks).SkipLast(1));

            //Tasks list
            List<Task> tasks = new List<Task>();

            //Initialise Box Container
            Pokemon[] box = await pokedex.getMultiplePokemonByFilter(30, (boxNumber) * 30, [("displayStatus", true)]);

            //Array of tasks that will give local image paths
            Task<string>[] displayImagesTasks = new Task<string>[box.Length];

            //Update Header
            HeaderText.Text = $"{box[0].speciesID} - {box[box.Length - 1].speciesID}";

            //Update Footer
            FooterText.Text = $"Box {boxNumber + 1}";

            //Populate Arrays
            for (int i = 0; i < box.Length; i++) 
            {
                displayImagesTasks[i] = api.ensureImageLocal(box[i].displayImage);
            }

            string[] displayImagePaths = await Task.WhenAll(displayImagesTasks);

            for (int i = 0; i < box.Length; i++){
                imageElements[i].Source = new BitmapImage(new Uri("../." + displayImagePaths[i], UriKind.Relative));
            }

            for (int i = box.Length; i < 30; i++)
            {
                imageElements[i].Source = new BitmapImage(new Uri("", UriKind.Relative));
            }

            await Task.WhenAll(tasks);

            Trace.WriteLine($"Images For Box {boxNumber} loaded");

            currentBox = box;
        }

        private async void PreviousBoxButton_Click(object sender, RoutedEventArgs e)
        {
            //Go to previous Box
            currentBoxNumber -= 1;
            currentBoxNumber = trueMod(currentBoxNumber , (int)Math.Ceiling((decimal)(await pokedex.getPokedexSize()) / 30));
            
            boxTasks.Add(displayBox(currentBoxNumber));
        }

        private async void NextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            //Go to next box
            currentBoxNumber++;
            currentBoxNumber = trueMod(currentBoxNumber, (int)Math.Ceiling((decimal)(await pokedex.getPokedexSize()) / 30));

            boxTasks.Add(displayBox(currentBoxNumber));
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //Image Elements
            for (int i = 0; i < 30; i++)
            {
                imageElements[i] = this.FindName($"BoxPokemonImage{i + 1}") as Image;
            }

            //Load First Box
            boxTasks.Add(displayBox(currentBoxNumber));
        }

        private void FooterText_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        //C# doesn't have modulo, custom function to account
        private int trueMod(int input, int mod)
        {
            if(input >= 0)
            {
                return input % mod;
            } 
            else
            {
                return (mod + input) % mod;
            }
        }

        private void BoxPokemonImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Image pressedImage = sender as Image;

            string pokemonName = currentBox[int.Parse(pressedImage.Name.Remove(0, 15)) - 1].name;

            Trace.WriteLine(pokemonName);

            _ = pokedex.toggleCaught(pokemonName);
        }
    }
}
