namespace Bivrost.Bivrost360Player
{
    using SharpDX.Direct3D11;
    using System.Drawing;

    public interface IContentUpdatableFromMediaEngine
	{
		/// <summary>
		/// The renderer receives a texture that it will use but not manage
		/// Should not be disposed.
		/// </summary>
		/// <param name="textureL">Texture that should be used with the left eye or common view</param>
		/// <param name="textureR">Texture that should be used with the right eye, can be null</param>
		void ReceiveTextures(Texture2D textureL, Texture2D textureR);


		/// <summary>
		/// The renderer receives a source bitmap and coordinates for viewports in 
		/// </summary>
		/// <param name="bitmap">Source bitmap with the complete video frame</param>
		/// <param name="coordsL">Coordinates of the bitmap that should be used with the left eye or common view</param>
		/// <param name="coordsR">Coordinates of the bitmap that should be used with the right eye, can be null</param>
		void ReceiveBitmap(Bitmap bitmap, MediaDecoder.ClipCoords coordsL, MediaDecoder.ClipCoords coordsR);


		/// <summary>
		/// The renderer is instructed that the content previously sent is no more valid and should be discarded.
		/// </summary>
		void ClearContent();


		/// <summary>
		/// Sets the projection of the content provided.
		/// Should not affect anything when the content is cleared.
		/// </summary>
		/// <param name="projection">The projection to be set. Autodetect value should not reach here.</param>
		void SetProjection(ProjectionMode projection);
	}
}
