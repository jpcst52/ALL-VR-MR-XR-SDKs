# ARCore plane placement

A small native Android AR example. ARCore detects horizontal planes; tap a detected floor to place or replace a cyan 12 cm cube anchored at that hit-test pose. No cloud service or API key is used.

## Requirements

- Android Studio with Android SDK Platform 35 and JDK 17.
- Gradle 8.9 for command-line builds (or configure Android Studio to use Gradle 8.9).
- Android 7.0/API 24 or later on an ARCore-supported phone/tablet with Google Play Services for AR installed. Check Google's supported-device list before testing; an emulator is not a substitute for the camera/plane-detection experience.

## Build and run

1. Open `arcore-android` in Android Studio and allow Gradle sync. The project uses the official `com.google.ar:core:1.46.0` artifact from Google's Maven repository.
2. Connect an ARCore-supported device, enable USB debugging, and run the `app` configuration. Accept the camera permission and any Google Play Services for AR installation prompt.
3. Slowly move the camera to scan a floor. Tap the detected floor to place the cube; tap again to move it.

Command line: the Gradle wrapper is included, so no separate Gradle install is needed. With `JAVA_HOME` pointing at JDK 17 and `ANDROID_HOME` at the Android SDK, run `./gradlew :app:assembleDebug` (Windows: `gradlew.bat :app:assembleDebug`), then `./gradlew :app:installDebug` with a device connected. The debug APK is written to `app/build/outputs/apk/debug/app-debug.apk`. The first build downloads Gradle 8.9 and the Android Gradle Plugin, which can take several minutes.

The build has been verified to produce an APK (JDK 17, SDK Platform 35). Running it on an emulator is not verified: with a Google APIs x86_64 Android 14 image and the ARCore emulator APK sideloaded, the app launches but ARCore's session creation failed in my testing. Use a physical ARCore-supported device, or an emulator image with Google Play, to try plane detection.

## Official references

- [ARCore supported devices](https://developers.google.com/ar/devices)
- [ARCore Android quickstart](https://developers.google.com/ar/develop/java/quickstart)
- [ARCore SDK for Android](https://developers.google.com/ar/develop/java)
- [ARCore releases](https://github.com/google-ar/arcore-android-sdk/releases)

## Run it

Toolchain: Android Studio (or JDK 17 plus the Android SDK) and a physical ARCore-supported phone. Open `arcore-android`, run `app`, scan the floor, tap to place the cube.

## What was verified in this repository

The Gradle build producing an APK is the author's earlier result (JDK 17, SDK Platform 35), not re-run in the latest documentation pass. Plane detection was **not verified on a physical device or a working emulator**, and no AR headset is involved.
