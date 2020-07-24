## About this project

This project constitutes a Unity native plugin and a set of Unity C# script components allow third parties to incorporate Servo browser windows into Unity scenes.

For more background information, see the blog post at https://blog.mozvr.com/a-browser-plugin-for-unity/

## Structure of this repository

1. The Unity project is `src/ServoUnity`, with a sample scene at `src/ServoUnity/Assets/ExampleScene.unity`.
2. Native code for the Unity plugin is in `src/ServoUnityPlugin`.
3. The compiled plugin will be placed in `src/ServoUnity/Assets/Plugins`.
4. The Unity C# scripts designed to be used by the user's application are in `src/ServoUnity/Assets/Scripts`.

## License

[The license file](License) sets out the full license text.

The code is licensed under the MPL2.

This license is compatible with use in a proprietary and/or commercial application.

## Building from source

### Prerequisites

What | Minimum version | Where to download 
---- | --------------- | ------------
Unity | 2019.3 | <https://unity.com>
For macOS: Xcode tools |  | <https://developer.apple.com>

During this development phase of the project, only macOS is supported. 

### libsimpleservo2

Servo itself enters the plugin through the wrapper library `libsimpleservo2`. You can find a binary build of libsimpleservo2 at https://github.com/philip-lamb/servo/releases, or you can build it from source. This fork of the servo repository will soon be merged back to the main servo repo and these instructions will be amended once that is complete.

Build libsimpleservo2 from source:
1. `git clone --branch phil-ss2-headless https://github.com/philip-lamb/servo.git`
2. `cd servo`
3. `./mach bootstrap`
4. `./mach build --libsimpleservo2`
The release libraries will be built by default to path `target/release`.

### The servo-unity plugin build

The Xcode project for the plugin is at `src/ServoUnityPlugin/macOS/servo_unity.xcodeproj`. Compiling this project requires linking to Unity's plugin headers which are normally contained inside the Unity application bundle. Check that the build setting for header search paths is correct for the version of Unity installed on your system.

Prior to building, a build step removes any previous plugin build (`servounity.bundle`) from the Unity project's `Plugins` folder. 
The Xcode project builds the plugin bundle directly into the same folder. If you wish to change this behaviour, uncheck "deployment postprocessing" in the Xcode build settings.

## Operating the plugin inside the Unity Editor

The plugin can run inside the Unity Editor, but some setup is required first:]
1. At present, the plugin can be run and stopped once per Editor session. (This is due to the fact that Unity does not unload and reload native plugins between runs in the Editor.) You'll need to quit and relaunch the Editor before running again.
2. The plugin currently looks for required GStreamer plugins at the same path as the executable; when running in the Unity Editor, this is unfortunately the path to the Unity Editor executable inside the Unity app bundle. We're working on fixing this, but in the meantime, prior to use, you'll need to navigate to the libsimpleservo2 binary folder, and execute the following command: `sudo cp libgstapp.so libgstlibav.so libgstvideoconvert.so libgstaudioconvert.so libgstmatroska.so libgstvideofilter.so libgstaudiofx.so libgstogg.so libgstvideoparsersbad.so libgstaudioparsers.so libgstopengl.so libgstvideoscale.so libgstaudioresample.so libgstopus.so libgstvolume.so libgstautodetect.so libgstplayback.so libgstvorbis.so libgstcoreelements.so libgstproxy.so libgstvpx.so libgstdeinterlace.so libgstrtp.so libgstwebrtc.so libgstinterleave.so libgsttheora.so libgstisomp4.so libgsttypefindfunctions.so "/Applications/Unity/Hub/Editor/2019.3.13f1/Unity.app/Contents/MacOS/"` You might need to amend the final path for a different version of the Unity Editor.



