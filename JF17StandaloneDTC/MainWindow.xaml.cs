/******************************************
-----Welcome to the JF-17 Standalone DTC!-----
This program will enable you to configure the DCS JF-17 by Deka outside of DCS.
You will be able to configure radio presets, countermeasures, control settings, and more!
*******************************************/

/******************************************
-----Program Flow-----
User double-clicks the exe to start this program
The program launches
If there is a setup/ini file present the program will load the most recent settings
If there was not setup/ini file present the program will load blank
The user will first designate either their Config folder or JF-17 folder or both (undecided)
The program will then load the options.lua data if it is present. If it is not present then the user will be prompted to generate info
The user will then be able to adjust settings by clicking on the options on the screen
Decide if you want to use live updates as the user changes valiues and they are confirmed as "good"
If live updates are not used, the user will press an Export button to export their settings
After notification the program will then re-import the settings.
The user can either exit the porogram or continue to do another edit.
*******************************************/

/******************************************
TODO:
-have people try it out
-make video
-post on ed UserFiles
*******************************************/

/******************************************
Lessons Learned:
-Making the ~200 comms fields x3 (freq, label, and notes field) could have been smoother
-Think early in dev on how you should have the program save. It is possible to merge the different types.
-Think early in dev on how you want to handle the error/"user dont do that" conditions. They cany be merged.
*******************************************/

/******************************************
Improvements:
-Clean up Assets and References folder
-properly format the code to standards so that it looks nice
-prevent the yellow box on the help page from moving with the text
-Create a custom Windows border
-Integrate woth other aircraft DTC (kiowa?)
-implement a check to make sure the jf 17 is in the options lua. (code has been started)
*******************************************/

/******************************************
Version Tracker:
v1.0
-Initial Release
*******************************************/

