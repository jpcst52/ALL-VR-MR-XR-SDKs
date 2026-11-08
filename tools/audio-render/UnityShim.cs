using System;

// A stand-in for the few UnityEngine.Mathf members the kit's synthesis code uses (same behaviour, float math).
namespace UnityEngine
{
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Sin(float x) => (float)Math.Sin(x);
        public static float Cos(float x) => (float)Math.Cos(x);
        public static float Pow(float a, float b) => (float)Math.Pow(a, b);
        public static float Exp(float x) => (float)Math.Exp(x);
        public static float Sqrt(float x) => (float)Math.Sqrt(x);
        public static float Abs(float x) => Math.Abs(x);
        public static float Log(float x) => (float)Math.Log(x);
        public static float Log10(float x) => (float)Math.Log10(x);
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Clamp(float v, float a, float b) => v < a ? a : (v > b ? b : v);
        public static int Clamp(int v, int a, int b) => v < a ? a : (v > b ? b : v);
        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Repeat(float t, float len) => Clamp(t - (float)Math.Floor(t / len) * len, 0f, len);
        public static int FloorToInt(float v) => (int)Math.Floor(v);
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static float Floor(float v) => (float)Math.Floor(v);
        public static float Atan(float v) => (float)Math.Atan(v);
        public static float Tan(float v) => (float)Math.Tan(v);
        public static float SmoothStep(float a, float b, float t) { t = Clamp01((t - a) / (b - a)); return t * t * (3f - 2f * t); }
    }
}
