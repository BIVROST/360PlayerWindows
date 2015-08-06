// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;

namespace ModelRendering
{
    // Use this namespace here in case we need to use Direct3D11 namespace as well, as this
    // namespace will override the Direct3D11.
    using SharpDX.Toolkit.Graphics;
	using SharpDX.MediaFoundation;
	using System.Threading;
	using System.Windows;
	using Windows.Storage.Streams;
	using Windows.Storage;
	using System.Threading.Tasks;
	

    /// <summary>
    /// Simple SpriteBatchAndFont application using SharpDX.Toolkit.
    /// The purpose of this application is to use SpriteBatch and SpriteFont.
    /// </summary>
    public class ModelRenderingGame : Game
    {
        private GraphicsDeviceManager graphicsDeviceManager;
        private SpriteBatch spriteBatch;
        private SpriteFont arial16BMFont;

        private PointerManager pointer;

        private Model model;

        private List<Model> models;

        private BoundingSphere modelBounds;
        private Matrix world;
        private Matrix view;
        private Matrix projection;

		private GeometricPrimitive primitive;
		private BasicEffect basicEffect;

		public static ManualResetEvent eventReadyToPlay = new ManualResetEvent(false);
		public static bool AbortSignal = false;
		public static MediaEngine mediaEngine;
		public static MediaEngineEx mediaEngineEx;

		SharpDX.Direct3D11.Texture2D texture;
		SharpDX.DXGI.Surface surface;
		long ts;
		int w, h;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelRenderingGame" /> class.
        /// </summary>
        public ModelRenderingGame()
        {
            // Creates a graphics manager. This is mandatory.
            graphicsDeviceManager = new GraphicsDeviceManager(this);
            graphicsDeviceManager.PreferredGraphicsProfile = new FeatureLevel[] { FeatureLevel.Level_11_0, };

            pointer = new PointerManager(this);

            // Setup the relative directory to the executable directory
            // for loading contents with the ContentManager
            Content.RootDirectory = "Content";


			


        }


		private void InitMedia()
		{
			//Media setup

			eventReadyToPlay = new ManualResetEvent(false);
			MediaManager.Startup();
			var mediaEngineFactory = new MediaEngineClassFactory();
			var dxgiManager = new DXGIDeviceManager();
			dxgiManager.ResetDevice((Device)GraphicsDevice);
			MediaEngineAttributes attr = new MediaEngineAttributes();
			attr.VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm;
			attr.DxgiManager = dxgiManager;
			mediaEngine = new MediaEngine(mediaEngineFactory, attr, MediaEngineCreateFlags.None);
			mediaEngine.PlaybackEvent += (playEvent, param1, param2) =>
			{
				switch (playEvent)
				{
					case MediaEngineEvent.CanPlay:
						eventReadyToPlay.Set();
						break;
					case MediaEngineEvent.TimeUpdate:
						break;
					case MediaEngineEvent.Error:
					case MediaEngineEvent.Abort:
					case MediaEngineEvent.Ended:
						//System.Diagnostics.Debug.WriteLine("ENDED OR ERROR");
						break;
				}
			};

			mediaEngineEx = mediaEngine.QueryInterface<MediaEngineEx>();

			LoadMedia();

			if (!eventReadyToPlay.WaitOne(1000))
			{
				Console.WriteLine("Unexpected error: Unable to play this file");
			}



			//Get our video size
			
			mediaEngine.GetNativeVideoSize(out w, out h);


			texture = new SharpDX.Direct3D11.Texture2D((Device)GraphicsDevice, new Texture2DDescription()
			{
				Width = w,
				Height = h,
				MipLevels = 1,
				ArraySize = 1,
				Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
				Usage = ResourceUsage.Default,
				SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None
			});

			surface = texture.QueryInterface<SharpDX.DXGI.Surface>();

			// Play the music
			mediaEngineEx.Loop = false;
			mediaEngineEx.Play();
			//mediaEngineEx.Volume = 0;

			
		}

		private async void LoadMedia()
		{
			StorageFile storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync("Assets/fullhd.mp4");
			string path = storageFile.Path;
			if (storageFile != null)
			{
				var sstr = await storageFile.OpenAsync(FileAccessMode.Read);

				//  streamCom = new ComObject(sstr);
				ByteStream byteStream = new ByteStream(sstr);
				mediaEngineEx.SetSourceFromByteStream(byteStream, path);
			}
		}



