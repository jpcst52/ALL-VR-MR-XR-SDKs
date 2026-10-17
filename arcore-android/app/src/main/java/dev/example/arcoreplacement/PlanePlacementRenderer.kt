package dev.example.arcoreplacement

import android.opengl.GLES11Ext
import android.opengl.GLES20
import android.opengl.GLSurfaceView
import android.opengl.Matrix
import android.util.Log
import android.view.Surface
import com.google.ar.core.Coordinates2d
import com.google.ar.core.Frame
import com.google.ar.core.Plane
import com.google.ar.core.Session
import com.google.ar.core.TrackingState
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.FloatBuffer
import javax.microedition.khronos.egl.EGLConfig
import javax.microedition.khronos.opengles.GL10

class PlanePlacementRenderer : GLSurfaceView.Renderer {
    @Volatile private var session: Session? = null
    @Volatile private var pendingTap: FloatArray? = null
    @Volatile private var displayRotation = Surface.ROTATION_0
    private var cameraTextureId = -1
    private var viewportWidth = 1
    private var viewportHeight = 1
    private var backgroundProgram = 0
    private var cubeProgram = 0
    private var cubeBuffer = 0
    private var projection = FloatArray(16)
    private var view = FloatArray(16)
    private var model = FloatArray(16)
    private var viewModel = FloatArray(16)
    private var mvp = FloatArray(16)
    private var lastFrameError: String? = null

    @Synchronized
    fun setSession(value: Session) {
        session = value
        if (cameraTextureId != -1) value.setCameraTextureName(cameraTextureId)
    }

    @Synchronized
    fun queueTap(x: Float, y: Float) {
        pendingTap = floatArrayOf(x, y)
    }

    fun setDisplayRotation(rotation: Int) {
        displayRotation = rotation
    }