/******************************************
https://github.com/rstarkov/LsonLib
*******************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LsonLib;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;

namespace JF17StandaloneDTC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string path_optionslua;
        string path_jf17DocFolder;
        string path_customerRadioLua;
        string path_musicFolder;
        string path_jf17Folder;
        string path_optionsluaBackup;
        string path_customerRadioLuaBackup;
        string appPath = System.AppDomain.CurrentDomain.BaseDirectory;//gets the path of were the utility is running
        string appName = "JF17StandaloneDTC";
        string settingsFile;
        string settingsFolder;
        string path_optionsluaSettingsLoad;
        string path_jf17DocFolderSettingsLoad;

        bool isPathOptionsLuaSet;
        bool isPathJf17FolderSet;

        Dictionary<int, decimal> dictionary_comms = new Dictionary<int, decimal>();//in this dictionary the channel is the first value, with the frequency being the second (properly formated)
        Dictionary<int, string> dictionary_notes = new Dictionary<int, string>();//in this dictionary the channel is the first value, with the notes being the second (properly formated)

        //https://stackoverflow.com/questions/12727491/programmatically-set-textblock-foreground-color/12727632
        //label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color) ColorConverter.ConvertFromString("#0da04c")); //green
        //label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color) ColorConverter.ConvertFromString("#f40000")); //red

        public MainWindow()
        {
            InitializeComponent();

            settingsFile = appPath + appName + @"/Settings.txt";
            settingsFolder = appPath + appName;

            textBlock_helpText1.Text = "Welcome to the JF-17 Standalone DTC by Bailey! This program is designed to give you the ability to modify the DTC features of the DCS JF-17 by Deka while outside of the game in an easy fashion. You can use this utility both while DCS is or is not running.";
            textBlock_helpText2.Text = "*****This program creates and modifies files and folders on your computer. If you are not comfortable with this, do not use this program. All changes that this program makes are reversable.*****";
            

            textBlock_helpText.Text = 
                "Instructions:\n" +
                "1. Click the 'Select options.lua' button. Select your options.lua, most likely located at 'C:\\Users\\<PROFILE>\\Saved Games\\DCS\\Config\\options.lua'.\n" +
                "2. Click the 'Select JF17 Doc' button. Select the JF-17 Doc folder, most likely located at 'C:\\DcsInstallLocation\\DCS World OpenBeta\\Mods\\aircraft\\JF-17\\Doc'.\n" +
                "3. Click the 'Import' button to import DTC data.\n" +
                "4. Modify the settings of the DTC by using the tabs at the top of the DTC window.\n" +
                "5. Click the 'Export' button to export your JF-17 DTC data to DCS. All of your settings will be saved including the file/folder locations. You will not have to do steps 1, 2, or 3 the next time you use this utility.\n" +
                "6. Use the Backup buttons to save and load backups in the directory of this utility. These are helpfull after DCS updates, for example.\n" +
                "\n" +
                "Thank you for using this utility and thank you to Deka for an amazing DCS module. If you have any comments, concerns, suggestions, or just wanna say 'Hi', feel free to contact me on Discord: Bailey#6230.\n" +
                "Enjoy!\n" +
                "~Bailey SEP2021\n";

            label_status.Text = System.DateTime.Now + " : " + "Welcome!!!";
            label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));
            
            if (File.Exists(settingsFile))//if the safe file exists, load it
            {
                var savedSettings = LsonVars.Parse(File.ReadAllText(settingsFile));//put the contents of the settings file into a lua read

                string path_optionsluaForLoad = savedSettings[appName]["userOptionsLuaLocation"].GetString();
                string path_jf17DocFolderForLoad = savedSettings[appName]["userJF17DocLocation"].GetString();

                //this is for loading. gotta "decode" the paths for lua format reasons
                path_optionslua = path_optionsluaForLoad.Replace('|', '\\');
                path_jf17DocFolder = path_jf17DocFolderForLoad.Replace('|', '\\');

                Console.WriteLine("Loaded options lua path as " + path_optionslua);
                isPathOptionsLuaSet = true;
                Console.WriteLine("Loaded jf17 doc folder as " + path_jf17DocFolder);
                isPathJf17FolderSet = true;
                path_customerRadioLua = Path.Combine(path_jf17DocFolder, @"customerRadio.lua");
                path_jf17Folder = Directory.GetParent(path_jf17DocFolder).ToString();
                path_musicFolder = Path.Combine(path_jf17Folder, @"Sounds\sdef\Cockpit\DPlayer");

                Button_import_Clicked();
            }

            //code to code the code so the code cand be coded without having to code
            ////https://stackoverflow.com/questions/54941568/programaticly-add-multiple-labels-to-wpf-grid
            //for (int i = 0; i < 200; i++)
            //{
            //    Label lbl = new Label { Foreground = Brushes.White, HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center, 
            //        Background = Brushes.LightBlue, Content = "name" + i, Height = 20, Width = 320, Name = "_" + i };
            //    lbl.SetValue(Grid.RowProperty, i);
            //    this.LayoutRoot.Children.Add(lbl);

            //}
            //int j = 5;
            //for (int i = 32; i < 200; i++)  // output:
            //{
            //    j++;
            //    Console.WriteLine("<TextBox x:Name = \"ch" + i + "_notes\" Grid.Row = \"" + j + "\" Grid.Column = \"2\" Height = \"19\" HorizontalAlignment = \"Stretch\" />");
            //    i++;
            //    Console.WriteLine("<TextBox x:Name = \"ch" + i + "_notes\" Grid.Row = \"" + j + "\" Grid.Column = \"5\" Height = \"19\" HorizontalAlignment = \"Stretch\" />");
            //}

            //<xctk:DecimalUpDown x:Name="ch31_freq" Grid.Row="5" Grid.Column="4" DefaultValue="100.000" Width="65" Height="19" Minimum="100" Maximum="432.750" Increment="0.5" HorizontalAlignment = "Left" ParsingNumberStyle = "AllowDecimalPoint" DisplayDefaultValueOnEmptyText = "True" MaxLength = "7" AutoSelectBehavior = "Never" TextAlignment = "Center" />

            //int j = 40;
            //double freq = 224.000;
            //for (int i = 100; i < 201; i++)  // output:
            //{

            //    Console.WriteLine("<xctk:DecimalUpDown x:Name=\"ch" + i + "_freq\" Grid.Row=\"" + j + "\" Grid.Column=\"1\""   + " DefaultValue=\"" + freq + "\" Width=\"65\" Height=\"19\" Minimum=\"100\" Maximum=\"432.750\" Increment=\"0.5\" HorizontalAlignment=\"Left\" ParsingNumberStyle=\"AllowDecimalPoint\" DisplayDefaultValueOnEmptyText=\"True\" MaxLength=\"7\" AutoSelectBehavior=\"Never\" TextAlignment=\"Center\" />");
            //    i++;
            //    freq = freq + 1.000;
            //    Console.WriteLine("<xctk:DecimalUpDown x:Name=\"ch" + i + "_freq\" Grid.Row=\"" + j + "\" Grid.Column=\"4\""   + " DefaultValue=\"" + freq + "\" Width=\"65\" Height=\"19\" Minimum=\"100\" Maximum=\"432.750\" Increment=\"0.5\" HorizontalAlignment=\"Left\" ParsingNumberStyle=\"AllowDecimalPoint\" DisplayDefaultValueOnEmptyText=\"True\" MaxLength=\"7\" AutoSelectBehavior=\"Never\" TextAlignment=\"Center\" />");
            //    j++;
            //    freq = freq + 1.000;
            //}
        }

        private void button_selectFile_Click(object sender, RoutedEventArgs e)
        {
            //https://docs.microsoft.com/en-us/dotnet/api/system.io.path.combine?view=net-5.0
            //https://stackoverflow.com/questions/1140383/how-can-i-get-the-current-user-directory
            //https://wpf-tutorial.com/dialogs/the-openfiledialog/
            //when the user clicks this button they will be prompted to pick their options.lua file
            //which is located at C:\Users\<PROFILE>\Saved Games\DCS\Config\options.lua
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Lua files (*.lua)|*.lua";//fliters only for .lua files
            openFileDialog.Title = ("Hint: C:\\Users\\<PROFILE>\\Saved Games\\DCS\\Config\\options.lua");//gives the user a hint to where the file is located

            //make some strings that contain educated guesses to where the file is located
            string[] paths1 = { System.Environment.GetEnvironmentVariable("USERPROFILE"), "Saved Games", "DCS.openbeta", "Config"};
            string fullPath1 = Path.Combine(paths1);

            string[] paths2 = { System.Environment.GetEnvironmentVariable("USERPROFILE"), "Saved Games", "DCS", "Config"};
            string fullPath2 = Path.Combine(paths2);

            if (File.Exists(fullPath2))
            {
                openFileDialog.InitialDirectory = fullPath2;//opens the dialog at what may be the path of the file
            }
            else
            {
                openFileDialog.InitialDirectory = fullPath1;//opens the dialog at what may be the path of the file
            }

            if (openFileDialog.ShowDialog() == true)//if something was selected then
                if (openFileDialog.FileName.Contains("options.lua"))//if the name of the file is options.lua, it is likely the file we are looking for
                {
                    //the correct file was selected. populate the field
                    //MessageBox.Show("You selected: " + openFileDialog.FileName);//debug
                    isPathOptionsLuaSet = true;
                    path_optionslua = openFileDialog.FileName;
                    label_status.Text = System.DateTime.Now + " : " + "options.lua selected";
                }
                else //the file selected was not the one we are looking for
                { 
                    isPathOptionsLuaSet = false;
                    MessageBox.Show("You selected: '" + openFileDialog.FileName + "'\nThe file you selected does not seem to be correct. Please try selecting 'options.lua' again.");
                    label_status.Text = System.DateTime.Now + " : " + "options.lua not selected";
                    label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
                }
        }

        private void button_selectFile2_Click(object sender, RoutedEventArgs e)
        {
            //when the user clicks this button they will be prompted to pick their JF17 FOLDER
            //which is located at DcsInstalLocation\DCS World\Mods\aircraft\JF-17\Doc
            //https://stackoverflow.com/questions/1922204/open-directory-dialog
            //https://github.com/ookii-dialogs/ookii-dialogs-wpf
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            dialog.UseDescriptionForTitle = true;
            dialog.Description = ("Hint: DcsInstalLocation\\DCS World\\Mods\\aircraft\\JF-17\\Doc");//gives the user a hint to where the file is located

            //****************************************//
            dialog.SelectedPath = @"C:\Program Files\DCS World OpenBeta\Mods\aircraft\JF-17\Doc";//try
            //****************************************//

            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                if (dialog.SelectedPath.Contains(@"Mods\aircraft\JF-17\Doc"))
                {
                    //the correct folder was selected. populate the field
                    isPathJf17FolderSet = true;
                    //MessageBox.Show("You selected: " + dialog.SelectedPath);//debug
                    path_jf17DocFolder = dialog.SelectedPath;
                    path_customerRadioLua = Path.Combine(path_jf17DocFolder, @"customerRadio.lua");
                    
                    path_jf17Folder = Directory.GetParent(path_jf17DocFolder).ToString();
                    path_musicFolder = Path.Combine(path_jf17Folder, @"Sounds\sdef\Cockpit\DPlayer");
                    //MessageBox.Show("Your radio presets will be exported to: " + path_customerRadioLua);//NOT debug. show this

                    //debug
                    Console.WriteLine("path_jf17DocFolder = " + path_jf17DocFolder);
                    Console.WriteLine("path_customerRadioLua = " + path_customerRadioLua);
                    Console.WriteLine("path_jf17Folder = " + path_jf17Folder);
                    Console.WriteLine("path_musicFolder = " + path_musicFolder);
                    label_status.Text = System.DateTime.Now + " : " + "JF-17 Doc selected";
                    label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));
                }
                else
                {
                    isPathJf17FolderSet = false;
                    MessageBox.Show("You selected: '" + dialog.SelectedPath + "'\nThe folder you selected does not seem to be correct. Please try selecting the JF-17 Doc folder again.");
                    path_jf17DocFolder = null;//null it so ppl dont try somehting funny after being told that they did it wrong
                    path_customerRadioLua = null;//null it so ppl dont try somehting funny after being told that they did it wrong
                    label_status.Text = System.DateTime.Now + " : " + "JF-17 Doc not selected";
                    label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
                }

            }

        }



        private void button_import_Click(object sender, RoutedEventArgs e)
        {
            //clicking this button will load the data from the two locations into 
            //the respective fields, assuming that both locations have been set by the user
            Button_import_Clicked();
            
        }

        private void Button_import_Clicked()
        {
            
                //clicking this button will load the data from the two locations into 
                //the respective fields, assuming that both locations have been set by the user
                if (isPathOptionsLuaSet == true && isPathJf17FolderSet == true)//both have been set by the user
                {
                    //https://stackoverflow.com/questions/881445/easiest-way-to-parse-a-lua-datastructure-in-c-sharp-net

                    var optionsLuaParse = LsonVars.Parse(File.ReadAllText(path_optionslua));//load the options.lua into memory

                    //TODO implement a check to make sure the jf 17 is in the options lua. 
                    //if (optionsLuaParse["options"]["plugins"]["JF-17"] == null)
                    //{
                    //    MessageBox.Show("There isnt a JF17 in the file");
                    //}
                    //else
                    //{
                    //    MessageBox.Show("There is a JF17 in the file");
                    //}

                    LoadCommsPage();


                    //start loading!!!!
                    //all decimalupDown boxe should be GetDecimal()
                    //all "selectedIndex" should be GetInt()
                    //all true/false should be GetBool()

                    //MessageBox.Show(optionsLuaParse["options"]["plugins"]["JF-17"]["CHAFFBINGO"].GetInt().ToString());//debug
                    general_AntZoomInv.IsChecked = optionsLuaParse["options"]["plugins"]["JF-17"]["AntZoomInv"].GetBool();
                    cm_CHAFFBINGO.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["CHAFFBINGO"].GetInt().ToString();
                    //skip the cockpit versions

                    if (optionsLuaParse["options"]["plugins"]["JF-17"]["CPTModel"].GetString().Equals("JF-17-CPT-PERF1"))//kinda odd, but go with it.
                    {
                        general_CPTModel.SelectedIndex = 1;
                    }
                    else
                    {
                        general_CPTModel.SelectedIndex = 0;
                    }

                    controls_DLPOD_TDC.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["DLPOD_TDC"].GetDecimal().ToString();
                    general_DMAPTYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["DMAPTYPE"].GetInt();
                    cm_FLAREBINGO.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["FLAREBINGO"].GetInt().ToString();
                    general_GUNLIMIT.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["GUNLIMIT"].GetInt();
                    general_GUNSIGHT.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["GUNSIGHT"].GetInt();


                    controls_HUD_TDC.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["HUD_TDC"].GetDecimal().ToString();
                    general_HiddenStick.IsChecked = optionsLuaParse["options"]["plugins"]["JF-17"]["HiddenStick"].GetBool();
                    general_IcingOnCake.IsChecked = optionsLuaParse["options"]["plugins"]["JF-17"]["IcingOnCake"].GetBool();
                    general_KYBDPITCH.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["KYBDPITCH"].GetInt();
                    general_MUSICNUM_SLIDER.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["MUSICNUM_SLIDER"].GetDecimal().ToString();

                    //****See Countermeasure stuff after general_ and controls_ sections*****//

                    controls_RDR_ELEV.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["RDR_ELEV"].GetDecimal().ToString();
                    controls_RDR_TDC.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["RDR_TDC"].GetDecimal().ToString();
                    general_RemoveProbe.IsChecked = optionsLuaParse["options"]["plugins"]["JF-17"]["RemoveProbe"].GetBool();
                    controls_TDC_DEADZONE.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["TDC_DEADZONE"].GetDecimal().ToString();

                    controls_TVIR_TDC.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["TVIR_TDC"].GetDecimal().ToString();
                    general_TestingChg.IsChecked = optionsLuaParse["options"]["plugins"]["JF-17"]["TestingChg"].GetBool();
                    general_VOICELOCALE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["VOICELOCALE"].GetInt();
                    controls_WMD_TDC.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["WMD_TDC"].GetDecimal().ToString();



                    /***************************
                    ["PROG01CHBI"] = 0, --Chaff BI. [Index] = [value]; 0 = 0.0; 1 = 0.2; 2 = 0.5; 4 = 0.75; 5 = 1.0 (it skips 3 for some reason)
                    ["PROG01CHI"] = 3, --Chalf SI. [Index] = [value]; 0 = 0.2; 1 = 0.5; 2 = 1; 3 = 2; 4 = 3; 5 = 4.5; 6 = 5
                    ["PROG01CHN"] = 4, --Chaff BQ. the absolute value indicated in the menu 1-6
                    ["PROG01CHR"] = 3, --Chaff SQ. the absolute value indicated in the menu 1-4

                    ["PROG01FLBI"] = 3, --Flare BI. [Index] = [value]; 0 = 0.0; 1 = 0.2; 2 = 0.5; 3 = 0.75; 4 = 1.0
                    ["PROG01FLI"] = 2, --Flare SI. [Index] = [value]; 0 = 0.2; 1 = 0.5; 2 = 1; 3 = 2; 4 = 3; 5 = 4.5; 6 = 5
                    ["PROG01FLN"] = 1, --Flare BQ. the absolute value indicated in the menu 1-6
                    ["PROG01FLR"] = 1, --Flare SQ. the absolute value indicated in the menu 1-4

                    ["PROG01TYPE"] = 0, --0 = CH; 1 = FL; 2 = CH+FL
                    ******************/

                    switch (optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01CHBI"].GetInt())
                    {
                        case 0:
                            cm_PROG01CHBI.SelectedIndex = 0;
                            break;
                        case 1:
                            cm_PROG01CHBI.SelectedIndex = 1;
                            break;
                        case 2:
                            cm_PROG01CHBI.SelectedIndex = 2;
                            break;
                        //case 3: //does not exist for some odd reason. Tell Deka
                        //    cm_PROG01CHBI.SelectedIndex = 3;
                        //    break;
                        case 4:
                            cm_PROG01CHBI.SelectedIndex = 3;
                            break;
                        case 5:
                            cm_PROG01CHBI.SelectedIndex = 4;
                            break;
                    }

                    cm_PROG01CHI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01CHI"].GetInt();
                    cm_PROG01CHN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01CHN"].GetInt() - 1;
                    cm_PROG01CHR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01CHR"].GetInt() - 1;

                    cm_PROG01FLBI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01FLBI"].GetInt();
                    cm_PROG01FLI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01FLI"].GetInt();
                    cm_PROG01FLN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01FLN"].GetInt() - 1;
                    cm_PROG01FLR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01FLR"].GetInt() - 1;

                    cm_PROG01TYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01TYPE"].GetInt();


                    switch (optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02CHBI"].GetInt())
                    {
                        case 0:
                            cm_PROG02CHBI.SelectedIndex = 0;
                            break;
                        case 1:
                            cm_PROG02CHBI.SelectedIndex = 1;
                            break;
                        case 2:
                            cm_PROG02CHBI.SelectedIndex = 2;
                            break;
                        //case 3: //does not exist for some odd reason. Tell Deka
                        //    cm_PROG02CHBI.SelectedIndex = 3;
                        //    break;
                        case 4:
                            cm_PROG02CHBI.SelectedIndex = 3;
                            break;
                        case 5:
                            cm_PROG02CHBI.SelectedIndex = 4;
                            break;
                    }

                    cm_PROG02CHI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02CHI"].GetInt();
                    cm_PROG02CHN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02CHN"].GetInt() - 1;
                    cm_PROG02CHR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02CHR"].GetInt() - 1;

                    cm_PROG02FLBI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02FLBI"].GetInt();
                    cm_PROG02FLI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02FLI"].GetInt();
                    cm_PROG02FLN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02FLN"].GetInt() - 1;
                    cm_PROG02FLR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02FLR"].GetInt() - 1;

                    cm_PROG02TYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02TYPE"].GetInt();



                    switch (optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03CHBI"].GetInt())
                    {
                        case 0:
                            cm_PROG03CHBI.SelectedIndex = 0;
                            break;
                        case 1:
                            cm_PROG03CHBI.SelectedIndex = 1;
                            break;
                        case 2:
                            cm_PROG03CHBI.SelectedIndex = 2;
                            break;
                        //case 3: //does not exist for some odd reason. Tell Deka
                        //    cm_PROG03CHBI.SelectedIndex = 3;
                        //    break;
                        case 4:
                            cm_PROG03CHBI.SelectedIndex = 3;
                            break;
                        case 5:
                            cm_PROG03CHBI.SelectedIndex = 4;
                            break;
                    }

                    cm_PROG03CHI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03CHI"].GetInt();
                    cm_PROG03CHN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03CHN"].GetInt() - 1;
                    cm_PROG03CHR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03CHR"].GetInt() - 1;

                    cm_PROG03FLBI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03FLBI"].GetInt();
                    cm_PROG03FLI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03FLI"].GetInt();
                    cm_PROG03FLN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03FLN"].GetInt() - 1;
                    cm_PROG03FLR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03FLR"].GetInt() - 1;

                    cm_PROG03TYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03TYPE"].GetInt();




                    switch (optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04CHBI"].GetInt())
                    {
                        case 0:
                            cm_PROG04CHBI.SelectedIndex = 0;
                            break;
                        case 1:
                            cm_PROG04CHBI.SelectedIndex = 1;
                            break;
                        case 2:
                            cm_PROG04CHBI.SelectedIndex = 2;
                            break;
                        //case 3: //does not exist for some odd reason. Tell Deka
                        //    cm_PROG04CHBI.SelectedIndex = 3;
                        //    break;
                        case 4:
                            cm_PROG04CHBI.SelectedIndex = 3;
                            break;
                        case 5:
                            cm_PROG04CHBI.SelectedIndex = 4;
                            break;
                    }

                    cm_PROG04CHI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04CHI"].GetInt();
                    cm_PROG04CHN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04CHN"].GetInt() - 1;
                    cm_PROG04CHR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04CHR"].GetInt() - 1;

                    cm_PROG04FLBI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04FLBI"].GetInt();
                    cm_PROG04FLI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04FLI"].GetInt();
                    cm_PROG04FLN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04FLN"].GetInt() - 1;
                    cm_PROG04FLR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04FLR"].GetInt() - 1;

                    cm_PROG04TYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04TYPE"].GetInt();




                    switch (optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05CHBI"].GetInt())
                    {
                        case 0:
                            cm_PROG05CHBI.SelectedIndex = 0;
                            break;
                        case 1:
                            cm_PROG05CHBI.SelectedIndex = 1;
                            break;
                        case 2:
                            cm_PROG05CHBI.SelectedIndex = 2;
                            break;
                        //case 3: //does not exist for some odd reason. Tell Deka
                        //    cm_PROG05CHBI.SelectedIndex = 3;
                        //    break;
                        case 4:
                            cm_PROG05CHBI.SelectedIndex = 3;
                            break;
                        case 5:
                            cm_PROG05CHBI.SelectedIndex = 4;
                            break;
                    }

                    cm_PROG05CHI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05CHI"].GetInt();
                    cm_PROG05CHN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05CHN"].GetInt() - 1;
                    cm_PROG05CHR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05CHR"].GetInt() - 1;

                    cm_PROG05FLBI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05FLBI"].GetInt();
                    cm_PROG05FLI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05FLI"].GetInt();
                    cm_PROG05FLN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05FLN"].GetInt() - 1;
                    cm_PROG05FLR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05FLR"].GetInt() - 1;

                    cm_PROG05TYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05TYPE"].GetInt();

                label_status.Text = System.DateTime.Now + " : " + "Import success!!!";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));

            }
                else if (isPathOptionsLuaSet == true && isPathJf17FolderSet == false)//only the options lua was set
                {
                    MessageBox.Show("You have not selected your JF-17 Doc folder. Select your JF-17 Doc folder.");
                label_status.Text = System.DateTime.Now + " : " + "JF-17 Doc not selected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));

            }
                else if (isPathOptionsLuaSet == false && isPathJf17FolderSet == true)//only the docs folder was set
                {
                    MessageBox.Show("You have not selected your options.lua file. Select your options.lua file.");
                label_status.Text = System.DateTime.Now + " : " + "options.lua not selected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
            }
                else//nothing was set
                {
                    MessageBox.Show("You have not selected your options.lua file and JF-17 Doc folder. Select your options.lua file and JF-17 Doc folder.");
                label_status.Text = System.DateTime.Now + " : " + "files not selected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
            }
            
        }

        private void LoadCommsPage()
        {
            if (File.Exists(path_customerRadioLua))//load the customerRadio.lua into memory if it exists
            {
                //MessageBox.Show(path_customerRadioLua + " loaded.");
                //var customerRadioLuaParse = LsonVars.Parse(File.ReadAllText(path_customerRadioLua));//cant use this bc it isnt lua
                //https://www.c-sharpcorner.com/UploadFile/mahesh/how-to-read-a-text-file-in-C-Sharp/
                string[] customerRadioParse = File.ReadAllLines(path_customerRadioLua);
                foreach (string radioPresetLine in customerRadioParse)
                {
                    //Console.WriteLine(radioPresetLine);
                    if (radioPresetLine.Contains('='))//could also use "presets["?
                    {

                        //https://stackoverflow.com/questions/378415/how-do-i-extract-text-that-lies-between-parentheses-round-brackets
                        int presetNumber = Convert.ToInt32(radioPresetLine.Split('[', ']')[1]);//this is explained in the above link
                     //https://stackoverflow.com/questions/14998595/need-to-get-a-string-after-a-word-in-a-string-in-c-sharp/14998640
                        string toBeSearched = "=";
                        //string frequencyNumber = radioPresetLine.Substring(radioPresetLine.IndexOf(toBeSearched) + toBeSearched.Length);
                        //Console.WriteLine("Index of toBeSearched: "+radioPresetLine.IndexOf(toBeSearched));

                       //Console.WriteLine(radioPresetLine.Substring(radioPresetLine.IndexOf(toBeSearched) + 2, 9));//debug
                        decimal frequencyNumber = Convert.ToDecimal(radioPresetLine.Substring(radioPresetLine.IndexOf(toBeSearched) + 2,9));//the freqs are 9 numbers long, normally
                        frequencyNumber = (frequencyNumber / 1000000);//this is the converter for a human-readable freq
                    //MessageBox.Show("Channel " + presetNumber + ": Freq "+ frequencyNumber);//debug
                                                                                            //put the number in the correct box in the gui
                                                                                            //ch22_freq
                        ///("ch"+presetNumber+"_freq").Value = frequencyNumber;//uhh, good luck
                        //dictionary_comms.Add(presetNumber, frequencyNumber);//this does not allow re-writes on a second load
                        dictionary_comms[presetNumber] = frequencyNumber;//this allows previous values to be written over

                        //*****Get the notes*****//
                        if (radioPresetLine.Contains("--"))
                        {
                            string toBeSearchedNotes = "--";
                            //TODO clean this up so that it make sense
                            //Console.WriteLine("Note Length: " +   (radioPresetLine.Length)  + " - " + radioPresetLine.IndexOf(toBeSearchedNotes) + " = " +  (radioPresetLine.Length - radioPresetLine.IndexOf(toBeSearchedNotes) - 2));
                            string frequencyNotes = radioPresetLine.Substring(radioPresetLine.IndexOf(toBeSearchedNotes) + toBeSearchedNotes.Length , (radioPresetLine.Length - radioPresetLine.IndexOf(toBeSearchedNotes)) - 2);
                            dictionary_notes[presetNumber] = frequencyNotes;//this allows previous values to be written over
                            //Console.WriteLine("Notes: " + frequencyNotes);
                            

                        }
                    }
                }
                //https://stackoverflow.com/questions/28552603/stringify-key-value-pairs-in-dictionary
                //debug
                //foreach (KeyValuePair<int, string> kvp in dictionary_notes)
                //{
                //    //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                //    Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                //}
                LoadCommChannelsAndFreqs();
            }
            else//the file was not found or has not yet been made. Either generate one now, or rely on the WPF presets
            {
                //System.Windows.MessageBox.Show("File does not exist. Reseting to baseline. Export will generate a new file.");
            }
        }

        private void LoadCommChannelsAndFreqs()
        {
            //https://stackoverflow.com/questions/40520050/wpf-foreach-textbox-loop/40522342
            //this gets the list of the text boxes in the var somehow. I dont quite understand, but it works. (thank goodness)
            //load notes
            var commNoteBoxes = CommsGrid.Children.OfType<TextBox>().Where(x => x.Name.Contains("ch"));
            int keyCounterForTheNthPreset = 22;//the presets start at channel 22
            foreach (TextBox textInTheBox in commNoteBoxes)
            {
                if (dictionary_notes.ContainsKey(keyCounterForTheNthPreset))
                {
                    textInTheBox.Text = dictionary_notes[keyCounterForTheNthPreset];
                }
                else //dont do anything bc the dictionary entry wasnt there
                { 
                }
                ++keyCounterForTheNthPreset;
            }

            //load freqs
            var commFreqBoxes = CommsGrid.Children.OfType<DecimalUpDown>().Where(x => x.Name.Contains("ch"));
            keyCounterForTheNthPreset = 22;//the presets start at channel 22. reset because of the previous foreach above
            foreach (DecimalUpDown textInTheDecimalUpDown in commFreqBoxes)
            {
                if (dictionary_comms.ContainsKey(keyCounterForTheNthPreset))
                {
                    textInTheDecimalUpDown.Value = dictionary_comms[keyCounterForTheNthPreset];
                }
                else //dont do anything bc the dictionary entry wasnt there
                {
                }
                ++keyCounterForTheNthPreset;
            }
            //https://www.geeksforgeeks.org/c-sharp-dictionary-containsvalue-method/
        }

        private void button_export_Click(object sender, RoutedEventArgs e)
        {
            //clicking this button will export the data in all of the fields in the DTC to the 
            //respective files: Options.lua and CustomerRadio.lua.
            //Using this button will enact the "save fold locations" command at the end (so that if there is an error, it would not have saved the bad stuff)

           

            if (isPathOptionsLuaSet == true && isPathJf17FolderSet == true)//both have been set by the user
            {
                SaveUserDirectories();
                //*****Export Options Lua Section*****//
                var optionsLuaParseforLoad = LsonVars.Parse(File.ReadAllText(path_optionslua));//load the options.lua into memory

                

               
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["AntZoomInv"] = general_AntZoomInv.IsChecked;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["CHAFFBINGO"] = cm_FLAREBINGO.Value;
                //skip the cockpit versions

                
                if (general_CPTModel.SelectedIndex.Equals(1))//kinda odd, but go with it. -- JF-17-CPT or JF-17-CPT-PERF1
                {
                    //general_CPTModel.SelectedIndex = 1;
                    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["CPTModel"] = ("JF-17-CPT-PERF1");
                }
                else
                {
                    //general_CPTModel.SelectedIndex = 0;
                    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["CPTModel"] = ("JF-17-CPT");
                }

                
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["DLPOD_TDC"] = controls_DLPOD_TDC.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["DMAPTYPE"] = general_DMAPTYPE.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["FLAREBINGO"] = cm_FLAREBINGO.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["GUNLIMIT"] = general_GUNLIMIT.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["GUNSIGHT"] = general_GUNSIGHT.SelectedIndex;

                
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["HUD_TDC"] = controls_HUD_TDC.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["HiddenStick"] = general_HiddenStick.IsChecked;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["IcingOnCake"] = general_IcingOnCake.IsChecked;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["KYBDPITCH"] = general_KYBDPITCH.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["MUSICNUM_SLIDER"] = general_MUSICNUM_SLIDER.Value;
                
                
                //****See Countermeasure stuff after general_ and controls_ sections*****//
                
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["RDR_ELEV"] = controls_RDR_ELEV.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["RDR_TDC"] = controls_RDR_TDC.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["RemoveProbe"] = general_RemoveProbe.IsChecked;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["TDC_DEADZONE"] = controls_TDC_DEADZONE.Value;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["TVIR_TDC"] = controls_TVIR_TDC.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["TestingChg"] = general_TestingChg.IsChecked;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["VOICELOCALE"] = general_VOICELOCALE.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["WMD_TDC"] = controls_WMD_TDC.Value;

                //*****Countermeasures Export*****//

                switch (cm_PROG01CHBI.SelectedIndex)
                {
                    case 0:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 0;
                        break;
                    case 1:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 1;
                        break;
                    case 2:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 2;
                        break;
                    case 3: 
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 4;
                        break;
                    case 4:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 5;
                        break;
                    //case 5: //cant happen because there arent this many entries. Tell Deka
                    //    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 5;
                    //    break;
                }

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHI"] = cm_PROG01CHI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHN"] = cm_PROG01CHN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHR"] = cm_PROG01CHR.SelectedIndex + 1;

                
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01FLBI"] = cm_PROG01FLBI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01FLI"] = cm_PROG01FLI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01FLN"] = cm_PROG01FLN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01FLR"] = cm_PROG01FLR.SelectedIndex + 1;
                
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01TYPE"] = cm_PROG01TYPE.SelectedIndex;



                switch (cm_PROG02CHBI.SelectedIndex)
                {
                    case 0:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 0;
                        break;
                    case 1:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 1;
                        break;
                    case 2:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 2;
                        break;
                    case 3:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 4;
                        break;
                    case 4:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 5;
                        break;
                        //case 5: //cant happen because there arent this many entries. Tell Deka
                        //    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 5;
                        //    break;
                }

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHI"] = cm_PROG02CHI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHN"] = cm_PROG02CHN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHR"] = cm_PROG02CHR.SelectedIndex + 1;


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02FLBI"] = cm_PROG02FLBI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02FLI"] = cm_PROG02FLI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02FLN"] = cm_PROG02FLN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02FLR"] = cm_PROG02FLR.SelectedIndex + 1;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02TYPE"] = cm_PROG02TYPE.SelectedIndex;


                switch (cm_PROG03CHBI.SelectedIndex)
                {
                    case 0:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 0;
                        break;
                    case 1:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 1;
                        break;
                    case 2:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 2;
                        break;
                    case 3:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 4;
                        break;
                    case 4:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 5;
                        break;
                        //case 5: //cant happen because there arent this many entries. Tell Deka
                        //    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 5;
                        //    break;
                }

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHI"] = cm_PROG03CHI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHN"] = cm_PROG03CHN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHR"] = cm_PROG03CHR.SelectedIndex + 1;


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03FLBI"] = cm_PROG03FLBI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03FLI"] = cm_PROG03FLI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03FLN"] = cm_PROG03FLN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03FLR"] = cm_PROG03FLR.SelectedIndex + 1;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03TYPE"] = cm_PROG03TYPE.SelectedIndex;



                switch (cm_PROG04CHBI.SelectedIndex)
                {
                    case 0:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 0;
                        break;
                    case 1:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 1;
                        break;
                    case 2:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 2;
                        break;
                    case 3:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 4;
                        break;
                    case 4:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 5;
                        break;
                        //case 5: //cant happen because there arent this many entries. Tell Deka
                        //    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 5;
                        //    break;
                }

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHI"] = cm_PROG04CHI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHN"] = cm_PROG04CHN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHR"] = cm_PROG04CHR.SelectedIndex + 1;


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04FLBI"] = cm_PROG04FLBI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04FLI"] = cm_PROG04FLI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04FLN"] = cm_PROG04FLN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04FLR"] = cm_PROG04FLR.SelectedIndex + 1;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04TYPE"] = cm_PROG04TYPE.SelectedIndex;


                switch (cm_PROG05CHBI.SelectedIndex)
                {
                    case 0:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 0;
                        break;
                    case 1:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 1;
                        break;
                    case 2:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 2;
                        break;
                    case 3:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 4;
                        break;
                    case 4:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 5;
                        break;
                        //case 5: //cant happen because there arent this many entries. Tell Deka
                        //    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 5;
                        //    break;
                }

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHI"] = cm_PROG05CHI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHN"] = cm_PROG05CHN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHR"] = cm_PROG05CHR.SelectedIndex + 1;


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05FLBI"] = cm_PROG05FLBI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05FLI"] = cm_PROG05FLI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05FLN"] = cm_PROG05FLN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05FLR"] = cm_PROG05FLR.SelectedIndex + 1;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05TYPE"] = cm_PROG05TYPE.SelectedIndex;

                //*****Countermeasures Export End*****//

                //the actual export
                File.WriteAllText(path_optionslua, LsonVars.ToString(optionsLuaParseforLoad)); // serialize back to a file

                //*****Comms Export Begin*****//

                //foreach (var item in dictionary_comms)
                //{
                //    foo(item.Key);
                //    bar(item.Value);
                //}

                //put the notes in the dictionary

                var commNoteBoxes = CommsGrid.Children.OfType<TextBox>().Where(x => x.Name.Contains("ch"));
                int keyCounterForTheNthPreset = 22;//the presets start at channel 22
                foreach (TextBox textInTheBox in commNoteBoxes)
                {
                    dictionary_notes[keyCounterForTheNthPreset] = textInTheBox.Text;//this allows previous values to be written over
                    ++keyCounterForTheNthPreset;
                }

                //put the freqs in the dictionary
                var commFreqBoxes = CommsGrid.Children.OfType<DecimalUpDown>().Where(x => x.Name.Contains("ch"));
                keyCounterForTheNthPreset = 22;//the presets start at channel 22. reset because of the previous foreach above
                foreach (DecimalUpDown textInTheDecimalUpDown in commFreqBoxes)
                {
                    dictionary_comms[keyCounterForTheNthPreset] = (decimal)textInTheDecimalUpDown.Value;//this allows previous values to be written over
                    ++keyCounterForTheNthPreset;
                }


                //debug
                //foreach (KeyValuePair<int, string> kvp in dictionary_notes)
                //{
                //    //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                //    Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                //}



                using (StreamWriter sw = new StreamWriter(path_customerRadioLua)) //path
                {
                    int currentNoteNumber = 22;
                    foreach (var commDictionaryEntry in dictionary_comms)//we use the comms one becuse we know that all the freqs are there
                    {
                        sw.WriteLine("presets[" + commDictionaryEntry.Key + "] = " + Convert.ToInt32(commDictionaryEntry.Value * 1000000) + " --" + dictionary_notes[currentNoteNumber]);//remember to add the notes
                        ++currentNoteNumber;
                    }
                    sw.WriteLine("");
                    sw.WriteLine("return presets");

                    sw.WriteLine("--Exported via JF17 Standalone DTC by Bailey " + System.DateTime.Now);

                }

                //*****Comms Export End*****//
                label_status.Text = System.DateTime.Now + " : " + "Export success!!!";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));
            }
            else if (isPathOptionsLuaSet == true && isPathJf17FolderSet == false)//only the options lua was set
            {
                MessageBox.Show("You have not selected your JF-17 Doc folder. Select your JF-17 Doc folder.");
                label_status.Text = System.DateTime.Now + " : " + "JF-17 Doc not selected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));

            }
            else if (isPathOptionsLuaSet == false && isPathJf17FolderSet == true)//only the docs folder was set
            {
                MessageBox.Show("You have not selected your options.lua file. Select your options.lua file.");
                label_status.Text = System.DateTime.Now + " : " + "options.lua not selected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
            }
            else//nothing was set
            {
                MessageBox.Show("You have not selected your options.lua file and JF-17 Doc folder. Select your options.lua file and JF-17 Doc folder.");
                label_status.Text = System.DateTime.Now + " : " + "files not selected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
            }
            //*****Export Options Lua Section End*****//

        }

        private void SaveUserDirectories()
        {
            
                Console.WriteLine("DEBUG: Saving settings file");

                Directory.CreateDirectory(appPath + appName);//creates the save folder
                                                             //https://docs.microsoft.com/en-us/dotnet/api/system.io.streamwriter?redirectedfrom=MSDN&view=netcore-3.1

                //this is for saving
                string path_optionsluaForSave = path_optionslua.Replace('\\', '|');
                string path_jf17DocFolderForSave = path_jf17DocFolder.Replace('\\', '|');


                Console.WriteLine(path_optionsluaForSave);
                Console.WriteLine(path_jf17DocFolderForSave);
                //write the following in the text file
                string[] defaultExportString = {
                appName + " = ",
                //"DcsMultiplayerChatColorPicker = ",
                "{",
                "   [\"userOptionsLuaLocation\"] = \"" + path_optionsluaForSave +"\",",
                "   [\"userJF17DocLocation\"] = \"" + path_jf17DocFolderForSave +"\"",
                "}",
            };
                System.IO.File.WriteAllLines(settingsFile, defaultExportString);
           
        }
        

        
        //cheat. do not publish it
        private void MainMenuLogo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //path_optionslua = @"C:\Users\%PROFILE%\Saved Games\DCS.openbeta\Config\options.lua";
            //path_jf17DocFolder = @"C:\DcsInstallLocation\DCS World OpenBeta\Mods\aircraft\JF-17\";
            //path_customerRadioLua = @"C:\DcsInstallLocation\DCS World OpenBeta\Mods\aircraft\JF-17\Doc\customerRadio.lua";
            //path_musicFolder = @"C:\DcsInstallLocation\DCS World OpenBeta\Mods\aircraft\JF-17\Sounds\sdef\Cockpit\DPlayer";
            //isPathJf17FolderSet = true;
            //isPathOptionsLuaSet = true;
            label_status.Text = System.DateTime.Now + " : " + "secret button pressed...";
            label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));
        }

        private void button_musicFolder_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(path_musicFolder);
            if (Directory.Exists(path_musicFolder))
            {
                Console.WriteLine("File exists");
                Process.Start(path_musicFolder);
                label_status.Text = System.DateTime.Now + " : " + "music folder opened";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));
            }
            else
            {
                MessageBox.Show("You have not selected your JF-17 Doc folder. Select your JF-17 Doc folder.");
                label_status.Text = System.DateTime.Now + " : " + "JF-17 Doc not selected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
            }
        }

        private void button_donate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.com/paypalme/asherao");
            label_status.Text = System.DateTime.Now + " : " + "Thank you!!!";
            label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));
        }

        private void button_moreMods_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.digitalcombatsimulator.com/en/files/filter/user-is-baileywa/apply/?PER_PAGE=100");
            label_status.Text = System.DateTime.Now + " : " + "Mods link clicked";
            label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));
        }

        private void button_discord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/d4VFhde49b");
            label_status.Text = System.DateTime.Now + " : " + "Welcome to Discord!!!";
            label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));
        }

        private void button_saveBackup_Click(object sender, RoutedEventArgs e)
        {
            //when this is clicked the prrogram will save the two files to a folder in the app directory
            Directory.CreateDirectory(appPath + appName);//creates the save folder
            string path_backupDirectory = Directory.CreateDirectory(appPath + appName).ToString();

            //the following code is almost literally doubled. think about optimising it. maybe.

            if (isPathOptionsLuaSet == true && isPathJf17FolderSet == true)//both have been set by the user. it "has" to be done bc it uses info from the options.lua file
                
            {
              
                //*****Export Options Lua Section*****//
                var optionsLuaParseforLoad = LsonVars.Parse(File.ReadAllText(path_optionslua));//load the options.lua into memory




                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["AntZoomInv"] = general_AntZoomInv.IsChecked;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["CHAFFBINGO"] = cm_FLAREBINGO.Value;
                //skip the cockpit versions


                if (general_CPTModel.SelectedIndex.Equals(1))//kinda odd, but go with it. -- JF-17-CPT or JF-17-CPT-PERF1
                {
                    //general_CPTModel.SelectedIndex = 1;
                    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["CPTModel"] = ("JF-17-CPT-PERF1");
                }
                else
                {
                    //general_CPTModel.SelectedIndex = 0;
                    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["CPTModel"] = ("JF-17-CPT");
                }


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["DLPOD_TDC"] = controls_DLPOD_TDC.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["DMAPTYPE"] = general_DMAPTYPE.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["FLAREBINGO"] = cm_FLAREBINGO.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["GUNLIMIT"] = general_GUNLIMIT.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["GUNSIGHT"] = general_GUNSIGHT.SelectedIndex;


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["HUD_TDC"] = controls_HUD_TDC.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["HiddenStick"] = general_HiddenStick.IsChecked;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["IcingOnCake"] = general_IcingOnCake.IsChecked;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["KYBDPITCH"] = general_KYBDPITCH.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["MUSICNUM_SLIDER"] = general_MUSICNUM_SLIDER.Value;


                //****See Countermeasure stuff after general_ and controls_ sections*****//

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["RDR_ELEV"] = controls_RDR_ELEV.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["RDR_TDC"] = controls_RDR_TDC.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["RemoveProbe"] = general_RemoveProbe.IsChecked;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["TDC_DEADZONE"] = controls_TDC_DEADZONE.Value;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["TVIR_TDC"] = controls_TVIR_TDC.Value;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["TestingChg"] = general_TestingChg.IsChecked;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["VOICELOCALE"] = general_VOICELOCALE.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["WMD_TDC"] = controls_WMD_TDC.Value;

                //*****Countermeasures Export*****//

                switch (cm_PROG01CHBI.SelectedIndex)
                {
                    case 0:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 0;
                        break;
                    case 1:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 1;
                        break;
                    case 2:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 2;
                        break;
                    case 3:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 4;
                        break;
                    case 4:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 5;
                        break;
                        //case 5: //cant happen because there arent this many entries. Tell Deka
                        //    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHBI"] = 5;
                        //    break;
                }

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHI"] = cm_PROG01CHI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHN"] = cm_PROG01CHN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01CHR"] = cm_PROG01CHR.SelectedIndex + 1;


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01FLBI"] = cm_PROG01FLBI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01FLI"] = cm_PROG01FLI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01FLN"] = cm_PROG01FLN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01FLR"] = cm_PROG01FLR.SelectedIndex + 1;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG01TYPE"] = cm_PROG01TYPE.SelectedIndex;



                switch (cm_PROG02CHBI.SelectedIndex)
                {
                    case 0:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 0;
                        break;
                    case 1:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 1;
                        break;
                    case 2:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 2;
                        break;
                    case 3:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 4;
                        break;
                    case 4:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 5;
                        break;
                        //case 5: //cant happen because there arent this many entries. Tell Deka
                        //    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHBI"] = 5;
                        //    break;
                }

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHI"] = cm_PROG02CHI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHN"] = cm_PROG02CHN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02CHR"] = cm_PROG02CHR.SelectedIndex + 1;


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02FLBI"] = cm_PROG02FLBI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02FLI"] = cm_PROG02FLI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02FLN"] = cm_PROG02FLN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02FLR"] = cm_PROG02FLR.SelectedIndex + 1;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG02TYPE"] = cm_PROG02TYPE.SelectedIndex;


                switch (cm_PROG03CHBI.SelectedIndex)
                {
                    case 0:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 0;
                        break;
                    case 1:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 1;
                        break;
                    case 2:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 2;
                        break;
                    case 3:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 4;
                        break;
                    case 4:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 5;
                        break;
                        //case 5: //cant happen because there arent this many entries. Tell Deka
                        //    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHBI"] = 5;
                        //    break;
                }

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHI"] = cm_PROG03CHI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHN"] = cm_PROG03CHN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03CHR"] = cm_PROG03CHR.SelectedIndex + 1;


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03FLBI"] = cm_PROG03FLBI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03FLI"] = cm_PROG03FLI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03FLN"] = cm_PROG03FLN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03FLR"] = cm_PROG03FLR.SelectedIndex + 1;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG03TYPE"] = cm_PROG03TYPE.SelectedIndex;



                switch (cm_PROG04CHBI.SelectedIndex)
                {
                    case 0:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 0;
                        break;
                    case 1:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 1;
                        break;
                    case 2:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 2;
                        break;
                    case 3:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 4;
                        break;
                    case 4:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 5;
                        break;
                        //case 5: //cant happen because there arent this many entries. Tell Deka
                        //    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHBI"] = 5;
                        //    break;
                }

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHI"] = cm_PROG04CHI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHN"] = cm_PROG04CHN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04CHR"] = cm_PROG04CHR.SelectedIndex + 1;


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04FLBI"] = cm_PROG04FLBI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04FLI"] = cm_PROG04FLI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04FLN"] = cm_PROG04FLN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04FLR"] = cm_PROG04FLR.SelectedIndex + 1;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG04TYPE"] = cm_PROG04TYPE.SelectedIndex;


                switch (cm_PROG05CHBI.SelectedIndex)
                {
                    case 0:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 0;
                        break;
                    case 1:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 1;
                        break;
                    case 2:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 2;
                        break;
                    case 3:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 4;
                        break;
                    case 4:
                        optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 5;
                        break;
                        //case 5: //cant happen because there arent this many entries. Tell Deka
                        //    optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHBI"] = 5;
                        //    break;
                }

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHI"] = cm_PROG05CHI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHN"] = cm_PROG05CHN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05CHR"] = cm_PROG05CHR.SelectedIndex + 1;


                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05FLBI"] = cm_PROG05FLBI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05FLI"] = cm_PROG05FLI.SelectedIndex;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05FLN"] = cm_PROG05FLN.SelectedIndex + 1;
                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05FLR"] = cm_PROG05FLR.SelectedIndex + 1;

                optionsLuaParseforLoad["options"]["plugins"]["JF-17"]["PROG05TYPE"] = cm_PROG05TYPE.SelectedIndex;

                //*****Countermeasures Export End*****//
                path_optionsluaBackup = Path.Combine(path_backupDirectory, "options.lua");

                //the actual export
                File.WriteAllText(path_optionsluaBackup, LsonVars.ToString(optionsLuaParseforLoad)); // serialize back to a file

                //*****Comms Export Begin*****//

                //foreach (var item in dictionary_comms)
                //{
                //    foo(item.Key);
                //    bar(item.Value);
                //}

                //put the notes in the dictionary

                var commNoteBoxes = CommsGrid.Children.OfType<TextBox>().Where(x => x.Name.Contains("ch"));
                int keyCounterForTheNthPreset = 22;//the presets start at channel 22
                foreach (TextBox textInTheBox in commNoteBoxes)
                {
                    dictionary_notes[keyCounterForTheNthPreset] = textInTheBox.Text;//this allows previous values to be written over
                    ++keyCounterForTheNthPreset;
                }

                //put the freqs in the dictionary
                var commFreqBoxes = CommsGrid.Children.OfType<DecimalUpDown>().Where(x => x.Name.Contains("ch"));
                keyCounterForTheNthPreset = 22;//the presets start at channel 22. reset because of the previous foreach above
                foreach (DecimalUpDown textInTheDecimalUpDown in commFreqBoxes)
                {
                    dictionary_comms[keyCounterForTheNthPreset] = (decimal)textInTheDecimalUpDown.Value;//this allows previous values to be written over
                    ++keyCounterForTheNthPreset;
                }


                //debug
                //foreach (KeyValuePair<int, string> kvp in dictionary_notes)
                //{
                //    //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                //    Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                //}

                path_customerRadioLuaBackup = Path.Combine(path_backupDirectory, "customerRadio.lua");

                using (StreamWriter sw = new StreamWriter(path_customerRadioLuaBackup)) //path
                {
                    int currentNoteNumber = 22;
                    foreach (var commDictionaryEntry in dictionary_comms)//we use the comms one becuse we know that all the freqs are there
                    {
                        sw.WriteLine("presets[" + commDictionaryEntry.Key + "] = " + Convert.ToInt32(commDictionaryEntry.Value * 1000000) + " --" + dictionary_notes[currentNoteNumber]);//remember to add the notes
                        ++currentNoteNumber;
                    }
                    sw.WriteLine("");
                    sw.WriteLine("return presets");

                    sw.WriteLine("--Exported via JF17 Standalone DTC by Bailey " + System.DateTime.Now);

                }

                //*****Comms Export End*****//
                label_status.Text = System.DateTime.Now + " : " + "Backup exported!!!";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));
            }
            else if (isPathOptionsLuaSet == true && isPathJf17FolderSet == false)//only the options lua was set
            {
                MessageBox.Show("You have not selected your JF-17 Doc folder. Select your JF-17 Doc folder.");
                label_status.Text = System.DateTime.Now + " : " + "JF-17 Doc not selected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
            }
            else if (isPathOptionsLuaSet == false && isPathJf17FolderSet == true)//only the docs folder was set
            {
                MessageBox.Show("You have not selected your options.lua file. Select your options.lua file.");
                label_status.Text = System.DateTime.Now + " : " + "options.lua not selected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
            }
            else//nothing was set
            {
                MessageBox.Show("You have not selected your options.lua file and JF-17 Doc folder. Select your options.lua file and JF-17 Doc folder.");
                label_status.Text = System.DateTime.Now + " : " + "files not selected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
            }
            //*****Export Options Lua Section End*****//

        }

        private void button_loadBackup_Click(object sender, RoutedEventArgs e)
        {
            
            Console.WriteLine("Clicked Load Backup");

            path_optionsluaSettingsLoad = Path.Combine(settingsFolder, "options.lua");
            path_jf17DocFolderSettingsLoad = Path.Combine(settingsFolder, "customerRadio.lua");

            Console.WriteLine("Clicked Load Backup " + path_optionsluaSettingsLoad);
            Console.WriteLine("Clicked Load Backup " + path_jf17DocFolderSettingsLoad);

            
            if (File.Exists(path_optionsluaSettingsLoad) && File.Exists(path_jf17DocFolderSettingsLoad))//if both have been set/detected
            {
                Console.WriteLine("Clicked Load Backup and files exist");
                //https://stackoverflow.com/questions/881445/easiest-way-to-parse-a-lua-datastructure-in-c-sharp-net


                var optionsLuaParse = LsonVars.Parse(File.ReadAllText(path_optionsluaSettingsLoad));//load the options.lua into memory
               

                //TODO implement a check to make sure the jf 17 is in the options lua. 
                //if (optionsLuaParse["options"]["plugins"]["JF-17"] == null)
                //{
                //    MessageBox.Show("There isnt a JF17 in the file");
                //}
                //else
                //{
                //    MessageBox.Show("There is a JF17 in the file");
                //}

                LoadCommsPageViaBackup();


                //start loading!!!!
                //all decimalupDown boxe should be GetDecimal()
                //all "selectedIndex" should be GetInt()
                //all true/false should be GetBool()

                //MessageBox.Show(optionsLuaParse["options"]["plugins"]["JF-17"]["CHAFFBINGO"].GetInt().ToString());//debug
                general_AntZoomInv.IsChecked = optionsLuaParse["options"]["plugins"]["JF-17"]["AntZoomInv"].GetBool();
                cm_CHAFFBINGO.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["CHAFFBINGO"].GetInt().ToString();
                //skip the cockpit versions

                if (optionsLuaParse["options"]["plugins"]["JF-17"]["CPTModel"].GetString().Equals("JF-17-CPT-PERF1"))//kinda odd, but go with it.
                {
                    general_CPTModel.SelectedIndex = 1;
                }
                else
                {
                    general_CPTModel.SelectedIndex = 0;
                }

                controls_DLPOD_TDC.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["DLPOD_TDC"].GetDecimal().ToString();
                general_DMAPTYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["DMAPTYPE"].GetInt();
                cm_FLAREBINGO.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["FLAREBINGO"].GetInt().ToString();
                general_GUNLIMIT.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["GUNLIMIT"].GetInt();
                general_GUNSIGHT.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["GUNSIGHT"].GetInt();


                controls_HUD_TDC.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["HUD_TDC"].GetDecimal().ToString();
                general_HiddenStick.IsChecked = optionsLuaParse["options"]["plugins"]["JF-17"]["HiddenStick"].GetBool();
                general_IcingOnCake.IsChecked = optionsLuaParse["options"]["plugins"]["JF-17"]["IcingOnCake"].GetBool();
                general_KYBDPITCH.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["KYBDPITCH"].GetInt();
                general_MUSICNUM_SLIDER.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["MUSICNUM_SLIDER"].GetDecimal().ToString();

                //****See Countermeasure stuff after general_ and controls_ sections*****//

                controls_RDR_ELEV.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["RDR_ELEV"].GetDecimal().ToString();
                controls_RDR_TDC.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["RDR_TDC"].GetDecimal().ToString();
                general_RemoveProbe.IsChecked = optionsLuaParse["options"]["plugins"]["JF-17"]["RemoveProbe"].GetBool();
                controls_TDC_DEADZONE.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["TDC_DEADZONE"].GetDecimal().ToString();

                controls_TVIR_TDC.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["TVIR_TDC"].GetDecimal().ToString();
                general_TestingChg.IsChecked = optionsLuaParse["options"]["plugins"]["JF-17"]["TestingChg"].GetBool();
                general_VOICELOCALE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["VOICELOCALE"].GetInt();
                controls_WMD_TDC.Text = optionsLuaParse["options"]["plugins"]["JF-17"]["WMD_TDC"].GetDecimal().ToString();



                /***************************
                ["PROG01CHBI"] = 0, --Chaff BI. [Index] = [value]; 0 = 0.0; 1 = 0.2; 2 = 0.5; 4 = 0.75; 5 = 1.0 (it skips 3 for some reason)
			    ["PROG01CHI"] = 3, --Chalf SI. [Index] = [value]; 0 = 0.2; 1 = 0.5; 2 = 1; 3 = 2; 4 = 3; 5 = 4.5; 6 = 5
			    ["PROG01CHN"] = 4, --Chaff BQ. the absolute value indicated in the menu 1-6
			    ["PROG01CHR"] = 3, --Chaff SQ. the absolute value indicated in the menu 1-4

			    ["PROG01FLBI"] = 3, --Flare BI. [Index] = [value]; 0 = 0.0; 1 = 0.2; 2 = 0.5; 3 = 0.75; 4 = 1.0
			    ["PROG01FLI"] = 2, --Flare SI. [Index] = [value]; 0 = 0.2; 1 = 0.5; 2 = 1; 3 = 2; 4 = 3; 5 = 4.5; 6 = 5
			    ["PROG01FLN"] = 1, --Flare BQ. the absolute value indicated in the menu 1-6
			    ["PROG01FLR"] = 1, --Flare SQ. the absolute value indicated in the menu 1-4

			    ["PROG01TYPE"] = 0, --0 = CH; 1 = FL; 2 = CH+FL
                ******************/

                switch (optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01CHBI"].GetInt())
                {
                    case 0:
                        cm_PROG01CHBI.SelectedIndex = 0;
                        break;
                    case 1:
                        cm_PROG01CHBI.SelectedIndex = 1;
                        break;
                    case 2:
                        cm_PROG01CHBI.SelectedIndex = 2;
                        break;
                    //case 3: //does not exist for some odd reason. Tell Deka
                    //    cm_PROG01CHBI.SelectedIndex = 3;
                    //    break;
                    case 4:
                        cm_PROG01CHBI.SelectedIndex = 3;
                        break;
                    case 5:
                        cm_PROG01CHBI.SelectedIndex = 4;
                        break;
                }

                cm_PROG01CHI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01CHI"].GetInt();
                cm_PROG01CHN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01CHN"].GetInt() - 1;
                cm_PROG01CHR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01CHR"].GetInt() - 1;

                cm_PROG01FLBI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01FLBI"].GetInt();
                cm_PROG01FLI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01FLI"].GetInt();
                cm_PROG01FLN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01FLN"].GetInt() - 1;
                cm_PROG01FLR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01FLR"].GetInt() - 1;

                cm_PROG01TYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG01TYPE"].GetInt();


                switch (optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02CHBI"].GetInt())
                {
                    case 0:
                        cm_PROG02CHBI.SelectedIndex = 0;
                        break;
                    case 1:
                        cm_PROG02CHBI.SelectedIndex = 1;
                        break;
                    case 2:
                        cm_PROG02CHBI.SelectedIndex = 2;
                        break;
                    //case 3: //does not exist for some odd reason. Tell Deka
                    //    cm_PROG02CHBI.SelectedIndex = 3;
                    //    break;
                    case 4:
                        cm_PROG02CHBI.SelectedIndex = 3;
                        break;
                    case 5:
                        cm_PROG02CHBI.SelectedIndex = 4;
                        break;
                }

                cm_PROG02CHI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02CHI"].GetInt();
                cm_PROG02CHN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02CHN"].GetInt() - 1;
                cm_PROG02CHR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02CHR"].GetInt() - 1;

                cm_PROG02FLBI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02FLBI"].GetInt();
                cm_PROG02FLI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02FLI"].GetInt();
                cm_PROG02FLN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02FLN"].GetInt() - 1;
                cm_PROG02FLR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02FLR"].GetInt() - 1;

                cm_PROG02TYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG02TYPE"].GetInt();



                switch (optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03CHBI"].GetInt())
                {
                    case 0:
                        cm_PROG03CHBI.SelectedIndex = 0;
                        break;
                    case 1:
                        cm_PROG03CHBI.SelectedIndex = 1;
                        break;
                    case 2:
                        cm_PROG03CHBI.SelectedIndex = 2;
                        break;
                    //case 3: //does not exist for some odd reason. Tell Deka
                    //    cm_PROG03CHBI.SelectedIndex = 3;
                    //    break;
                    case 4:
                        cm_PROG03CHBI.SelectedIndex = 3;
                        break;
                    case 5:
                        cm_PROG03CHBI.SelectedIndex = 4;
                        break;
                }

                cm_PROG03CHI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03CHI"].GetInt();
                cm_PROG03CHN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03CHN"].GetInt() - 1;
                cm_PROG03CHR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03CHR"].GetInt() - 1;

                cm_PROG03FLBI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03FLBI"].GetInt();
                cm_PROG03FLI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03FLI"].GetInt();
                cm_PROG03FLN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03FLN"].GetInt() - 1;
                cm_PROG03FLR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03FLR"].GetInt() - 1;

                cm_PROG03TYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG03TYPE"].GetInt();




                switch (optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04CHBI"].GetInt())
                {
                    case 0:
                        cm_PROG04CHBI.SelectedIndex = 0;
                        break;
                    case 1:
                        cm_PROG04CHBI.SelectedIndex = 1;
                        break;
                    case 2:
                        cm_PROG04CHBI.SelectedIndex = 2;
                        break;
                    //case 3: //does not exist for some odd reason. Tell Deka
                    //    cm_PROG04CHBI.SelectedIndex = 3;
                    //    break;
                    case 4:
                        cm_PROG04CHBI.SelectedIndex = 3;
                        break;
                    case 5:
                        cm_PROG04CHBI.SelectedIndex = 4;
                        break;
                }

                cm_PROG04CHI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04CHI"].GetInt();
                cm_PROG04CHN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04CHN"].GetInt() - 1;
                cm_PROG04CHR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04CHR"].GetInt() - 1;

                cm_PROG04FLBI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04FLBI"].GetInt();
                cm_PROG04FLI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04FLI"].GetInt();
                cm_PROG04FLN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04FLN"].GetInt() - 1;
                cm_PROG04FLR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04FLR"].GetInt() - 1;

                cm_PROG04TYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG04TYPE"].GetInt();




                switch (optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05CHBI"].GetInt())
                {
                    case 0:
                        cm_PROG05CHBI.SelectedIndex = 0;
                        break;
                    case 1:
                        cm_PROG05CHBI.SelectedIndex = 1;
                        break;
                    case 2:
                        cm_PROG05CHBI.SelectedIndex = 2;
                        break;
                    //case 3: //does not exist for some odd reason. Tell Deka
                    //    cm_PROG05CHBI.SelectedIndex = 3;
                    //    break;
                    case 4:
                        cm_PROG05CHBI.SelectedIndex = 3;
                        break;
                    case 5:
                        cm_PROG05CHBI.SelectedIndex = 4;
                        break;
                }

                cm_PROG05CHI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05CHI"].GetInt();
                cm_PROG05CHN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05CHN"].GetInt() - 1;
                cm_PROG05CHR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05CHR"].GetInt() - 1;

                cm_PROG05FLBI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05FLBI"].GetInt();
                cm_PROG05FLI.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05FLI"].GetInt();
                cm_PROG05FLN.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05FLN"].GetInt() - 1;
                cm_PROG05FLR.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05FLR"].GetInt() - 1;

                cm_PROG05TYPE.SelectedIndex = optionsLuaParse["options"]["plugins"]["JF-17"]["PROG05TYPE"].GetInt();

                label_status.Text = System.DateTime.Now + " : " + "Backup loaded!!!";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#0da04c"));
            }
            else
            {
                MessageBox.Show("Backup files not detected. Export a backup.");
                label_status.Text = System.DateTime.Now + " : " + "backup not detected";
                label_status.Foreground = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#f40000"));
            }
        }

        private void LoadCommsPageViaBackup()
        {
            //if (File.Exists(path_customerRadioLua))//load the customerRadio.lua into memory if it exists
                //we already know it exists
            //{
                //MessageBox.Show(path_customerRadioLua + " loaded.");
                //var customerRadioLuaParse = LsonVars.Parse(File.ReadAllText(path_customerRadioLua));//cant use this bc it isnt lua
                //https://www.c-sharpcorner.com/UploadFile/mahesh/how-to-read-a-text-file-in-C-Sharp/
                string[] customerRadioParse = File.ReadAllLines(path_jf17DocFolderSettingsLoad);
                foreach (string radioPresetLine in customerRadioParse)
                {
                    //Console.WriteLine(radioPresetLine);
                    if (radioPresetLine.Contains('='))//could also use "presets["?
                    {

                        //https://stackoverflow.com/questions/378415/how-do-i-extract-text-that-lies-between-parentheses-round-brackets
                        int presetNumber = Convert.ToInt32(radioPresetLine.Split('[', ']')[1]);//this is explained in the above link
                                                                                               //https://stackoverflow.com/questions/14998595/need-to-get-a-string-after-a-word-in-a-string-in-c-sharp/14998640
                        string toBeSearched = "=";
                        //string frequencyNumber = radioPresetLine.Substring(radioPresetLine.IndexOf(toBeSearched) + toBeSearched.Length);
                        //Console.WriteLine("Index of toBeSearched: "+radioPresetLine.IndexOf(toBeSearched));

                        //Console.WriteLine(radioPresetLine.Substring(radioPresetLine.IndexOf(toBeSearched) + 2, 9));//debug
                        decimal frequencyNumber = Convert.ToDecimal(radioPresetLine.Substring(radioPresetLine.IndexOf(toBeSearched) + 2, 9));//the freqs are 9 numbers long, normally
                        frequencyNumber = (frequencyNumber / 1000000);//this is the converter for a human-readable freq
                                                                      //MessageBox.Show("Channel " + presetNumber + ": Freq "+ frequencyNumber);//debug
                                                                      //put the number in the correct box in the gui
                                                                      //ch22_freq
                        ///("ch"+presetNumber+"_freq").Value = frequencyNumber;//uhh, good luck
                        //dictionary_comms.Add(presetNumber, frequencyNumber);//this does not allow re-writes on a second load
                        dictionary_comms[presetNumber] = frequencyNumber;//this allows previous values to be written over

                        //*****Get the notes*****//
                        if (radioPresetLine.Contains("--"))
                        {
                            string toBeSearchedNotes = "--";
                            //TODO clean this up so that it make sense
                            //Console.WriteLine("Note Length: " +   (radioPresetLine.Length)  + " - " + radioPresetLine.IndexOf(toBeSearchedNotes) + " = " +  (radioPresetLine.Length - radioPresetLine.IndexOf(toBeSearchedNotes) - 2));
                            string frequencyNotes = radioPresetLine.Substring(radioPresetLine.IndexOf(toBeSearchedNotes) + toBeSearchedNotes.Length, (radioPresetLine.Length - radioPresetLine.IndexOf(toBeSearchedNotes)) - 2);
                            dictionary_notes[presetNumber] = frequencyNotes;//this allows previous values to be written over
                                                                            //Console.WriteLine("Notes: " + frequencyNotes);


                        }
                    }
                }
                //https://stackoverflow.com/questions/28552603/stringify-key-value-pairs-in-dictionary
                //debug for dictionaries
                //foreach (KeyValuePair<int, string> kvp in dictionary_notes)
                //{
                //    //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                //    Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                //}
                LoadCommChannelsAndFreqs();
            //}
            //else//the file was not found or has not yet been made. Either generate one now, or rely on the WPF presets
            //{
            //    System.Windows.MessageBox.Show("File does not exist. Reseting to baseline. Export will generate a new file.");
            //}
        }
    }
    ////https://stackoverflow.com/questions/52621314/wpf-drag-and-drop-text-file-into-application
    ////this works, but maybe too complicated for the user
    //private void TextBox_Drop(object sender, DragEventArgs e)
    //{
    //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
    //    {
    //        string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
    //        if (files != null && files.Length > 0)
    //        {
    //            ((TextBox)sender).Text = files[0];
    //        }
    //    }
    //}

    //private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
    //{
    //    e.Handled = true;
    //}
}
