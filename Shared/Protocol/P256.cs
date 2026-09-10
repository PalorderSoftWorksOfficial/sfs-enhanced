using System;
using System.Numerics;
using System.Security.Cryptography;

namespace SFSEnhanced.Shared.Protocol
{
    public static class P256
    {
        private static readonly BigInteger Prime = ParseHex("FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFF");
        private static readonly BigInteger CurveA = Prime - 3;
        private static readonly BigInteger CurveB = ParseHex("5AC635D8AA3A93E7B3EBBD55769886BC651D06B0CC53B0F63BCE3C3E27D2604B");
        private static readonly BigInteger Order = ParseHex("FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551");
        private static readonly BigInteger GeneratorXValue = ParseHex("6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296");
        private static readonly BigInteger GeneratorYValue = ParseHex("4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5");
        private const int CoordinateBytes = 32;

        public static byte[] GeneratePrivateKey()
        {
            var bytes = new byte[CoordinateBytes];
            using var rng = RandomNumberGenerator.Create();
            while (true)
            {
                rng.GetBytes(bytes);
                BigInteger value = FromBytes(bytes);
                if (value.Sign > 0 && value < Order) return bytes;
            }
        }

        public static void PublicPoint(byte[] privateKey, out byte[] x, out byte[] y)
        {
            Point publicPoint = Multiply(FromBytes(privateKey), new Point(GeneratorXValue, GeneratorYValue));
            if (publicPoint.IsInfinity) throw new InvalidOperationException("Public point is at infinity.");
            x = ToBytes(publicPoint.X);
            y = ToBytes(publicPoint.Y);
        }

        public static bool IsOnCurve(byte[] x, byte[] y)
        {
            if (x == null || y == null || x.Length != CoordinateBytes || y.Length != CoordinateBytes) return false;
            BigInteger pointX = FromBytes(x);
            BigInteger pointY = FromBytes(y);
            if (pointX.Sign < 0 || pointX >= Prime || pointY.Sign < 0 || pointY >= Prime) return false;
            BigInteger left = pointY * pointY % Prime;
            BigInteger right = (pointX * pointX % Prime * pointX + CurveA * pointX + CurveB) % Prime;
            return left == right;
        }

        public static byte[] DeriveSharedSecret(byte[] privateKey, byte[] peerX, byte[] peerY)
        {
            if (!IsOnCurve(peerX, peerY)) return null;
            Point shared = Multiply(FromBytes(privateKey), new Point(FromBytes(peerX), FromBytes(peerY)));
            if (shared.IsInfinity) return null;
            return ToBytes(shared.X);
        }

        private readonly struct Point
        {
            public readonly BigInteger X;
            public readonly BigInteger Y;
            public readonly bool IsInfinity;

            public Point(BigInteger x, BigInteger y)
            {
                X = x;
                Y = y;
                IsInfinity = false;
            }

            public static Point Infinity => default;
        }

        private static Point Multiply(BigInteger scalar, Point point)
        {
            Point result = Point.Infinity;
            bool hasResult = false;
            var bytes = ToBytes(scalar);
            for (int byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
            {
                for (int bit = 7; bit >= 0; bit--)
                {
                    if (hasResult) result = Double(result);
                    if ((bytes[byteIndex] & (1 << bit)) != 0)
                    {
                        result = hasResult ? Add(result, point) : point;
                        hasResult = true;
                    }
                }
            }
            return result;
        }

        private static Point Double(Point point)
        {
            if (point.IsInfinity || point.Y.Sign == 0) return Point.Infinity;
            BigInteger slope = Mod((3 * point.X * point.X + CurveA) * ModInverse(Mod(2 * point.Y)));
            BigInteger x = Mod(slope * slope - 2 * point.X);
            BigInteger y = Mod(slope * (point.X - x) - point.Y);
            return new Point(x, y);
        }

        private static Point Add(Point left, Point right)
        {
            if (left.IsInfinity) return right;
            if (right.IsInfinity) return left;
            if (left.X == right.X)
            {
                if (Mod(left.Y + right.Y).Sign == 0) return Point.Infinity;
                return Double(left);
            }
            BigInteger slope = Mod((right.Y - left.Y) * ModInverse(right.X - left.X));
            BigInteger x = Mod(slope * slope - left.X - right.X);
            BigInteger y = Mod(slope * (left.X - x) - left.Y);
            return new Point(x, y);
        }

        private static BigInteger Mod(BigInteger value)
        {
            value %= Prime;
            if (value.Sign < 0) value += Prime;
            return value;
        }

        private static BigInteger ModInverse(BigInteger value) => BigInteger.ModPow(Mod(value), Prime - 2, Prime);

        private static BigInteger ParseHex(string hex)
        {
            var bytes = new byte[hex.Length / 2 + 1];
            for (int i = 0; i < hex.Length; i += 2)
                bytes[bytes.Length - 2 - i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return new BigInteger(bytes);
        }

        private static BigInteger FromBytes(byte[] bigEndian)
        {
            var littleEndian = new byte[bigEndian.Length + 1];
            for (int i = 0; i < bigEndian.Length; i++)
                littleEndian[bigEndian.Length - 1 - i] = bigEndian[i];
            return new BigInteger(littleEndian);
        }

        private static byte[] ToBytes(BigInteger value)
        {
            var littleEndian = value.ToByteArray();
            var result = new byte[CoordinateBytes];
            int count = Math.Min(littleEndian.Length, CoordinateBytes);
            for (int i = 0; i < count; i++)
                result[CoordinateBytes - 1 - i] = littleEndian[i];
            return result;
        }
    }
}
