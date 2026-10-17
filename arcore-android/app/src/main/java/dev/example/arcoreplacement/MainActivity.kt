package dev.example.arcoreplacement

import android.Manifest
import android.app.Activity
import android.content.pm.PackageManager
import android.graphics.Color
import android.opengl.GLSurfaceView
import android.os.Bundle
import android.util.Log
import android.view.Gravity
import android.view.MotionEvent
import android.widget.FrameLayout
import android.widget.TextView
import android.widget.Toast
import com.google.ar.core.ArCoreApk
import com.google.ar.core.Config
import com.google.ar.core.Session

class MainActivity : Activity() {
    private lateinit var surfaceView: GLSurfaceView
    private lateinit var renderer: PlanePlacementRenderer
    private var session: Session? = null
    private var installRequested = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        renderer = PlanePlacementRenderer()
        surfaceView = GLSurfaceView(this).apply {
            setEGLContextClientVersion(2)
            setRenderer(renderer)
            renderMode = GLSurfaceView.RENDERMODE_CONTINUOUSLY
            setOnTouchListener { _, event ->
                if (event.action == MotionEvent.ACTION_UP) {
                    renderer.queueTap(event.x, event.y)
                }
                true
            }
        }

        val instructions = TextView(this).apply {
            text = "Move to find a floor, then tap to place a marker"
            setTextColor(Color.WHITE)
            textSize = 16f
            setPadding(24, 20, 24, 20)
            setBackgroundColor(0x88000000.toInt())
            gravity = Gravity.CENTER
        }
        val root = FrameLayout(this).apply {
            addView(surfaceView, FrameLayout.LayoutParams(-1, -1))
            addView(
                instructions,
                FrameLayout.LayoutParams(-1, FrameLayout.LayoutParams.WRAP_CONTENT, Gravity.TOP)
            )
        }
        setContentView(root)
    }

    override fun onResume() {
        super.onResume()
        renderer.setDisplayRotation(windowManager.defaultDisplay.rotation)
        if (!hasCameraPermission()) {
            requestPermissions(arrayOf(Manifest.permission.CAMERA), CAMERA_PERMISSION_REQUEST)
            return
        }
        resumeArSession()
        surfaceView.onResume()
    }

    override fun onConfigurationChanged(newConfig: android.content.res.Configuration) {
        super.onConfigurationChanged(newConfig)
        renderer.setDisplayRotation(windowManager.defaultDisplay.rotation)
    }

    override fun onPause() {
        super.onPause()
        surfaceView.onPause()
        session?.pause()
    }

    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == CAMERA_PERMISSION_REQUEST &&
            grantResults.firstOrNull() == PackageManager.PERMISSION_GRANTED
        ) {
            resumeArSession()
            surfaceView.onResume()
        } else if (requestCode == CAMERA_PERMISSION_REQUEST) {
            Toast.makeText(this, "Camera permission is required for AR.", Toast.LENGTH_LONG).show()
            finish()
        }
    }

    private fun hasCameraPermission() =
        checkSelfPermission(Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED

    private fun resumeArSession() {
        try {
            when (ArCoreApk.getInstance().requestInstall(this, !installRequested)) {
                ArCoreApk.InstallStatus.INSTALL_REQUESTED -> {
                    installRequested = true
                    return
                }
                ArCoreApk.InstallStatus.INSTALLED -> Unit
            }

            val currentSession = session ?: Session(this).also { session = it }
            currentSession.configure(
                Config(currentSession).apply {
                    planeFindingMode = Config.PlaneFindingMode.HORIZONTAL
                }
            )
            renderer.setSession(currentSession)
            currentSession.resume()
        } catch (exception: Exception) {
            Log.e(TAG, "Unable to start the ARCore session.", exception)
            Toast.makeText(this, "Unable to start ARCore. Check that Google Play Services for AR is installed.", Toast.LENGTH_LONG).show()
            session?.close()
            session = null
            finish()
        }
    }

    companion object {
        private const val TAG = "PlanePlacement"
        private const val CAMERA_PERMISSION_REQUEST = 1
    }
}
