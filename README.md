## Structure of this repository

1. The Unity project is `src/ServoUnity`, with a sample scene at `src/ServoUnity/Assets/ExampleScene.unity`.
2. Native code for the Unity plugin is in `src/ServoUnityPlugin`.
3. The compiled plugin is in `src/ServoUnity/Assets/Plugins`.
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
Servo | | https://github.com/servo/servo

During this development phase of the project, only macOS is supported. The Xcode project for the plugin is at `src/ServoUnityPlugin/macOS/servo_unity.xcodeproj`. Compiling this project requires linking to Unity's plugin headers which are normally contained inside the Unity application bundle. Check that the build setting for header search paths is correct for the version of Unity installed on your system.

The Xcode project builds the plugin bundle directly into the Unity project's `Plugins` folder. Note the `.meta` files Unity places alongside all files and folders will cause a code-signing error, so you are advised to remove the previous plugin before compiling.
