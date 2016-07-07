How to run BIVROST 360Player for Windows on the OSVR headset
============================================================

Tested on:

* Razer OSVR HDK v1.4 (OSVR Hacker Development Kit)
* Microsoft Windows 10 64 bit.
* Runtime 0.6.1197-g8796762

1.	Download and install the OSVR Runtime for Windows (http://access.osvr.com/binary/osvr-runtime-installer)
2.	Right click desktop, choose "Display Settings", select the OSVR screen (probably 1080x1920 portrait), click on "Advanced display settings" and change resolution to "1920x1080 (Recommended)". Finally press "Apply" and close the window.
3.	Run the OSVR: Start menu -> All programs -> OSVR -> OSVR Server
4.	Run the BIVROST 360Player, select a movie. Video on the OSVR should be visible in a few seconds.

If you have OSVR already installed you have to run it in extended mode (disable direct mode).
To do it, run: Start menu -> All programs -> OSVR -> Disable direct mode.


Troubleshooting
---------------

**"OSVR not detected" in the 360Player**  
Available solutions:

#.	Try to select the "Select OSVR Server" window, click on it and then press enter.
#.	Disconnect power from the OSVR Belt Box, wait a few seconds and connect it again.
#.	Run: Start menu -> All programs -> OSVR -> Disable direct mode
#.	Restart your computer

		
**Video upside down, half frame visible on top, other on bottom or similar distortions**

Try other screen orientations of OSVR in windows Display Settings

	
**No display on OSVR headset, but video is playing in the 360Player window**  
Available solutions:

#.	Disconnect power and USB from the OSVR Belt Box, wait a few seconds and connect it again.
#.	Run: Start menu -> All programs -> OSVR -> Disable direct mode
#.	Restart your computer

