# Project Description
.NET wrapper for the Oculus Rift SDK.

This wrapper can be used with DirectX or OpenGL, and doesn't doesn't depend on SharpDX or other libraries.

# Please Note
As of May 2016 I have decided to shut down the OculusWrap project. 

This means that I will not be making any more modifications to the project or update the project to match any future versions of the Oculus SDK.

I will leave the currently working source code and binaries for anyone who wish to use them as they are or to create your own derivation of the project, should you choose to do so. 

Should you feel like sharing your own creations, based on the OculusWrap project, please feel free to write a post in the OculusWrap discussions forum such that others may learn about your projects.


# Final available release
Last updated 23nd of April 2016. 

The 2.3.2.1 release of OculusWrap is here. This release is built for Oculus SDK 1.3.2.0.

This release of OculusWrap bypasses the use of the LibOvr.lib library, provided by the Oculus SDK, and instead directly accesses the Oculus runtime libraries. This means that the DllOVR.dll x86 and x64 assemblies, previously included in the OculusWrap project, are no longer needed.

The release also adds support for unsafe execution of the platform invoke methods used to wrap the Oculus SDK methods. The unsafe implementation provides a faster execution, by bypassing the managed security checks that are normally performed when transitioning from managed to unmanaged code.

You may notice that the download for the _OculusWrap 2.3.2.1 DirectX 11 Demo_ is marked as being in beta. This doesn't mean that it's not finished, it's just an indication that I'm currently the only person to have tested it so it may still contain errors I haven't found yet.

_Please note that Oculus SDK version 1.3.2.0 supports DirectX 11, DirectX 12 and OpenGL._

# Summary
This project allows developers to use the Oculus SDK in their own .NET projects. 

While OculusWrap does not itself depend on SharpDX, SharpDX is utilized in one of the unit test projects and in the demo application, for demonstrating how to interface with DirectX. There is nothing preventing developers from using OculusWrap for SlimDX, OpenGL or other DirectX or OpenGL implementations.

To compile the source code yourself, you will first need to download the Oculus SDK for Windows, version 1.3.2.0, from the Oculus website ([https://developer.oculus.com/downloads/](https://developer.oculus.com/downloads/)).

_Please note that although the OculusWrap supports DirectX 11, DirectX 12 and OpenGL, the project has currently only been tested with DirectX 11._

# Contents
The OculusWrap source code consists of the following:


**OculusWrap** (AnyCPU)
A C# project containing the raw wrapper class for accessing the methods in the LibOVR library and a managed wrapper which handles the marshalling and memory allocation code necessary to communicate with the native DllOVR dll.

> NOTE: the following have been removed but are still available in [The CodePlex Archive](https://archive.codeplex.com/?p=oculuswrap).

**OculusWrapTest** (AnyCPU)
A Visual Studio unit test, testing those of the methods in the DllOVR library, which are identical for  DirectX 11, DirectX 12 and OpenGL.

**OculusWrapTest-DX11** (AnyCPU)
A Visual Studio unit test, testing those of the methods in the DllOVR library, which are used for DirectX 11. This is a superset of the OculusWrapTest project. 

_This test project depends on SharpDX for DirectX 11._

**OculusWrapDemo-DX11** (AnyCPU)
A Windows desktop application which demonstrates how to use the OculusWrap library. The library uses DirectX 11 to show a rotating cube in front of the head mounted display. Head tracking is enabled, allowing you to look at the rotating cube from multiple angles. 

_This demo project depends on SharpDX for DirectX 11._