BIVROST 360Player for Windows
=============================

The easiest way to watch 360-videos and images using a VR headset!

![Screenshot of BIVROST 360Player](Docs/360Player-movie.png)

The 360Player by BIVROST is a video player for immersive spherical videos and images. The desktop application for Windows allows you to play virtual reality media on your PC using a VR headset. It supports all versions of Oculus Rift, HTC Vive, OSVR and Windows Mixed Reality headsets.

Download the player to experience videos in a smooth way and with high performance. Watch images and videos up to 8K and enjoy the finest resolution at 360 degrees.


Key features
------------

* 360 videos in up to 8K resolution
* 3D (stereoscopic) playback
* 360 photo support (photosphere)
* VR Headsets support
  * Oculus: Oculus Rift DK2, Oculus Rift CV1
  * OpenVR (SteamVR): HTC Vive
  * OSVR: OSVR HDK 1.4, OSVR HDK 2.0
  * Windows Mixed Reality (with SteamVR plugin)
* Hardware accelerated video decoding and rendering
* High performance, low latency playback
* Compatible with Windows 8.1 and 10


Requirements
------------

### Minimum requirements
* Dual Core CPU (dual core Intel Celeron N2xx or better)
* 1GB of RAM
* Intel HD 3000 graphic card or better with DX10 support
* 1366x768 screen resolution
* Windows 8.1
* Microsoft .NET 4.5


### Suggested configuration for 4K playback
* Dual Core 3rd generation i5 CPU or better
* 2GB of RAM
* DX11 compatible Nvidia/AMD discrete graphics (GCN for AMD or Kepler for Nvidia)
* 1920x1080 screen resolution
* Windows 10



Supported files and internet services
-------------------------------------
* Local and remote mp4 with H264 or H265/HEVC encoding.
* Local and remote images: JPEG and PNG.
* HLS streams
* Littlstar streams
* Pornhub streams


Using the player
================

To use the 360Player for Windows open a file or an URL address from the Internet using *File → Open File*, *File → Open URL* or the buttons at the center of the player when no movie is playing.

You can also drag and drop a movie or image onto the player or right click on a file and select "Open in 360Player".

![Right click menu on a mp4 file](Docs/Rightclick-menu.png)

Keyboard shortcuts
------------------

Available all the time:

* **`Control` + `O`**: open a file
* **`Control` + `U`**: open an URL from the Internet
* **`Control` + `Q`**: quit 360Player
* **`Control` + `,`**: settings
* **`F1`**: help
* **`F11`** or **doubleclick movie**: toggle fullscreen
* **`Escape`**: exit fullscreen


Available only when movie is playing:

* **`Control` + `S`**: stop
* **`Control` + `R`**: rewind
* **`Space`** or **click movie**: play/pause
* **`L`**: enable little planet projection (only on equirectangular and dome)
* **`N`**: disable little planet projection
* **`Control` + `T`**: enable/disable user headset tracking (look in the direction the headset is looking)
* **Arrow keys** or **`A` `W` `S` `D`**: look around
* **`+`/`-`** or **mouse wheel**: zoom in and out
* **`Control` + `0`**: reset zoom
* **`[`** and **`]`**: skip backwards/forwards by 5 seconds

Gamepad control
---------------

XBox or compatible gamepad is supported.

* **`A`**: play/pause
* **`Y`**: rewind
* **d-pad left/right**: skip backwards/forwards by 5 seconds
* **d-pad up/down**: volume up/down
* **analog left** or **analog right**: look around (both sticks works the same)


3dconnexion SpaceMouse/SpaceNavigator
-------------------------------------

The 3D mice made by 3dconnexion are also supported. 
[Official drivers for the device](http://www.3dconnexion.pl/service/drivers.html) must be installed.

* **Tilt up/down**: look up/down (this option can be inverted in settings)
* **Tilt left/right**: look left/right
* **Rotate clockwise/counter-clockwise**: look left/right

Enabled only when advanced control is enabled in settings:

* **Left button**: play/pause
* **Right button**: rewind
* **Push up/down**: zoom



Support & contact information
=============================

To contact support or fill a bug report, please use [the support form][github-support]. 

[github-support]: https://github.com/BIVROST/360PlayerWindows/issues/new?labels=support

This software is free and can be used only for noncommercial purposes. To purchase the commercial license contact us: contact@bivrost360.com.

### Why is the player crashing on Nvidia Optimus?
You need to run the player with Nvidia graphic card. Click here for more instructions. There are some Optimus configurations that may not work properly. Feel free to write to support if you encountered one of them to help us make the player better.

### I'm getting "File not supported" error when trying to play video.
BIVROST 360Player supports playback of video files and codecs based on Microsoft Media Foundation and common image formats.  
Popular video file formats compatible with the Player: mp4, avi, mov. Popular codecs: H.263, H.264, H.265/HEVC, Windows Media Video. Matroska is not currently supported. Complete list of formats is available [on the MSDN][msdn-file-formats].  
The Player also displays PNG and JPEG photos.  
However, not all formats and resolutions are supported on all systems. It depends on your configuration and Windows version. The greatest variety is available on Windows 10.

[msdn-file-formats]: https://msdn.microsoft.com/pl-pl/library/windows/desktop/dd757927(v=vs.85).aspx

### Do I need the Oculus Rift or OSVR camera to be connected?
The positional tracker (camera) is not needed but it is recommended. It helps to re-position the headset when playback is set up to always look forward in VR. Without position tracking headset gyroscope may drift at times and cause the videos to start in a wrong position.

### Is a VR headset required?
No. Headsets are supported but are not required, You can watch the videos using your computer's screen and look around with a mouse or another device.

### 360Player won't run on Windows N and Windows KN
You have to install Media Feature Pack for correct Windows version: Windows 8.1 ([KB2929699][KB2929699]), Windows 10 ([KB3010081][KB3010081]) or Windows 10 1511 ([KB3099229][KB3099229]).  
If your version is not listed, check [Microsoft's list of Media Feature Packs][media-feature-pack-list] for all applicable Windows versions.

[KB2929699]: https://support.microsoft.com/en-us/kb/2929699
[KB3010081]: https://support.microsoft.com/en-us/kb/3010081
[KB3099229]: https://support.microsoft.com/en-us/kb/3099229
[media-feature-pack-list]: https://support.microsoft.com/en-us/help/3145500/media-feature-pack-list-for-windows-n-editions

### Problems with installation? 
SmartScreen may prevent you from running the setup application. 
Press "run anyway". 

If you do not see the button, press "more info" first. 
It might also help to run the classical installer. 
Windows 8.1 or newer is required, will not work on XP, Vista, 7 or 8.


### OSVR: Video is upside down, half frame visible on top, other on bottom or similar distortions

Try other screen orientations of OSVR in Windows Display Settings


### OSVR: No display on OSVR headset, but the video is playing in the 360Player window and OSVR is selected in the options

360Player for Windows does not currently support OSVR in Direct Mode.
Only the Extended mode is supported.

Either switch OSVR to Extended mode or use install SteamVR and use OpenVR as the 360Player's headset type.



Changelog
=========

The BIVROST 360Player has been in development since 2015, all of the major improvements are listed in [the changelog](CHANGELOG.md).
