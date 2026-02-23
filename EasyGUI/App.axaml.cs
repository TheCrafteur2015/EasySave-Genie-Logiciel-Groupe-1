using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using EasyGUI.Views;
using EasyGUI.ViewModels;
using System.Linq;

namespace EasyGUI
{
    /// <summary>
    /// Represents the entry point for the Avalonia application.
    /// Manages the application lifecycle and initialization of the main window.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the application by loading the XAML resources.
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Called when the framework initialization is completed.
        /// Configures the main window and sets up the ViewModel for the desktop application lifetime.
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Disables Avalonia's built-in data annotation validation.
        /// This is often necessary to prevent conflicts with custom validation logic or duplicates.
        /// </summary>
        private static void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}