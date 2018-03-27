BIVROST 360Player for Windows Changelog
=======================================

### 2015-08-31 f5c4a8ac / 1.0.0.43
UI improvements and fixes, stability fixes.  
Details:
- Added internal notification center
- Better error handling
- Stability fixes: fixed a crash while opening a file after srcnotsupported error
- Added license windows
- Removed DX11 feature level requirement for testing


### 2015-09-08 8e713523 / 1.0.0.52
New features, auto updater UI.  
Details:
- Changed the repeat button position
- Enhanced notifications (actions, custom action label)
- Added update notifications in a player
- Initial clickonce installer update API usage
- Upcoming headsets support framework
- Fixed "a task was canceled" exception during shutdown
- Added repeat video playback button


### 2015-09-25 e8c6a5f7 / 1.0.0.56
Streaming services support.  
Details:
- Preliminary video streaming services support. Currently supported:
    - Facebook
    - Vrideo
    - LittlStar
    - plain video file url
- Added "LittlePlanet" - stereographic projection for equirectangular videos (L/N keyboard shortcuts)
- Additional UI cleanup
- Added several video playback modes
- Minor stability fixes


### 2015-10-01 7dae49d9 / 1.0.0.62
Statistical tools for video analysis. Bivrost protocol handling for web integration.  
Details:
- Added heatmap tracking 
- Cleaned up player dependencies 
- Added protocol bivrost: handling with websocket integration
- UI update


### 2015-12-04 d4627020 / 1.0.0.128
OSVR headset support. Prerelease version.  
Details:
Version released to Razer.
- Added preliminary OSVR headset support
- Configuration variables bound to UI
- Added youtube-dl integration for 720p videos with automatic updates
- Adde browser plugins installer
- Updated vrideo integration
- Updated protocol integration


### 2016-01-04 c35ac709 / 1.0.0.138
CES 2016 edition.  
Details:
- Fixed Facebook stereo detection bug
- Added HTTP Live Streaming (HLS) media source
- Updated OSVR support
- Fixed OSVR service connectivity issues
- Fixed bad spherical projection with uncommon video aspect ratio


### 2016-01-15 71fcebe2 / 1.0.0.169
New video streaming services integration. Bug fixes.  
Details:
- New video streaming services parser
    - Integrated new vrideo parser and StreamingServices framework
    - Added version information in the "About" window
    - Added movie title support for vrideo
- New system shell integration
    - fixed: player files being copied to a home directory
    - new: proper appref-ms registry hooks
    - new: clipboard replaced by wm_copydata for instance to instance communication
- Updated ClickOnce settings 


### 2016-02-05 6a8abf2 / 1.0.0.175
Remote Control for player (enabled in Debug), new "About" window.


### 2016-03-11 1ccba3d / 1.0.0.178
Updates to Oculus SDK and OSVR integration.



### 2016-05-02 e4ee3b9 / 1.0.0.179
Pornhub streaming support. Added licensing. 
Details:
- Streaming unit tests
- Added dome projection support
- Rebranding of UI
- Licensing support, commercial version.


### 2016-05-05 dff8a63 / 1.0.0.181
Remote control update.  
Details:
- Remote control API example in a separate project
- Forwarding player events to remote control

### 2016-08-07 af8be38 / 1.0.0.182
OpenVR support, published build of previous unreleased features.   
Details:
- Fixed mailto links
- Configuration options description
- Fixed TaB or SbS mode carrying on to mono mode videos
- URLs are  now visible in "Recents" menu
- Cleanup of Oculus integration
- Ambient light shader in Dome projection
- Littlstar and Vrideo update
- SRGB support on Desktop and all Headsets
- Now working With HTC Vive (SteamVR)
- VRUI enabled in OSVR and updated in Oculus.
- Now working with Oculus CV1.
- Merged some OSVR and Oculus integration


### 2016-08-22 b078f26 / 1.0.0.183
Log window and fix of Youtube integration. 


### 2016-09-07 dabd126 / 1.0.0.186
Performance fixes. Fixed occasional blinking.  
Details:
- Streaming services cleanup and fixes.


### 2016-11-24 dcb16ac / 1.0.0.188
Codesigning and projection options  
Details:
- Refactor of features enabling
- Projection options in dropdown menu
- Recents and OpenURI fixes
- Licensing update with feature detection
- New codesigning certificate
- Heatmap protocol update
- GhostVR integration
- ILookProvider integration in headsets
- Tabbed interface in log window
- OpenVR and VRUI fixes


### 2016-12-15 c9b9d9a / 1.0.0.189
Licensing fixes and other minor updates  
Details:
- Option not to provide a license key in builds that do not need licenses
- Fixed License Ninja certificate check
- Added shortcut to stop
- OSVR update


