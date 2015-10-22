using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LegacyPlayer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			this.Loaded += MainWindow_Loaded;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			Window window = Window.GetWindow(this);
			var wih = new WindowInteropHelper(window);
			IntPtr hWnd = wih.Handle;

            RenderForm form = new RenderForm("Media foundation video sink");
            form.ClientSize = new System.Drawing.Size(960, 540);

            MediaDecoderLegacy mp = new MediaDecoderLegacy(form.Handle);
            //mp.OpenUrl(@"Bitspiration3f.mp4");
            //mp.OpenUrl(@"http://bivrost360.com/videos/okrutna-2k-youtube.mp4");
            //mp.OpenUrl(@"https://video-waw1-1.xx.fbcdn.net/hvideo-xpa1/v/t42.1790-2/12034798_870751392974383_522028061_n.mp4?efg=eyJ2ZW5jb2RlX3RhZyI6InFmXzUxMndfY3JmXzIzX21haW5fNC4yX3AxMF9zZCJ9&oh=b6e4bf3217912d2233adc42f3d2368e0&oe=5623C2B8");

            mp.OpenUrl2(@"D:\TestVideos\maroon.m4a", @"D:\TestVideos\maroon-video.mp4");
            bool seek = false;
            RenderLoop.Run(form, () =>
            {
                if (Keyboard.IsKeyDown(Key.S))
                {
                    if (!seek)
                    {
                        mp.Seek(3f);
                        seek = true;
                    }
                }
                else seek = false;

                
                
            });

			

        }
	}
}
