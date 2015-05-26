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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PlayerUI
{
	/// <summary>
	/// Interaction logic for ShellView.xaml
	/// </summary>
	public partial class ShellView : Window
	{
		public ShellView()
		{
			InitializeComponent();
			//System.Threading.ManualResetEvent waitLock = new System.Threading.ManualResetEvent(false);

			//Task.Factory.StartNew(() =>
			//{
			//	Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			//	ofd.Filter = "Video MP4|*.mp4";
			//	bool? result = ofd.ShowDialog();
			//	if (result.HasValue)
			//		if (result.Value == true)
			//		{
			//			BivrostPlayerPrototype.PlayerPrototype.TextureCreated += (tex) =>
			//			{
			//				Caliburn.Micro.Execute.OnUIThread(() =>
			//				{
			//					this.Canvas1.Scene = new Scene();
			//					this.Canvas1.SetVideoTexture(tex);
			//				});
			//			};
			//			BivrostPlayerPrototype.PlayerPrototype.Play(ofd.FileName);
			//		}
			//});

			//waitLock.WaitOne(000);
			//this.Canvas1.extTexture = BivrostPlayerPrototype.PlayerPrototype.externalRenderTargetTexture;

			Color gray = Colors.LightGray;
			this.Canvas1.ClearColor = new SharpDX.Color4(gray.R / 255F, gray.G / 255F, gray.B / 255F, gray.A / 255F);

		}
	}
}
