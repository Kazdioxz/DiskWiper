using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace Disk_Wiper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Variables publiques
        #region Public
        public bool isTotalWipe = false; //Bool permettant la sauvegarde du choix pour la wipeList.
        public int numberPass = 0; //Int permettant la sauvegarde du nombre de pass à effectuer.
        public int passed = 0; //Int permettant la sauvegarde du nombre pass efféctué.
        public string diskPath = ""; //String contenant le chemin d'accès au lecteur (Ex : C:\)
        public string diskLetter = ""; //String contenant la lettre du lecteur (Ex : C)
        public long space = 0; //Long contenant la quantité de données à écrire.
        public bool isOsDisk = false; //Bool renseignant sur la nature du disk.
        public bool _canWrite = true; //Bool autorisant ou non l'écriture du fichier (permet l'arrêt de la tâche).
        public DateTime currentDateTime;
        public DateTime startDateTime;
        public string path = "";
        public bool reboot = false;
        #endregion
        //Variables privées
        #region Private
        #endregion

        //Initialisation
        #region init
        public MainWindow()
        {
            InitializeComponent();
            //Selection d'index par défaut pour éviter les bugs
            #region Index par défaut pour les listes
            wipeList.SelectedIndex = 0; //Séléction de l'index 0 pour la comboBox relative au type d'action
            securityList.SelectedIndex = 0; //Séléction de l'index 0 pour la comboBox relative au type de sécurité
            #endregion

            //Récupération des disques connéctés
            #region Récupération des disques connéctés

            //Ajouts des disques
            DriveInfo[] drives = DriveInfo.GetDrives(); //Liste de tout les drives connectés à l'ordinateur
            foreach (DriveInfo info in drives) //Pour chaque drive(info) dans la liste drives (Liste de tout les drives)
            {
                //Si le drive est prêt, qu'il n'est, ni un disque réseau et ni un RAMDisk et qu'il  possède un chemin d'accès.
                if (info.IsReady && info.DriveType != DriveType.Network && info.DriveType != DriveType.NoRootDirectory && info.DriveType != DriveType.Ram)
                {
                    driveList.Items.Add(info.Name); //Ajout de son nom dans la liste
                }
            }

            //Selection du disque par défaut

            //Si la liste possède des items
            if (driveList.Items != null)
            {
                //Séléction de l'index 0
                driveList.SelectedIndex = 0;
            }


            #endregion


        }
        #endregion


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //Actions du boutton
            #region Main


            var msgB = MessageBox.Show("Are you sure you want to continue ?", "Infos", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (msgB == MessageBoxResult.Yes)
            {
                //Detection de l'etat
                #region Init
                //Mise à jour de l'UI
                #region UI
                wipeList.IsEnabled = false;
                securityList.IsEnabled = false;
                driveList.IsEnabled = false;
                mainBar.Value = 0;
                percentMainBar.Visibility = Visibility.Visible;
                sinceLabel.Visibility = Visibility.Visible;
                #endregion





                //Lettre ?
                #region Letter
                diskPath = driveList.SelectedItem.ToString();
                diskLetter = diskPath.Replace(@":\", "");
                #endregion


                //Formatage et espace max?
                #region Format
                DriveInfo getInfo = new DriveInfo(diskLetter);
                switch (wipeList.SelectedIndex)
                {
                    case 0:
                        isTotalWipe = false;
                        space = getInfo.AvailableFreeSpace;
                        break;
                    case 1:
                        isTotalWipe = true;
                        space = getInfo.TotalSize;
                        break;
                }
                #endregion



                //Nombre de passes ?
                #region passes
                switch (securityList.SelectedIndex)
                {
                    case 0:
                        numberPass = 1;
                        break;
                    case 1:
                        numberPass = 3;
                        break;
                    case 2:
                        numberPass = 7;
                        break;
                    case 3:
                        numberPass = 35;
                        break;
                }
                #endregion
                #endregion




                if (mainButton.Content.ToString() == "Wipe")
                {
                    mainButton.Content = "Cancel";



                    #region Actions
                    if (isTotalWipe)
                    {
                        //Formatage du volumme
                        #region Format
                        //File System
                        #region FileSystem
                        string fileSystem = "";
                        if (space > 2000000000)
                        {
                            fileSystem = "NTFS";
                        }
                        else
                        {
                            fileSystem = "FAT32";
                        }
                        #endregion
                        sinceLabel.Visibility = Visibility.Hidden; //Mise à jour d'élément graphique
                        statutLabel.Text = $@"Statut : Formating {diskPath}";
                        mainBar.IsIndeterminate = true;
                        await Task.Run(() => format(fileSystem, "WIPPING"));//Formatage du volumme
                        mainBar.IsIndeterminate = false;
                        #endregion
                        //Ecriture du fichier overwrite
                        #region Writer
                        _canWrite = true; //Autorisation d'écriture

                        percentMainBar.Visibility = Visibility.Visible; //Mise à jour d'élément graphique
                        startDateTime = DateTime.Now; //Récupération du temps actuel
                        sinceLabel.Visibility = Visibility.Visible; //Mise à jour d'élément graphique
                        await Task.Run(() => overwrite());

                        #endregion
                    }
                    else
                    {
                        //Ecriture du fichier overwrite
                        #region Writer
                        _canWrite = true; //Autorisation d'écriture

                        percentMainBar.Visibility = Visibility.Visible; //Mise à jour d'élément graphique
                        startDateTime = DateTime.Now; //Récupération du temps actuel
                        sinceLabel.Visibility = Visibility.Visible; //Mise à jour d'élément graphique
                        await Task.Run(() => overwrite());

                        #endregion
                    }


                    #endregion

                }
                else if (mainButton.Content.ToString() == "Cancel")
                {
                    reboot = true;
                    mainButton.Content = "Wipe"; //Mise à jour d'élément graphique
                    _canWrite = false;


                }
            }
            #endregion
        }



        //Différentes fonctions utiles.
        #region Fonctions
        #region format
        public async void format(string fileSystem, string diskName)
        {
            DriveInfo info = new DriveInfo(diskPath);
            if (info.IsReady)
            {
                string path = $"{diskPath}format.cmd"; //Mise en place du chemin d'accès
                                                       //Suppresion
                #region delete
                if (File.Exists(path))//Suppression du fichier si déjà existant
                    File.Delete(path);
                #endregion
                //Ecriture
                #region write
                using (StreamWriter writer = File.CreateText(path))//Ecriture du fichier de commande sur le disque permettant le formatage avec les paramètres requis
                {
                    writer.WriteLine($@"FORMAT {diskLetter}: /Y /FS:{fileSystem} /V:{diskName} /Q");//Commande Windows native "FORMAT C: /Y /FS:(NTFS our FAT32) /V:NomDuVolumme /Q"
                    writer.Close();
                }
                #endregion

                //Démarrage du process
                #region process
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = path;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.Start();
                    process.WaitForExit();
                }
                #endregion

            }
            await Task.Delay(1);
        }
        #endregion




        public async void overwrite()
        {
            for (int x = 0; x < numberPass; x++)
            {
                passed++;
                //Définitions
                string fileName = RandomString(8);//Génération d'un nom de fichier aléatoire
                path = diskPath + fileName;

                //Code relatif à l'ecriture du fichier
                #region Writer

                //Definitions des variables
                #region DefVar
                int _update = 0;//Permet de mettre à jour les informations d'avancement
                const int blockSize = 1024 * 8;
                const int blocksPerMb = (1024 * 1024) / blockSize;
                byte[] data = new byte[blockSize];
                Random rng = new Random();
                #endregion

                using (FileStream stream = File.OpenWrite(path))
                {
                    try
                    {
                        for (int i = 0; i < (space * blocksPerMb); i++)
                        {
                            //Ecriture du fichier et mise à jour des éléments graphiques correspondants
                            #region Write&Update
                            if (_canWrite)//Ecriture autorisée
                            {
                                rng.NextBytes(data); //Généreation de bytes aléatoires
                                stream.Write(data, 0, data.Length); //Ecriture des bytes dans le fichier

                                #region Update UI
                                if (_update == 3500) //Mise à jour des infos utilisateur
                                {
                                    _update = 0;
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        long size;
                                        if (passed != 0)
                                        {
                                            size = GetFileSizeOnDisk(path) / 1024 / 1024 * passed;
                                        }
                                        else
                                        {
                                            size = GetFileSizeOnDisk(path) / 1024 / 1024;
                                        }
                                        long totalSpace = ((long)space / (long)1024 / (long)1024) * numberPass;
                                        long percent = getPercent(size, totalSpace);
                                        mainBar.Value = percent;
                                        currentDateTime = DateTime.Now;
                                        TimeSpan ts = (startDateTime - currentDateTime);
                                        FileInfo fileInfo = new FileInfo(path);
                                        statutLabel.Text = $@"Statut : Wipping the disk {size}MB/{space / 1024 / 1024 * passed}MB {mainBar.Value}%{Environment.NewLine}Step {passed}/{numberPass}";
                                        sinceLabel.Text = $"Started since {ts.ToString(@"hh\:mm\:ss")}";
                                        percentMainBar.Text = mainBar.Value.ToString() + "%";
                                    });
                                }
                                else
                                {
                                    _update++;
                                }
                                #endregion
                            }
                            else
                            {
                                #region Closing
                                this.Dispatcher.Invoke(() =>
                                {
                                    statutLabel.Text = "Statut : Closing...";
                                    mainButton.IsEnabled = false;
                                    percentMainBar.Visibility = Visibility.Hidden;
                                    sinceLabel.Visibility = Visibility.Hidden;
                                    mainBar.IsIndeterminate = true;

                                });

                                stream.Close(); //Arrêt de l'écriture
                                                //Suppression du fichier
                            #region Delete
                            del:;
                                try
                                {
                                    File.Delete(path);

                                }
                                catch
                                {
                                    goto del;
                                }
                                Environment.Exit(0);
                            }
                            #endregion
                            //await Task.Delay(10000);
                            if (reboot)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    this.Visibility = Visibility.Hidden;
                                    System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                                });

                            }
                            #endregion
                            #endregion

                        }
                        stream.Close();
                    }
                    catch (System.IO.IOException)
                    {
                        //L'écriture est finie
                        #region Done
                        #region UpdateUI
                        this.Dispatcher.Invoke(() =>
                        {
                            long size = GetFileSizeOnDisk(path) / 1024 / 1024;
                            long totalSpace = ((long)space / (long)1024 / (long)1024) * numberPass;
                            long percent = getPercent(size, totalSpace);
                            mainBar.Value = percent;
                            currentDateTime = DateTime.Now;
                            TimeSpan ts = (startDateTime - currentDateTime);
                            FileInfo fileInfo = new FileInfo(path);
                            statutLabel.Text = $@"Statut : Wipping the disk {size}MB/{space / 1024 / 1024}MB {mainBar.Value}%{Environment.NewLine}Step {passed}/{numberPass}";
                            sinceLabel.Text = $"Started since {ts.ToString(@"hh\:mm\:ss")}";
                            percentMainBar.Text = mainBar.Value.ToString() + "%";
                        });
                        #endregion



                        this.Dispatcher.Invoke(() =>
                        {
                            wipeList.IsEnabled = false;
                            securityList.IsEnabled = false;
                            driveList.IsEnabled = false;
                            mainBar.Value = 0;
                            percentMainBar.Visibility = Visibility.Hidden;
                            sinceLabel.Visibility = Visibility.Hidden;
                            mainBar.IsIndeterminate = true;
                            mainButton.IsEnabled = false;
                            statutLabel.Text = "Statut : Deleting ...";
                        });

                        stream.Close();
                    #region Delete
                    delete:;
                        try
                        {
                            if (File.Exists(path)) File.Delete(path);
                        }
                        catch
                        {
                            goto delete;
                        }
                        this.Dispatcher.Invoke(() =>
                        {
                            mainBar.IsIndeterminate = false;
                            percentMainBar.Visibility = Visibility.Visible;
                            sinceLabel.Visibility = Visibility.Visible;
                        });
                        #endregion
                        #endregion


                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occured !\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }







                #endregion
            }

            this.Dispatcher.Invoke(() =>
            {
                wipeList.IsEnabled = true;
                securityList.IsEnabled = true;
                driveList.IsEnabled = true;
                mainBar.IsIndeterminate = false;
                mainButton.IsEnabled = true;
                mainButton.Content = "Wipe";
                mainBar.Value = 0;
                percentMainBar.Visibility = Visibility.Hidden;
                sinceLabel.Visibility = Visibility.Hidden;
                statutLabel.Text = "Statut : Waiting for user actions";
                MessageBox.Show("Done !", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                mainBar.Value = 0;
                passed = 0;
                sinceLabel.Text = "Started since 00:00:00";
            });

            await Task.Delay(1);

        }




        #region percent
        public long getPercent(long x, long y)
        {
            try
            {
                return x * 100 / y;
            }
            catch
            {
                MessageBox.Show($"Not enough space on drive", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return 0;
            }
        }
        #endregion

        #region FileSize
        public static long GetFileSizeOnDisk(string file)
        {
            try
            {
                FileInfo info = new FileInfo(file);
                uint dummy, sectorsPerCluster, bytesPerSector;
                int result = GetDiskFreeSpaceW(info.Directory.Root.FullName, out sectorsPerCluster, out bytesPerSector, out dummy, out dummy);
                if (result == 0) throw new Win32Exception();
                uint clusterSize = sectorsPerCluster * bytesPerSector;
                uint hosize;
                uint losize = GetCompressedFileSizeW(file, out hosize);
                long size;
                size = (long)hosize << 32 | losize;
                return ((size + clusterSize - 1) / clusterSize) * clusterSize;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
                return 0;
            }
        }

        [DllImport("kernel32.dll")]
        static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
           [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
           out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
           out uint lpTotalNumberOfClusters);
        #endregion

        #region randomString
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        #endregion

        #endregion

        //Fermeture de la fenêtre
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            statutLabel.Text = "Statut : Closing...";
            mainButton.IsEnabled = false;
            percentMainBar.Visibility = Visibility.Hidden;
            sinceLabel.Visibility = Visibility.Hidden;
            mainBar.IsIndeterminate = true;
            _canWrite = false;


            if (File.Exists(path))
            {
                e.Cancel = true;

            }
            else
            {
                e.Cancel = false;
                Environment.Exit(0);
            }

        }
    }
}
