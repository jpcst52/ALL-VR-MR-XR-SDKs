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

Command line (with Gradle 8.9 and Android SDK configured): `gradle :app:assembleDebug`, then `gradle :app:installDebug`.

## Official references

- [ARCore supported devices](https://developers.google.com/ar/devices)
- [ARCore Android quickstart](https://developers.google.com/ar/develop/java/quickstart)
- [ARCore SDK for Android](https://developers.google.com/ar/develop/java)
- [ARCore releases](https://github.com/google-ar/arcore-android-sdk/releases)
