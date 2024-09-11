using System.Configuration;
using System.Data;
using System.Windows;
using LivingDexLibrary;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Living_Dex
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DatabaseFacade PokedexFacade = new DatabaseFacade(new DatabaseHandler());
            DatabaseFacade APICacheFacade = new DatabaseFacade(new APIAccess());
            APICacheFacade.EnsureCreated();
            PokedexFacade.EnsureCreated();
        }
    }

}