        protected override void LoadContent()
        {
			InitMedia();

            // Load the fonts
            //arial16BMFont = Content.Load<SpriteFont>("Arial16");

            // Load the model (by default the model is loaded with a BasicEffect. Use ModelContentReaderOptions to change the behavior at loading time.
            models = new List<Model>();
            foreach (var modelName in new[] { "Dude" })
            {
                //model = Content.Load<Model>(modelName);
                
                // Enable default lighting  on model.
                //BasicEffect.EnableDefaultLighting(model, true);

                //models.Add(model);
            }
            //model = models[0];

            // Instantiate a SpriteBatch
            spriteBatch = ToDisposeContent(new SpriteBatch(GraphicsDevice));

			primitive = GeometricPrimitive.Sphere.New(GraphicsDevice, 6f, 32, true);

			basicEffect = new SharpDX.Toolkit.Graphics.BasicEffect(GraphicsDevice)
			{
				View = Matrix.LookAtRH(new Vector3(0, 0, 5), new Vector3(0, 0, 0), Vector3.UnitY),
				Projection = Matrix.PerspectiveFovRH((float)Math.PI / 4.0f, (float)graphicsDeviceManager.PreferredBackBufferWidth / graphicsDeviceManager.PreferredBackBufferHeight, 0.1f, 100.0f),
				World = Matrix.Identity
			};

			basicEffect.PreferPerPixelLighting = false;
			basicEffect.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(GraphicsDevice, texture);
			basicEffect.TextureEnabled = true;
			basicEffect.LightingEnabled = false;

			var resource = SharpDX.Direct3D11.Resource.FromPointer<SharpDX.Direct3D11.Resource>(texture.NativePointer);

			ShaderResourceViewDescription srvd = new ShaderResourceViewDescription()
			{

			};
			ResourceDimension type = texture.Dimension;

			SharpDX.Direct3D11.Texture2DDescription description = texture.Description;
			srvd.Format = description.Format;
			srvd.Dimension = ShaderResourceViewDimension.Texture2D;
			srvd.Texture2D.MipLevels = description.MipLevels;
			srvd.Texture2D.MostDetailedMip = description.MipLevels - 1;
			
			ShaderResourceView srv2 = new ShaderResourceView((Device)GraphicsDevice, resource, srvd);

			((Device)GraphicsDevice).ImmediateContext.PixelShader.SetShaderResource(0, srv2);
			((Device)GraphicsDevice).ImmediateContext.Flush();

            base.LoadContent();
        }

        protected override void Initialize()
        {
            Window.Title = "Model Rendering Demo";
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            

            // Calculate the bounds of this model
            //modelBounds = model.CalculateBounds();

            // Calculates the world and the view based on the model size
			//const float MaxModelSize = 10.0f;
			//var scaling = MaxModelSize / modelBounds.Radius;
            view = Matrix.LookAtRH(new Vector3(0, 0, 1 * 2.5f), new Vector3(0, 0, 0), Vector3.UnitY);
            projection = Matrix.PerspectiveFovRH(0.9f, (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height, 0.1f, 110.0f);
            world = Matrix.Translation(-modelBounds.Center.X, -modelBounds.Center.Y, -modelBounds.Center.Z) * Matrix.Scaling(1) * Matrix.RotationY((float)gameTime.TotalGameTime.TotalSeconds);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clears the screen with the Color.CornflowerBlue
            GraphicsDevice.Clear(Color.CornflowerBlue);

			if (mediaEngine.OnVideoStreamTick(out ts))
			{
				mediaEngine.TransferVideoFrame(surface, null, new SharpDX.Rectangle(0, 0, w, h), null);
			}

			primitive.Draw(basicEffect);

            // Draw the model
            //model.Draw(GraphicsDevice, world, view, projection);

            // Render the text
            //spriteBatch.Begin();
            //spriteBatch.DrawString(arial16BMFont, "Press the pointer to switch models...\r\nCurrent Model: " + model.Name, new Vector2(16, 16), Color.White);
            //spriteBatch.End();

            // Handle base.Draw
            base.Draw(gameTime);
        }
    }
}