    override fun onSurfaceCreated(gl: GL10?, config: EGLConfig?) {
        GLES20.glClearColor(0f, 0f, 0f, 1f)
        GLES20.glEnable(GLES20.GL_DEPTH_TEST)
        cameraTextureId = createCameraTexture()
        session?.setCameraTextureName(cameraTextureId)
        backgroundProgram = createProgram(BACKGROUND_VERTEX_SHADER, BACKGROUND_FRAGMENT_SHADER)
        cubeProgram = createProgram(CUBE_VERTEX_SHADER, CUBE_FRAGMENT_SHADER)
        val names = IntArray(1)
        GLES20.glGenBuffers(1, names, 0)
        cubeBuffer = names[0]
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, cubeBuffer)
        GLES20.glBufferData(
            GLES20.GL_ARRAY_BUFFER,
            CUBE_VERTICES.size * FLOAT_SIZE,
            createFloatBuffer(CUBE_VERTICES),
            GLES20.GL_STATIC_DRAW
        )
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, 0)
    }

    override fun onSurfaceChanged(gl: GL10?, width: Int, height: Int) {
        viewportWidth = width
        viewportHeight = height
        GLES20.glViewport(0, 0, width, height)
    }

    override fun onDrawFrame(gl: GL10?) {
        val activeSession = session ?: return
        try {
            activeSession.setDisplayGeometry(displayRotation, viewportWidth, viewportHeight)
            val frame = activeSession.update()
            GLES20.glClear(GLES20.GL_COLOR_BUFFER_BIT or GLES20.GL_DEPTH_BUFFER_BIT)
            drawCamera(frame)
            processTap(frame)

            val anchor = synchronized(this) {
                placedAnchor?.takeIf { it.trackingState == TrackingState.TRACKING }
            }
            if (anchor != null) {
                val camera = frame.camera
                camera.getProjectionMatrix(projection, 0, 0.1f, 100f)
                camera.getViewMatrix(view, 0)
                anchor.pose.toMatrix(model, 0)
                val scale = FloatArray(16)
                Matrix.setIdentityM(scale, 0)
                Matrix.scaleM(scale, 0, MARKER_HALF_SIZE, MARKER_HALF_SIZE, MARKER_HALF_SIZE)
                Matrix.multiplyMM(viewModel, 0, model, 0, scale, 0)
                Matrix.multiplyMM(viewModel, 0, view, 0, viewModel, 0)
                Matrix.multiplyMM(mvp, 0, projection, 0, viewModel, 0)
                drawCube()
            }
            lastFrameError = null
        } catch (exception: Exception) {
            val errorKey = "${exception.javaClass.name}:${exception.message}"
            if (lastFrameError != errorKey) {
                Log.e(TAG, "ARCore frame update failed.", exception)
            }
            lastFrameError = errorKey
        }
    }

    private var placedAnchor: com.google.ar.core.Anchor? = null

    private fun processTap(frame: Frame) {
        val tap = synchronized(this) {
            pendingTap.also { pendingTap = null }
        } ?: return

        frame.hitTest(tap[0], tap[1])
            .firstOrNull { hit ->
                val plane = hit.trackable as? Plane
                plane != null && plane.isPoseInPolygon(hit.hitPose) &&
                    plane.trackingState == TrackingState.TRACKING
            }
            ?.let { hit ->
                synchronized(this) {
                    placedAnchor?.detach()
                    placedAnchor = hit.createAnchor()
                }
            }
    }

    private fun drawCamera(frame: Frame) {
        val ndc = floatArrayOf(-1f, -1f, 1f, -1f, -1f, 1f, 1f, 1f)
        val textureCoordinates = FloatArray(8)
        frame.transformCoordinates2d(
            Coordinates2d.OPENGL_NORMALIZED_DEVICE_COORDINATES,
            createFloatBuffer(ndc),
            Coordinates2d.TEXTURE_NORMALIZED,
            createFloatBuffer(textureCoordinates)
        )
        val coordinates = createFloatBuffer(textureCoordinates)
        val positions = createFloatBuffer(ndc)

        GLES20.glDisable(GLES20.GL_DEPTH_TEST)
        GLES20.glUseProgram(backgroundProgram)
        val position = GLES20.glGetAttribLocation(backgroundProgram, "aPosition")
        val texCoord = GLES20.glGetAttribLocation(backgroundProgram, "aTexCoord")
        GLES20.glEnableVertexAttribArray(position)
        GLES20.glVertexAttribPointer(position, 2, GLES20.GL_FLOAT, false, 0, positions)
        GLES20.glEnableVertexAttribArray(texCoord)
        GLES20.glVertexAttribPointer(texCoord, 2, GLES20.GL_FLOAT, false, 0, coordinates)
        GLES20.glActiveTexture(GLES20.GL_TEXTURE0)
        GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, cameraTextureId)
        GLES20.glUniform1i(GLES20.glGetUniformLocation(backgroundProgram, "uTexture"), 0)
        GLES20.glDrawArrays(GLES20.GL_TRIANGLE_STRIP, 0, 4)
        GLES20.glDisableVertexAttribArray(position)
        GLES20.glDisableVertexAttribArray(texCoord)
        GLES20.glEnable(GLES20.GL_DEPTH_TEST)
    }

    private fun drawCube() {
        GLES20.glUseProgram(cubeProgram)
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, cubeBuffer)
        val position = GLES20.glGetAttribLocation(cubeProgram, "aPosition")
        GLES20.glEnableVertexAttribArray(position)
        GLES20.glVertexAttribPointer(position, 3, GLES20.GL_FLOAT, false, 3 * FLOAT_SIZE, 0)
        GLES20.glUniformMatrix4fv(
            GLES20.glGetUniformLocation(cubeProgram, "uMvp"),
            1,
            false,
            mvp,
            0
        )
        GLES20.glDrawArrays(GLES20.GL_TRIANGLES, 0, CUBE_VERTICES.size / 3)
        GLES20.glDisableVertexAttribArray(position)
        GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, 0)
    }

    private fun createCameraTexture(): Int {
        val names = IntArray(1)
        GLES20.glGenTextures(1, names, 0)
        GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, names[0])
        GLES20.glTexParameteri(
            GLES11Ext.GL_TEXTURE_EXTERNAL_OES,
            GLES20.GL_TEXTURE_MIN_FILTER,
            GLES20.GL_LINEAR
        )
        GLES20.glTexParameteri(
            GLES11Ext.GL_TEXTURE_EXTERNAL_OES,
            GLES20.GL_TEXTURE_MAG_FILTER,
            GLES20.GL_LINEAR
        )
        GLES20.glTexParameteri(
            GLES11Ext.GL_TEXTURE_EXTERNAL_OES,
            GLES20.GL_TEXTURE_WRAP_S,
            GLES20.GL_CLAMP_TO_EDGE
        )
        GLES20.glTexParameteri(
            GLES11Ext.GL_TEXTURE_EXTERNAL_OES,
            GLES20.GL_TEXTURE_WRAP_T,
            GLES20.GL_CLAMP_TO_EDGE
        )
        return names[0]
    }

    private fun createProgram(vertexSource: String, fragmentSource: String): Int {
        fun compile(type: Int, source: String): Int {
            val shader = GLES20.glCreateShader(type)
            GLES20.glShaderSource(shader, source)
            GLES20.glCompileShader(shader)
            val status = IntArray(1)
            GLES20.glGetShaderiv(shader, GLES20.GL_COMPILE_STATUS, status, 0)
            check(status[0] == GLES20.GL_TRUE) { GLES20.glGetShaderInfoLog(shader) }
            return shader
        }

        val program = GLES20.glCreateProgram()
        GLES20.glAttachShader(program, compile(GLES20.GL_VERTEX_SHADER, vertexSource))
        GLES20.glAttachShader(program, compile(GLES20.GL_FRAGMENT_SHADER, fragmentSource))
        GLES20.glLinkProgram(program)
        val status = IntArray(1)
        GLES20.glGetProgramiv(program, GLES20.GL_LINK_STATUS, status, 0)
        check(status[0] == GLES20.GL_TRUE) { GLES20.glGetProgramInfoLog(program) }
        return program
    }

    private fun createFloatBuffer(values: FloatArray): FloatBuffer =
        ByteBuffer.allocateDirect(values.size * FLOAT_SIZE)
            .order(ByteOrder.nativeOrder())
            .asFloatBuffer()
            .apply { put(values); position(0) }

    companion object {
        private const val TAG = "PlanePlacement"
        private const val FLOAT_SIZE = 4
        private const val MARKER_HALF_SIZE = 0.06f

        private val BACKGROUND_VERTEX_SHADER = """
            attribute vec4 aPosition;
            attribute vec2 aTexCoord;
            varying vec2 vTexCoord;
            void main() {
                gl_Position = aPosition;
                vTexCoord = aTexCoord;
            }
        """.trimIndent()

        private val BACKGROUND_FRAGMENT_SHADER = """
            #extension GL_OES_EGL_image_external : require
            precision mediump float;
            uniform samplerExternalOES uTexture;
            varying vec2 vTexCoord;
            void main() {
                gl_FragColor = texture2D(uTexture, vTexCoord);
            }
        """.trimIndent()

        private val CUBE_VERTEX_SHADER = """
            attribute vec4 aPosition;
            uniform mat4 uMvp;
            void main() {
                gl_Position = uMvp * aPosition;
            }
        """.trimIndent()

        private val CUBE_FRAGMENT_SHADER = """
            precision mediump float;
            void main() {
                gl_FragColor = vec4(0.1, 0.85, 0.95, 1.0);
            }
        """.trimIndent()

        private val CUBE_VERTICES = floatArrayOf(
            -1f,-1f, 1f,  1f,-1f, 1f,  1f, 1f, 1f,  -1f,-1f, 1f,  1f, 1f, 1f,  -1f, 1f, 1f,
            -1f,-1f,-1f, -1f, 1f,-1f,  1f, 1f,-1f,  -1f,-1f,-1f,  1f, 1f,-1f,  1f,-1f,-1f,
            -1f,-1f,-1f, -1f,-1f, 1f, -1f, 1f, 1f,  -1f,-1f,-1f, -1f, 1f, 1f, -1f, 1f,-1f,
             1f,-1f,-1f,  1f, 1f,-1f,  1f, 1f, 1f,   1f,-1f,-1f,  1f, 1f, 1f,  1f,-1f, 1f,
            -1f, 1f,-1f, -1f, 1f, 1f,  1f, 1f, 1f,  -1f, 1f,-1f,  1f, 1f, 1f,  1f, 1f,-1f,
            -1f,-1f,-1f,  1f,-1f,-1f,  1f,-1f, 1f,  -1f,-1f,-1f,  1f,-1f, 1f, -1f,-1f, 1f
        )
    }
}
