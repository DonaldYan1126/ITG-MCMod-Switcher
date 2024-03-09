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
using System.IO.Compression;
using System.Text.Json;
using Windows.UI.Xaml.Media.Imaging;

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
            TweekMods.ItemsSource = modInfoList;
            Windows.UI.Core.Preview.SystemNavigationManagerPreview.GetForCurrentView().CloseRequested +=
            async (sender, args) =>
            {
                args.Handled = true;
                ContentDialog CloseDialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = "Confirm your change?",
                    Content = "For safety reason, we will not modify your .minecraft folder directly. If you want, please click Yes below, or back to edit and click the Confirm button showing on Switcher page.",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary
                };
                if (isChanged)
                {
                    var result = await CloseDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        ConfirmChange();
                    }
                    else if (result == ContentDialogResult.Secondary)
                    {
                        App.Current.Exit();
                    }
                }
                else App.Current.Exit();
            };
        }



        List<ModInfo> modInfoList = new List<ModInfo>();
        bool isChanged = false;

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
        }

        private async void Report()
        {
            Thickness thickness = new Thickness();
            thickness.Bottom = 16;
            ImageBrush imageBrush = new ImageBrush();
            StackPanel panel = new StackPanel();
            TextBlock text = new TextBlock();
            text.Text = "There's no mod folder at Documents/IMSMods.\nCreate Folder such as 1.20.4-Fabric and put mods(.jar) in it.";
            text.Margin = thickness;
            Image image = new Image();
            imageBrush.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Tutorial/AddFiles.png", UriKind.Absolute));
            image.Source = imageBrush.ImageSource;
            panel.Children.Add(text);
            panel.Children.Add(image);
            ContentDialog ErrorDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "No Mods!",
                Content = panel,
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
            Thickness thickness = new Thickness();
            thickness.Bottom = 16;
            ImageBrush imageBrush = new ImageBrush();
            StackPanel panel = new StackPanel();
            TextBlock text = new TextBlock();
            text.Text = "Because this is a UWP app,\nwe need permission to access your .minecraft folder";
            text.Margin = thickness;
            Image image = new Image();
            imageBrush.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Tutorial/GivePermission.png", UriKind.Absolute));
            image.Source = imageBrush.ImageSource;
            panel.Children.Add(text);
            panel.Children.Add(image);
            ContentDialog ErrorDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Access Denial!",
                Content = panel,
                PrimaryButtonText = "OK, I will",
                CloseButtonText = "Wait",
                DefaultButton = ContentDialogButton.Primary
            };

            await Task.Delay(100);

            ContentDialogResult result = await ErrorDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
        }

        private async void ConfirmChange()
        {
            if (!Version.Items.Any())
            {
                Report();
            }
            else if (Version.SelectedItem != null)
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
                    SuccessBar.Title = "Success";
                    SuccessBar.Message = "Successfully switch your mods' version";
                    SuccessBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
                    SuccessBar.IsOpen = true;
                }
                catch (Exception ex)
                {
                    Request();
                }

            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            ConfirmChange();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadMods();
        }

        private void Version_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Listing();
            isChanged = true;
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            Listing();
        }

        private async void Listing()
        {
            if (Version.Items.Any() && Version.SelectedItem != null)
            {
                Tweeker.IsExpanded = false;
                // Delete source and clear items to refresh ListView.
                TweekMods.ItemsSource = null;
                TweekMods.Items.Clear();
                modInfoList.Clear();
                // 获取选中的文件夹名称
                string selectedFolderName = Version.SelectedItem.ToString();

                // 获取选中的文件夹的路径
                StorageFolder selectedFolder = await imsModsFolder.GetFolderAsync(selectedFolderName);

                // 获取选中文件夹中的所有.jar文件
                IReadOnlyList<StorageFile> jarFiles = await selectedFolder.GetFilesAsync();

                foreach (StorageFile jarFile in jarFiles)
                {
                    if (jarFile.Name.EndsWith(".jar") || jarFile.Name.EndsWith(".jar.disable"))
                    {
                        bool isEnabled = !jarFile.Path.EndsWith(".jar.disable");
                        using (var stream = await jarFile.OpenStreamForReadAsync())
                        {
                            using (ZipArchive zipArchive = new ZipArchive(stream))
                            {
                                // 遍历文件中的所有条目
                                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                                {
                                    // 如果条目是 fabric.mod.json 文件
                                    if (entry.FullName == "fabric.mod.json")
                                    {
                                        // 读取 fabric.mod.json 文件的内容
                                        string modJson = ReadEntryContent(entry);

                                        // 将 fabric.mod.json 文件的内容反序列化为 ModInfo 对象
                                        ModInfo modInfo = JsonSerializer.Deserialize<ModInfo>(modJson);

                                        modInfo.isEnabled = isEnabled;
                                        modInfo.fileName = jarFile.DisplayName;
                                        modInfo.FilePath = jarFile.Path;
                                        // 将 ModInfo 对象添加到 List 中
                                        modInfoList.Add(modInfo);
                                    }
                                }
                            }
                            stream.Close();
                        }
                    }
                }
                TweekMods.ItemsSource = modInfoList;
                Tweeker.IsExpanded = true;
            }

        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            isChanged = true;
            CheckBox checkBox = (CheckBox)sender;
            ModInfo modInfo = (ModInfo)checkBox.DataContext;
            RenameFile(modInfo, modInfo.isEnabled);
        }
        private async void RenameFile(ModInfo modInfo, bool enable)
        {
            // 重命名文件
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(modInfo.FilePath);

                // 获取文件所在的文件夹
                StorageFolder folder = await file.GetParentAsync();

                // 创建新的文件名
                string newFileName = !enable ? $"{modInfo.fileName}.jar.disable" : $"{modInfo.fileName}.jar";
                await file.RenameAsync(newFileName);
                modInfo.FilePath = folder.Path + "\\" + newFileName;
            }
            catch (Exception ex)
            {
                SuccessBar.Title = "Error";
                SuccessBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
                SuccessBar.Message = ex.Message;
                SuccessBar.IsOpen = true;
            }
        }


        /// <summary>
        /// 读取 ZipArchiveEntry 的内容
        /// </summary>
        /// <param name="entry">要读取内容的 ZipArchiveEntry</param>
        /// <returns>ZipArchiveEntry 的内容</returns>
        private static string ReadEntryContent(ZipArchiveEntry entry)
        {
            // 使用 StreamReader 读取 ZipArchiveEntry 的内容
            using (StreamReader streamReader = new StreamReader(entry.Open()))
            {
                return streamReader.ReadToEnd();
            }
        }



        public class ModInfo
        {
            /// <summary>
            /// Mod 名称
            /// </summary>
            public string name { get; set; }

            /// <summary>
            /// Mod 版本
            /// </summary>
            public string version { get; set; }
            public bool isEnabled { get; set; }
            public string FilePath { get; set; }
            public string fileName { get; set; }
        }


    }
}
