using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCDataServices
{
    public class AppOptions
    {
        #region var
        private static readonly string OptionsFileName = "dcds_startup_options.json";
        private static bool OptionsLoaded = false;
        private static StartupOption startOptions = null;
        private static string PARAMETERS_DIRECTORY = "params";
        #endregion //var

        public static string ParameterDirectory
        {
            get
            {
                return PARAMETERS_DIRECTORY;
            }
            set
            {
                PARAMETERS_DIRECTORY = value;
            }
        }

        public static StartupOption GetAppOptions
        {
            get
            {
                if (startOptions == null)
                {
                    LoadOptions();
                }
                return startOptions;
            }
        }

        public static void LoadOptions()
        {
            if (!OptionsLoaded)
            {
                string options_path_fname = System.IO.Path.Combine(AppOptions.ParameterDirectory, OptionsFileName);
                if (System.IO.File.Exists(options_path_fname))
                {
                    try
                    {
                        string jsonstr = System.IO.File.ReadAllText(options_path_fname);
                        startOptions = System.Text.Json.JsonSerializer.Deserialize<StartupOption>(jsonstr);
                        OptionsLoaded = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error loading options {e.Message}");
                        OptionsLoaded = false;
                        startOptions = null;
                    }
                }
                else
                {
                    Console.WriteLine($"startup options file {options_path_fname} missing.");
                }
            }
        }

    }
}
