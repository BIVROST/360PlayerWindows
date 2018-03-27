Bivrost 360Player development hints
===================================

Quick start
-----------

1. Download the latest Visual Studio with .NET desktop development enabled.
2. Checkout this repository using git clone with the URL:
   ```bash
   git clone git@gitlab.com:BIVROST/360PlayerWindows.git --recursive
   ```
3. Open the solution by doubleclicking `360Player for Windows.sln`.
4. Restore NuGet packages:
   Right click on the *360Player for Windows* solution in the Solution Explorer and choose *Restore NuGet Packages*.
5. Run the *360Player* project. 


Development troubleshooting
---------------------------


A few common problems that occured during the development with quick fixes


### The type or namespace name 'OSVR' could not be found (...)

You forgot to check out the submodules:

```bash
# on an already cloned repository:
git submodule init
git submodule update

# or when cloning:
git clone git@gitlab.com:BIVROST/360PlayerWindows.git --recursive
```


### Net framework 3.5 missing (windows 10)

You need to manually [install .NET 3.5][net-35].
And then restart Visual Studio.

[net-35]: https://answers.microsoft.com/en-us/insider/forum/insider_wintp-insider_install/how-to-instal-net-framework-35-on-windows-10/450b3ba6-4d19-45ae-840e-78519f36d7a4?auth=1


### Space Navigator 3dconnexion doesn't work

Did you install [the drivers][3dconnexion]?
[3dconnexion]: http://www.3dconnexion.pl/service/drivers.html


### Visual Studio works slowly

It might help to disable *Tools -> Options -> Debugging -> General -> Enable UI Debugging Tools for XAML*.


### Unit tests dont work

Change *Test -> Test settings -> Default processor architecture* to *x64*.


### Oculus DLL `LibOVRRT64_1.dll ` not found?

Add to path: `C:\Program Files\Oculus\Support\oculus-runtime`.
This is probably some legacy installation issue.
Issue was not found on new installations.



Projects in the solution
-------------------------

Developed with the player:
* 360Player - main project
* 360Player.Test - unit tests for the project (mostly streaming sources)
* AnalyticsForVR - implementation of BIVROST AnalyticsForVR client for WPF
* BivrostAnalytics - Google Analytics plugin
* Licensing - module that manages the connection to the LicenseNinja license server
* Logger - simple logging utility
* PlayerRemote - example application for the remote feature of the player

Projects imported from other sources:
* 3DconnexionDriver - .NET wrapper for the 3dConnexion Space Navigator 3d mouse ([source][link-3dconnexion])
* ClientKit - OSVR .NET wrapper ([source][link-clientkit])
* OculusWrap - .NET wrapper for Oculus Rift SDK



Integration - the `bivrost` protocol
------------------------------------

TO DO - eng. do zrobienia
TODO - hiszp. wszystko



File naming scheme - stereoscopy auto detection
-----------------------------------------------

TO DO



Features that have been disabled or removed
-------------------------------------------

In the course of development of 360Player for Windows, some features have been created but ultimately removed or hidden.
This is a list of those features with reasons why they are no longer in the default build.


### Watching the video from YouTube (disabled feature)
At some point, YouTube stopped serving high definition videos in a format that could be easily integrated. 
Currently YT serves a 720p video that can be still watched using the old mechanics, and 720p VR is not worth watching.   
The 720p support is still enabled, but is no longer advertised.  
Known issue: does not detect 360 video, will happily play everything as equirectangular.


### Watching the video from Vrideo (removed feature)
The platform shut down at the end of 2016 and the content is no longer available.
Vrideo support has been finally removed in the git commit tagged "vrideo-removed".


### Watching the video from Facebook (removed feature)
Worked at some point, but we stopped keeping up with the constant changes to how Facebook served video.  
Facebook had a special cubemap format. The format itself is still implemented.
Facebook support has been finally removed in the git commit tagged "facebook-removed".


### Remote Control (disabled feature)
At some point the player had a non-published feature with forwarding movie and head position from GearVR to be mirrored on the player.
The feature is hidden behind conditional compilation symbol `FEATURE_REMOTE_CONTROL` and also requires `Features.remote` to be enabled.

There is a test-server in the solution PlayerRemote.  

The Remote Control was just a one time test and is not of production quality.
There are plans to make a proper feature in the future.


### Browser plugins (disabled feature)
For some time the 360Player for Windows asked to install browser plugins to the locally installed Firefox and Chrome browsers.

The plugins added the "Open in 360Player" button on the corner of 360 videos on those sites:
* Youtube
* Facebook
* Vrideo
* Littlstar
* Pornhub

The plugin also added a class to the `body ` element when the domain was `bivrost360.com`. This class was used for feature detection from the `360WebPlayer`'s open-in-native functionality.

This feature has been disabled as the plugins are not kept up to date.

To enable the 360Player part of installing the plugins, use the conditional compilation flag `FEATURE_BROWSER_PLUGINS`. 

The source of the plugins is located in a git repository bundle `360Player/BrowserPlugins/browser-plugins-20160418T1309160200-1496ba5.gitbundle`.


### GhostVR (disabled feature)

GhostVR was one of the AnalyticsForVR session-sink providers. 
It has been disabled since.

To enable the player side, `FEATURE_GHOSTVR` conditional compilation symbol must be added to *360Player*, *AnalyticsForVR* and optionally *360Player.Test* projects in the solution.
There is also a license feature `GhostVR` to be granted.

Please note that the server is no longer available.


### 360WebPlayer integration (removed feature)
Removed in tagged commit `open-in-native-removed`.

(open-in-native)


### Licenses from external server (disabled feature)

Before moving into the R&D access, 360Player for Windows build had its features restricted by an external license server.
Now, no server is queried and basic features are enabled by default without asking.

The licensing feature is now hidden behind conditional compilation symbol `FEATURE_LICENSE_NINJA`.


### Canary builds (removed distribution channel)

This was an alpha channel for the player that was blocked by special license tokens. 
The last canary build has been released long ago and was not updated.
