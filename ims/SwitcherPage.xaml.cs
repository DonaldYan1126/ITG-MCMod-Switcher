using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ims
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SwitcherPage : Page
    {
        public SwitcherPage()
        {
            this.InitializeComponent();
            LoadMods();
        }

        StorageFolder imsModsFolder;

        private async void LoadMods()
        {
            StorageFolder documentsFolder = KnownFolders.DocumentsLibrary;
            string username = Environment.UserName;
            imsModsFolder = await documentsFolder.CreateFolderAsync("IMSMods", CreationCollisionOption.OpenIfExists);

            // 获取 PureModsChanger 文件夹下所有文件夹的名称
            IReadOnlyList<StorageFolder> subFolders = await imsModsFolder.GetFoldersAsync();

            // 绑定到ComboBox
            Version.ItemsSource = subFolders.Select(folder => folder.Name).ToList();
            Version.SelectedIndex = 0;
        }

        private async void Report()
        {
            ContentDialog ErrorDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "No Mods!",
                Content = "There's no mod folder at Documents/IMSMods. Create Folder such as 1.20.4-Fabric and put mods(.jar) in it.",
                PrimaryButtonText = "Refresh",
                CloseButtonText = "Later",
                DefaultButton = ContentDialogButton.Primary
            };

            await Task.Delay(100);

            ContentDialogResult result = await ErrorDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
                LoadMods();
        }

        private async void Request()
        {
            ContentDialog ErrorDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Access Denial!",
                Content = "Because this is a UWP app, so we have to request your permission to access your Minecraft folder",
                PrimaryButtonText = "OK, I will",
                CloseButtonText = "Wait",
                DefaultButton = ContentDialogButton.Primary
            };

            await Task.Delay(100);

            ContentDialogResult result = await ErrorDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
        }

        private async void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (Version.Items.Count == 0)
            {
                Report();
            }
            else
            {
                // 获取选中的文件夹名称
                string selectedFolderName = Version.SelectedItem.ToString();

                // 获取选中的文件夹的路径
                StorageFolder selectedFolder = await imsModsFolder.GetFolderAsync(selectedFolderName);

                // 获取选中文件夹中的所有.jar文件
                IReadOnlyList<StorageFile> jarFiles = await selectedFolder.GetFilesAsync();

                // 获取 Minecraft mods 文件夹的路径
                try
                {
                    string roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    StorageFolder minecraftModsFolder = await StorageFolder.GetFolderFromPathAsync(roamingPath + "\\.minecraft\\mods");

                    // 删除 Minecraft mods 文件夹中的所有文件
                    foreach (StorageFile file in await minecraftModsFolder.GetFilesAsync())
                    {
                        await file.DeleteAsync();
                    }
                    // 复制选中的文件夹中的所有文件到 Minecraft mods 文件夹中
                    foreach (StorageFile jarFile in jarFiles)
                    {
                        await jarFile.CopyAsync(minecraftModsFolder);
                    }

                    //Show success message
                    SuccessBar.IsOpen = true;
                }
                catch (Exception ex)
                {
                    Request();
                }

            }

        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadMods();
        }
    }
}
