using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Scheduler;

namespace wphThemes
{
    public partial class themes : PhoneApplicationPage
    {

        Collection<theme> gallery = new Collection<theme>();

        Collection<appentry> arr = new Collection<appentry>();
        
        class theme
        {
            public string title;
            public string id;
            public string thumb;
            public string description;
        }

        string baseUrl = "http://windowsphonehacker.com/wphthemes/";

        public themes()
        {
            InitializeComponent();
            updateThemes();
            downloadGalleryAsync();

            WindowsPhoneHacker.wph.bacon("themes2beta1");
        }

        void downloadGalleryAsync()
        {
            WebClient cl = new WebClient();
            cl.DownloadStringAsync(new Uri(baseUrl + "list.txt?c=" + DateTime.Now.Millisecond, UriKind.Absolute));
            cl.DownloadStringCompleted += new DownloadStringCompletedEventHandler(cl_DownloadStringCompleted);
        }

        void cl_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Result != "")
            {
                StringReader str = new StringReader(e.Result);
                XDocument xml = XDocument.Load(str);

                

                foreach (XElement theme in xml.Descendants("theme"))
                {
                    theme th = new theme();
                    th.description = theme.Descendants("description").FirstOrDefault().Value;
                    th.title = theme.Descendants("title").FirstOrDefault().Value;
                    th.id = theme.Descendants("id").FirstOrDefault().Value;
                    th.thumb = theme.Descendants("thumb").FirstOrDefault().Value;

                    gallery.Add(th);
                }

                updateGallery();
            }
        }

        void updateGallery()
        {
            

            
            this.Dispatcher.BeginInvoke(() =>
                {
                    galleryPanel.Children.Clear();

                    foreach (theme th in gallery) {

                        System.Diagnostics.Debug.WriteLine(th.thumb);

                        Image img = new Image();
                        img.Name = th.id;
                        img.Width = 300;
                        img.Height = 500;
                        img.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>((o, e) =>
                        {
                            if (MessageBox.Show("", "Download?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                            {
                                downloadTheme(((Image)o).Name);
                            }
                        });

                        WebClient dler = new WebClient();
                        dler.OpenReadAsync(new Uri("http://windowsphonehacker.com/wphthemes/thumbs/" + th.thumb, UriKind.Absolute));
                        dler.OpenReadCompleted += new OpenReadCompletedEventHandler((o,e) =>
                        {
                            if (e.Error == null)
                            {
                                BitmapImage bmp = new BitmapImage();
                                bmp.SetSource(e.Result);
                                img.Source = bmp;
                            }
                        });


                        //img.Source =

                        TextBlock tbTitle = new TextBlock();
                        tbTitle.FontWeight = FontWeights.Bold;
                        tbTitle.Text = th.title;

                        TextBlock tbDesc = new TextBlock();
                        tbDesc.Text = th.description;

                        galleryPanel.Children.Add(tbTitle);
                        galleryPanel.Children.Add(tbDesc);
                        galleryPanel.Children.Add(img);
                        
                    }
                    
                });
        }

        void downloadTheme(string theme)
        {
            Microsoft.Phone.Shell.SystemTray.SetProgressIndicator(this, new Microsoft.Phone.Shell.ProgressIndicator() { IsIndeterminate = true, IsVisible = true });
            WebClient client = new WebClient();
            client.OpenReadAsync(new Uri(baseUrl + "files/" + theme + ".zip", UriKind.Absolute), "theme_" + theme);
            client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted);
        }

        void client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            Microsoft.Phone.Shell.SystemTray.SetProgressIndicator(this, new Microsoft.Phone.Shell.ProgressIndicator() { IsIndeterminate = true, IsVisible = false });
            if (e.Error == null)
            {
                using (var stor = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication())
                {
                    SharpGIS.UnZipper u = new SharpGIS.UnZipper(e.Result);
                    
                    stor.CreateDirectory((string)e.UserState);

                    
                    foreach (string file in u.FileNamesInZip)
                    {
                        System.Diagnostics.Debug.WriteLine(file);

                        if (file.Contains("/"))
                        {
                            string dir = e.UserState + "/" +  file.Substring(0, file.IndexOf("/"));
                            if (!stor.DirectoryExists(dir))
                            {
                                stor.CreateDirectory(dir);
                            }
                        }

                        using (var str = stor.CreateFile(e.UserState + "\\" + file))
                        {
                            u.GetFileStream(file).CopyTo(str);
                        }
                    }
                }
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show("Theme installed: " + e.UserState, "Success!", MessageBoxButton.OK);
                updateThemes();
            });
        }

        void updateThemes()
        {
            themesPanel.Children.Clear();
            arr.Clear();

            bool themesFound = false;

            int i = 0;

            using (var stor = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication())
            {
                var dirs = stor.GetDirectoryNames("theme_*");
                foreach (string theme in dirs)
                {
                    try
                    {
                        TextBlock tbt = new TextBlock();
                        tbt.Text = theme;
                        themesPanel.Children.Add(tbt);

                        StreamReader reader;
                        XDocument xml;
                        using (var str = stor.OpenFile(theme + "/apps.txt", FileMode.Open))
                        {
                            reader = new StreamReader(str);
                            xml = XDocument.Load(reader);
                        }

                        StackPanel apppanel = new StackPanel();
                        int appi = 0;
                        foreach (XElement application in xml.Descendants("application"))
                        {
                            if (appi == 0)
                            {
                                apppanel = new StackPanel();
                                apppanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
                                themesPanel.Children.Add(apppanel);
                            }
                            appentry app = new appentry();
                            app.name = application.Attribute(XName.Get("name")).Value;
                            app.guid = application.Attribute(XName.Get("guid")).Value;
                            app.image = theme + "/" + application.Attribute(XName.Get("image")).Value;

                            Image img = new Image();

                            using (var str = stor.OpenFile(app.image, FileMode.Open))
                            {
                                BitmapImage bmp = new BitmapImage();
                                bmp.SetSource(str);
                                img.Source = bmp;
                            }
                            img.Width = 170;
                            img.Height = 170;
                            img.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                            img.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

                            img.Margin = new Thickness(2, 2, 2, 2);

                            img.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(img_Tap);

                            apppanel.Children.Add(img);

                            img.Name = "tile___" + i;

                            arr.Add(app);

                            appi++;

                            i++;

                            if (appi > 1)
                            {
                                appi = 0;
                            }
                        }

                        themesFound = true;
                    }
                    catch
                    {
                    }
                }
            }
            

            if (!themesFound)
            {
                TextBlock tb = new TextBlock();
                tb.Text = "You have no themes downloaded!\nSwipe to the right to browse the gallery.";
                themesPanel.Children.Add(tb);
            }
        }

        void img_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int i = Convert.ToInt32(((Image)(sender)).Name.Substring("tile___".Length));

            addTile(arr[i].guid, arr[i].image, arr[i].name, (Image)sender);

        }

        void addTile(String guid, String img, String title, Image imgraw)
        {
            //WritableBitmap for saving
            System.Windows.Media.Imaging.WriteableBitmap wb = new WriteableBitmap(173, 173);

            //Bitmap to store image
            System.Windows.Media.Imaging.BitmapImage bmp = new System.Windows.Media.Imaging.BitmapImage(new Uri("/" + img, UriKind.Relative));
            bmp.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.None; //load it immediately 

            
            //Render it
            wb.Render(imgraw, null);

            //Invalidate the image
            wb.Invalidate();

            //Filename based off the original input image
            string pfilename = "t_" + img.Replace("/", "_") + ".jpg";
            string filename = "/Shared/ShellContent/" + pfilename;

            //Store it in isostore (because managed tiles need to be)
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var st = new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, store))
                {
                    wb.SaveJpeg(st, 173, 173, 0, 100);
                }
            }

            //Create the tile
            var tile = new StandardTileData();

            tile.BackgroundImage = new Uri("isostore:/Shared/ShellContent/" + pfilename, UriKind.Absolute);
            tile.Title = title;

            //Pin it

            ShellTile.Create(new Uri("/go.xaml?c=" + DateTime.Now + "&go=" + guid, UriKind.Relative), tile);
        }

        private void PivotItem_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                
            }
            catch
            {
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //ScheduledAgent

            var oldTask = ScheduledActionService.Find("ScheduledAgent") as PeriodicTask;
            if (oldTask != null)
            {
                ScheduledActionService.Remove("ScheduledAgent");
            }

            Microsoft.Phone.Scheduler.PeriodicTask task = new Microsoft.Phone.Scheduler.PeriodicTask("ScheduledAgent");
            task.Description = "Updates theme live tiles";

            ScheduledActionService.Add(task);
            ScheduledActionService.LaunchForTest("ScheduledAgent", TimeSpan.FromMilliseconds(1200));

        }
    }
}