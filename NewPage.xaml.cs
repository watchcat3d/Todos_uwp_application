﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace Todos
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class NewPage : Page
    {
        public NewPage()
        {
            this.InitializeComponent();
            this.ViewModel = ViewModels.TodoItemViewModel.GetInstance();
        }

        private ViewModels.TodoItemViewModel ViewModel;
        private StorageFile imageFile = null;
        private Uri imauri = new Uri(Models.TodoItem.defaultImagePath);

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            bool suspending = ((App)App.Current).issuspend;
            if (suspending)
            {
                ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
                composite["title"] = textTitle.Text;
                composite["detail"] = textDetail.Text;
                composite["date"] = DueDate.Date;

                ApplicationData.Current.LocalSettings.Values["newpage"] = composite;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {

            if (e.NavigationMode == NavigationMode.New)
            {
                ApplicationData.Current.LocalSettings.Values.Remove("newpage");
            }
            else
            {
                if (ApplicationData.Current.LocalSettings.Values["image"] != null)
                {
                    StorageFile temp;
                    temp = await StorageApplicationPermissions.FutureAccessList.GetFileAsync((string)ApplicationData.Current.LocalSettings.Values["image"]);
                    IRandomAccessStream ir = await temp.OpenAsync(FileAccessMode.Read);
                    BitmapImage bi = new BitmapImage();
                    await bi.SetSourceAsync(ir);
                    MyImage.Source = bi;
                    ApplicationData.Current.LocalSettings.Values["image"] = null;
                }

                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("newpage"))
                {
                    var composite = ApplicationData.Current.LocalSettings.Values["newpage"] as ApplicationDataCompositeValue;
                    textTitle.Text = (string)composite["title"];
                    textDetail.Text = (string)composite["detail"];
                    DueDate.Date = (DateTimeOffset)composite["date"];

                    ApplicationData.Current.LocalSettings.Values.Remove("newpage");
                }
            }
            if (ViewModel != null)
            {
                if (ViewModel.SelectedItem == null)
                {
                    Create.Content = "Create";
                }
                else
                {
                    Create.Content = "Update";
                    Create.Click -= CreateClick;
                    Create.Click -= Update;
                    Create.Click += Update;
                    textTitle.Text = ViewModel.SelectedItem.title;
                    textDetail.Text = ViewModel.SelectedItem.description;
                    DueDate.Date = ViewModel.SelectedItem.duedate;
                    MyImage.Source = ViewModel.SelectedItem.coverImage;
                    Delete.Visibility = Visibility.Visible;
                }
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            textTitle.Text = "";
            textDetail.Text = "";
            DueDate.Date = DateTimeOffset.Now;
            if ((string)Create.Content == "Update")
            {
                Create.Content = "Create";
                Create.Click -= CreateClick;
                Create.Click -= Update;
                Create.Click += CreateClick;
                Delete.Visibility = Visibility.Collapsed;
                ViewModel.SelectedItem = null;
            }
        }

        private async void CreateClick(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                Title = "提示信息",
                PrimaryButtonText = "确定"
            };

            if (textTitle.Text == "")
            {
                dialog.Content = "标题不能为空";
                await dialog.ShowAsync();
            }
            else if (textDetail.Text == "")
            {
                dialog.Content = "内容主体不能为空";
                await dialog.ShowAsync();
            }
            else if (DueDate.Date < DateTimeOffset.Now)
            {
                dialog.Content = "日期不正确";
                await dialog.ShowAsync();
            }
            else
            {
                if (imageFile != null)
                {
                    string imageName = imageFile.Name;
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    StorageFile newImageFile = await imageFile.CopyAsync(localFolder, imageName, NameCollisionOption.ReplaceExisting);
                    imauri = new Uri(newImageFile.Path);
                }

                ViewModel.AddTodoItem(textTitle.Text, textDetail.Text, DueDate.Date, MyImage.Source as BitmapImage, imauri);
                textTitle.Text = "";
                textDetail.Text = "";
                DueDate.Date = DateTimeOffset.Now;
                Frame.Navigate(typeof(MainPage));
            }
        }

        private async void Update(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                Title = "提示信息",
                PrimaryButtonText = "确定"
            };

            if (textTitle.Text == "")
            {
                dialog.Content = "标题不能为空";
                await dialog.ShowAsync();
            }
            else if (textDetail.Text == "")
            {
                dialog.Content = "内容主体不能为空";
                await dialog.ShowAsync();
            }
            else if (DueDate.Date < DateTimeOffset.Now)
            {
                dialog.Content = "日期不正确";
                await dialog.ShowAsync();
            }
            else
            {
                if (imageFile != null)
                {
                    string imageName = imageFile.Name;
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    StorageFile newImageFile = await imageFile.CopyAsync(localFolder, imageName, NameCollisionOption.ReplaceExisting);
                    imauri = new Uri(newImageFile.Path);
                }

                ViewModel.UpdateTodoItem(textTitle.Text, textDetail.Text, DueDate.Date, MyImage.Source as BitmapImage, imauri);
                textTitle.Text = "";
                textDetail.Text = "";
                DueDate.Date = DateTimeOffset.Now;
                Frame.Navigate(typeof(MainPage));
            }
        }

        private void deleteButton(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveTodoItem(textTitle.Text, textDetail.Text, DueDate.Date);
            textTitle.Text = "";
            textDetail.Text = "";
            DueDate.Date = DateTimeOffset.Now;
            Delete.Visibility = Visibility.Collapsed;
            Frame.Navigate(typeof(MainPage));
        }

        private async void selectClick(object sender, RoutedEventArgs e)
        {
            var srcImage = new BitmapImage();
            FileOpenPicker openPicker = new FileOpenPicker();
            //选择视图模式  
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            //openPicker.ViewMode = PickerViewMode.List;  
            //初始位置  
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            //添加文件类型  
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                ApplicationData.Current.LocalSettings.Values["image"] = StorageApplicationPermissions.FutureAccessList.Add(file);
                imageFile = file;
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                {

                    await srcImage.SetSourceAsync(stream);
                    MyImage.Source = srcImage;
                    
                }
            }
        }
    }
}
