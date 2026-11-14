// Minimal stand-ins for the few UnityEngine types Contigu.Core uses, so Core and its EditMode tests can build and run
// outside the Unity editor (dotnet test / the run simulator). Only what Core actually touches is implemented.
using System;

namespace UnityEngine
{
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x;
        public int y;

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2Int zero { get { return new Vector2Int(0, 0); } }
        public static Vector2Int one { get { return new Vector2Int(1, 1); } }
        public static Vector2Int up { get { return new Vector2Int(0, 1); } }
        public static Vector2Int down { get { return new Vector2Int(0, -1); } }
        public static Vector2Int left { get { return new Vector2Int(-1, 0); } }
        public static Vector2Int right { get { return new Vector2Int(1, 0); } }

        public static Vector2Int operator +(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x + b.x, a.y + b.y); }
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x - b.x, a.y - b.y); }
        public static bool operator ==(Vector2Int a, Vector2Int b) { return a.x == b.x && a.y == b.y; }
        public static bool operator !=(Vector2Int a, Vector2Int b) { return !(a == b); }

        public bool Equals(Vector2Int other) { return x == other.x && y == other.y; }
        public override bool Equals(object obj) { return obj is Vector2Int other && Equals(other); }
        public override int GetHashCode() { return x * 73856093 ^ y * 19349663; }
        public override string ToString() { return "(" + x + ", " + y + ")"; }
    }

    public static class Mathf
    {
        public static int RoundToInt(float f) { return (int)Math.Round(f, MidpointRounding.ToEven); }
        public static int FloorToInt(float f) { return (int)Math.Floor(f); }
        public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }
        public static float Round(float f) { return (float)Math.Round(f, MidpointRounding.ToEven); }
        public static float Abs(float f) { return Math.Abs(f); }
        public static int Abs(int i) { return Math.Abs(i); }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static int Clamp(int v, int min, int max) { return v < min ? min : (v > max ? max : v); }
        public static float Clamp(float v, float min, float max) { return v < min ? min : (v > max ? max : v); }
        public static float Clamp01(float v) { return Clamp(v, 0f, 1f); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float Pow(float f, float p) { return (float)Math.Pow(f, p); }
        public static float Sqrt(float f) { return (float)Math.Sqrt(f); }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public static class Application
    {
        public static string persistentDataPath { get { return System.IO.Path.GetTempPath(); } }
    }
}
