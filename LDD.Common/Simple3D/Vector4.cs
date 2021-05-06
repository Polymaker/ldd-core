﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LDD.Common.Simple3D
{
    public struct Vector4
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        [XmlIgnore]
        public Vector3 Xyz
        {
            get => new Vector3(X, Y, Z);
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        [XmlIgnore]
        public bool IsEmpty => float.IsNaN(X);

        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4(Vector3 vector, float w)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
            W = w;
        }


        #region Operators

        public static Vector4 operator *(Vector4 vec, float number)
        {
            return new Vector4(vec.X * number, vec.Y * number, vec.Z * number, vec.W * number);
        }

        public static Vector4 operator /(Vector4 vec, float number)
        {
            return new Vector4(vec.X / number, vec.Y / number, vec.Z / number, vec.W / number);
        }

        public static Vector4 operator +(Vector4 left, Vector4 right)
        {
            return new Vector4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
        }

        public static Vector4 operator -(Vector4 vec1, Vector4 vec2)
        {
            return new Vector4(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z, vec1.W - vec2.W);
        }

        public static explicit operator Vector4(Vector4d vector)
        {
            return new Vector4((float)vector.X, (float)vector.Y, (float)vector.Z, (float)vector.W);
        }

        #endregion

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector4))
                return false;
            return Equals((Vector4)obj);
        }

        public bool Equals(Vector4 other)
        {
            return Equals(other, 0.0001f);
        }

        public bool Equals(Vector4 other, float tolerence)
        {
            if (IsEmpty || other.IsEmpty)
                return IsEmpty == other.IsEmpty;
            return Distance(this, other) <= tolerence;
            //return Math.Abs(X - other.X) < tolerence && Math.Abs(Y - other.Y) < tolerence && Math.Abs(Z - other.Z) < tolerence;
        }

        public static float Distance(Vector4 left, Vector4 right)
        {
            var dx = left.X - right.X;
            var dy = left.Y - right.Y;
            var dz = left.Z - right.Z;
            var dw = left.W - right.W;
            return (float)Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz) + (dw * dw));
        }

        public override string ToString()
        {
            return $"[{X};{Y};{Z};{W}]";
        }

        public static readonly Vector4 Zero = new Vector4();

        public static readonly Vector4 Empty = new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
    }
}
