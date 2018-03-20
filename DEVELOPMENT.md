Bivrost 360Player development hints
===================================

Quick start
-----------

1. Download the latest Visual Studio with .NET desktop development enabled.
2. Checkout this repository using git clone with the URL 



Troubleshooting
---------------

1. git submodule update for Managed-OSVR/ClientKit

2. Net framework 3.5 missing (windows 10)

https://answers.microsoft.com/en-us/insider/forum/insider_wintp-insider_install/how-to-instal-net-framework-35-on-windows-10/450b3ba6-4d19-45ae-840e-78519f36d7a4?auth=1

Restart Visual Studio



3. Space Navigator 3d Connexion:
http://www.3dconnexion.pl/service/drivers.html

4. If Visual Studio works slow, it might help to disable Tools -> Options -> Debugging -> General -> Enable UI Debugging Tools for XAML

5. Unit tests dont work? Test -> Test settings -> Default processor architecture -> x64


6. Oculus dll not found? Add to path: C:\Program Files\Oculus\Support\oculus-runtime (some old dev installation issue?)




Requirements
------------

Windows 8 (?), 8.1 or 10






Projects in the solution:
* 360Player - main project
* 360Player.Test - unit tests for the project (mostly streaming sources)
* AnalyticsForVR - implementation of BIVROST AnalyticsForVR client for WPF
* BivrostAnalytics - Google Analytics plugin
* Licensing - module that manages connection to the LicenseNinja license server
* Logger - simple logging utility
* PlayerRemote - example application for the remote feature of the player

Projects imported from other sources:
* 3DconnexionDriver - .NET wrapper for the 3dConnexion Space Navigator 3d mouse ([source][link-3dconnexion])
* ClientKit - OSVR .NET wrapper ([source][link-clientkit])
* OculusWrap - .NET wrapper for Oculus Rift SDK



Features that have been disabled
--------------------------------

In the course of development of 360Player for Windows, some features have been created but ultimately removed or hidden.
This is a list of these features and reasons why they are no more in the default build.

### Watching video from YouTube
At some point, YouTube stopped serving high definition video in a format that could be easily integrated. 
Currently YT serves 720p video that can be still watched using the old mechanics, and 720p VR is not worth watching.   
The 720p support is still enabled, but is no longer advertised.  
Known issue: does not detect 360 video, will happily play everything as equirectangular.


### Watching video from VRideo
The platform shut down at the end of 2016 and the content is no more available.
VRideo support has been finally removed in the git commit tagged "vrideo-removed".


### Watching video from Facebook
Worked at some point, but we stopped keeping up with constant changes to how Facebook serves video.  
Facebook had a special cubemap format. The format itself is still implemented.
Facebook support has been finally removed in the git commit tagged "facebook-removed".


### Remote Control
At some point the player had a non-published feature with forwarding movie and head position from GearVR to be mirrored on the player.
The feature is hidden behind conditional compilation symbol `FEATURE_REMOTE_CONTROL` and also requires `Features.remote` to be enabled.

There is a test-server in the solution PlayerRemote.  

The Remote Control was just a one time test and is not of production quality.
There are plans to make a proper feature of this in the future.


### Browser plugins
`FEATURE_BROWSER_PLUGINS`

### GhostVR


### ClickOnce


### 360WebPlayer integration
