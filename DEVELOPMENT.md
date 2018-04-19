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


### Oculus DLL `LibOVRRT64_1.dll` not found?

Add to path: `C:\Program Files\Oculus\Support\oculus-runtime`.
This is probably some legacy installation issue.
Issue was not found on new installations.


### Security debugging popup in Visual Studio

You might encounter this popup after starting the solution:

> The security debugging option is set but it requires the Visual Studio 
> hosting process which is unavailable in this debugging configuration.
> The security debugging option will be disabled. 
> This option may be re-enabled in the Security property page. 
> The debugging session will continue without security debugging

Please ignore it and press *OK*. Please check this [stack overflow answer](https://stackoverflow.com/a/45232833/785171) for details.


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
* MOTD - sub project dealing with detecting if any updates are available (and delivering other important messages on startup)
* MOTD.Test - serialization tests of the MOTD project.

Projects imported from other sources:
* 3DconnexionDriver - .NET wrapper for the 3dConnexion Space Navigator 3d mouse ([source][link-3dconnexion])
* ClientKit - OSVR .NET wrapper ([source][link-clientkit])
* OculusWrap - .NET wrapper for Oculus Rift SDK



Integration - the `bivrost` protocol
------------------------------------

The `bivrost` protocol enabled links in a browser to run the 360Player.

The protocol syntax is:

```
bivrost:<url>
    [?version=<version>]
    [&stereoscopy=<stereoscopy type>]
    [&projection=<projection type>]
    [&autoplay=<boolean>]
    [&loop=<boolean>]
```

Where:

* `<url>` is the absolute url to the media file, URI encoded.
* `<version>` is the version of software generating the link
* `<stereoscopy type>` is one of:
  * `autodetect` - guess by filename tags and media ratio (the default).
  * `mono` - whole image used.
  * `side_by_side` - image for left eye is on the left half, and right on the right half of the media.
  * `top_and_bottom` - the left eye is the top half of the image, the right one in the bottom half.
  * `top_and_bottom_reversed` - the left eye is the bottom half of the image, the right one in the top half.
* `<projection>` - only `equirectangular` is supported.

Only the `<url>` is required, all other values are optional.  
All except of the first argument have to be added with an question mark (`?`) or ampersand (`&`) at their start. The question mark before the first one and ampersands after the next ones - just like in a normal URI query string.


File naming scheme - stereoscopy auto detection
-----------------------------------------------

Stereoscopy in local and remote files is guessed from tags in the filename.  
Adding a word in the filename will change the stereoscopy file to that value.

Available words are:
* `SbS` or `LR` - side by side.
* `RL` - reverse side by side.
* `TaB` or `TB` - top and bottom.
* `BaT` or `BT` - top and bottom reversed.
* `mono` or none of the above - monoscopic.

!["Different types of stereoscopy"](Docs/stereoscopy-types.png)

Words have to be separated from other characters in the filename by interpunction characters (`_`, `-`, `,`, `.`, `(`, `)` or space).

For example, `movie_TaB.mp4`, `video(LR).mp4` or `http://example.com/another%20movie%20SbS.mp4` will work properly.


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


### 360WebPlayer two-way integration (removed feature)
The 360WebPlayer had a button that allowed the user to run the content in this native player. 
This was done using `bivrost:` protocol and a local websocket server run by the player.
The websockets server's only duty was to inform a popup window that the link has been successfully opened and that the popup window can be closed. 
The response functionality has been removed.
360Player for Windows can still run content via `bivrost:` protocol and 360WebPlayer still has the button and a popup to do this, the only change is that the popup no longer knows the state of the 360Player.

Removed in tagged commit `open-in-native-removed`.


### Licenses from external server (disabled feature)

Before moving into the R&D access, 360Player for Windows build had its features restricted by an external license server.
Now, no server is queried and basic features are enabled by default without asking.

The licensing feature is now hidden behind conditional compilation symbol `FEATURE_LICENSE_NINJA`.


### Canary builds (removed distribution channel)

This was an alpha channel for the player that was blocked by special license tokens. 
The last canary build has been released long ago and was not updated.