### 2016-12-16 488a4fc / 1.0.0.193
Headset tracking and analytics fixes  
Details:
- Copying headset rotation (works only when a headset is present)
- Headset tracking in google analytics
- Information whether the build is a canary build
- License ninja doesn't fail when https certificate is invalid


### 2017-03-24 0949eea / 1.0.0.196
New input system  
Details:
- Gamepad, Keyboard and 3dConnexion Navigator support
- Little planet fix


### 2017-05-11 fc82a7f (not released as separate version)
GhostVR integration  
Details:
- API connector support
- Displaying web forms after analytics have been sent


### 2017-05-12 d5b7fe4 (not released as separate version)
Cleanup of legacy projects and code  
Details:
- Removed some old testing windows
- Moved HMD implementations to PlayerUI/VR directory
- Removed project AnalyticsTester (empty)
- Removed project BivrostInstaller (not used, unfinished)
- Removed project BivrostWP (abandoned)
- Removed project ChangePublishPlayer (unused)
- Removed project ModelRendering.DrawingSurface (part of BivrostWP)
- Removed EventMode (non functional) 


### 2017-05-26 bf500e1 (not released as separate version)
Logger update and minor fixes  
Details:
- Moved Managed-OSVR to submodule
- Logger re-implementation: added tags to Logger
- Proper closing of child windows when main window is closed


### 2018-03-15 c873b2b / 1.0.0.197
Combined fix of a year of bug reports, source code cleanup
Details:
- Licensing: refactored licensing as a module independent from 360Player
- AnalyticsForVR: refactored heatmaps as a module independent from 360Player
- Fixed the namespace in the default project settings and resources (PlayerUI -> Bivrost360Player)
- Renamed 360Player and Licensing project folders to match project names
- Added zoom to space navigator, enable in options (fix #498)
- Removed headset autodetection
- Moved headset initialization to separate function and started catching oculus exceptions
- OculusWrap included in solution (fix #646)
- Separated headset stuff from ShellViewModel; touched up remote and ui separation
- Changed the icon for the newer vr-viking one
- Disabled little planet for cubemaps (fix #647)
- Browser plugins: refactored functionality to separate files
- Browser plugins: moved functionality behind conditional compile directive `FEATURE_BROWSER_PLUGINS` (fix #642)
- Updated documentation


### 2018-03-26 todotodotodotodo / 1.0.0.198
Fixed Oculus not properly turning off, fixed HLS not working, documentation update, some minor bugfixes and removal of not used features.
Details:
- Fixed the issue with Oculus playback not properly turning off (fix #650)
- Added displaying the changelog when an update is available (MOTD)
- Hidden licensing behind conditional compilation symbol `FEATURE_LICENSE_NINJA` (fix #666) 
- Buy page displays a popup about R&D instead of redirecting to the web page. (ref #667)
- Hidden GhostVR behind conditional compilation symbol `FEATURE_GHOSTVR` (fix #658)
- Removed open-in-native integration (fix #655) 
- Removed NuGet package: bouncy castle (crypto) 
- Removed NuGet package: fleck (websocket server) 
- Removed NuGet package: jint (javascript interpreter)
- Browser plugins moved to a separate directory, added plugin sources as gitbundle
- Docs: screenshot at top of readme, player usage and Microsoft links fixed 
- Docs: development updated
- Warning when Windows is not supported - that is older than 8.1 (fix #197)
- Menu options to zoom in and out (ref #370)
- Reset zoom resets to the same zoom that the scene starts with (fix #368)
- Added headset user direction option in menu (fix #172, fix #495) 
- Fixed settings shortcut (fix #364) 
- Docs: updated keyboard shortcuts (ref #2)
- Added zoom buttons (+/-) and alternate look around with AWSD (fix #305) 
- Added seeking with [ and ] (fix #201) 
- Added keyboard shortcuts to readme (fix #2)
- Docs: documentation update - keyboard, gamepad and 3dconnexion control
- AnalyticsForVR: fix for occasional loss of session data when streaming
- Fix for not working buttons on 3dConnexion devices
- Non-working HLS fix
- Pornhub support updated (fix #371)
- Removed facebook support (close #379, close #376)
- Vrideo support removed, they're out of business for over a year (fix #469)
- Youtube: fixed youtube-dl downloading - now asking (fix #374)
- GhostVR: race condition fixed (fix where you cannot run an update while update is running)
- Remote implementation moved behind conditional compilation (FEATURE_REMOTE_CONTROL)
- Option to change the headset while running
