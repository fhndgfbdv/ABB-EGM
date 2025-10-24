using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TigRotX_ABB
{
    class Quaternion
    {
        public double W { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        // 构造函数
        public Quaternion(double w, double x, double y, double z)
        {
            W = w;
            X = x;
            Y = y;
            Z = z;
        }

        // 计算四元数的长度
        public double Length()
        {
            return Math.Sqrt(W * W + X * X + Y * Y + Z * Z);
        }

        // 归一化四元数
        public Quaternion Normalize()
        {
            double len = Length();
            return new Quaternion(W / len, X / len, Y / len, Z / len);
        }

        // 四元数的共轭
        public Quaternion Conjugate()
        {
            return new Quaternion(W, -X, -Y, -Z);
        }

        // 四元数乘法
        public Quaternion Multiply(Quaternion other)
        {
            double w = W * other.W - X * other.X - Y * other.Y - Z * other.Z;
            double x = W * other.X + X * other.W + Y * other.Z - Z * other.Y;
            double y = W * other.Y - X * other.Z + Y * other.W + Z * other.X;
            double z = W * other.Z + X * other.Y - Y * other.X + Z * other.W;
            return new Quaternion(w, x, y, z);
        }

        // 球形线性插值 (Slerp)
        public Quaternion Slerp(Quaternion other, double t)
        {
            double cosHalfTheta = W * other.W + X * other.X + Y * other.Y + Z * other.Z;
            if (cosHalfTheta < 0)
            {
                other = other.Multiply(new Quaternion(-1, 0, 0, 0));
                cosHalfTheta = -cosHalfTheta;
            }
            if (Math.Abs(cosHalfTheta) >= 1.0)
            {
                return this;
            }
            double sinHalfTheta = Math.Sqrt(1.0 - cosHalfTheta * cosHalfTheta);
            if (Math.Abs(sinHalfTheta) < 0.001)
            {
                return new Quaternion(
                    W * (1 - t) + other.W * t,
                    X * (1 - t) + other.X * t,
                    Y * (1 - t) + other.Y * t,
                    Z * (1 - t) + other.Z * t
                );
            }
            double halfTheta = Math.Acos(cosHalfTheta);
            double ratioA = Math.Sin((1 - t) * halfTheta) / sinHalfTheta;
            double ratioB = Math.Sin(t * halfTheta) / sinHalfTheta;
            return new Quaternion(
                W * ratioA + other.W * ratioB,
                X * ratioA + other.X * ratioB,
                Y * ratioA + other.Y * ratioB,
                Z * ratioA + other.Z * ratioB
            );
        }

        //插补计算

        public void CailRun(Quaternion q0, Quaternion q1, int numSteps)
        {
            for (int i = 0; i < numSteps; i++)
            {
                double t = (double)i / (numSteps - 1);
                Quaternion q = q0.Slerp(q1, t);
                Console.WriteLine($"Step {i}: ({q.W}, {q.X}, {q.Y}, {q.Z})");
            }
        }

        // 四元数转欧拉角
        public EulerAngles ToEulerAngles()
        {
            double roll = Math.Atan2(2 * (W * X + Y * Z), 1 - 2 * (X * X + Y * Y));
            double pitch = Math.Asin(2 * (W * Y - Z * X));
            double yaw = Math.Atan2(2 * (W * Z + X * Y), 1 - 2 * (Y * Y + Z * Z));
            return new EulerAngles(roll, pitch, yaw);
        }
    }


    class EulerAngles
    {
        public double Roll { get; set; }
        public double Pitch { get; set; }
        public double Yaw { get; set; }

        public EulerAngles(double roll, double pitch, double yaw)
        {
            //Roll = roll;
            //Pitch = pitch;
            //Yaw = yaw;
            // 将角度转换为弧度
            Yaw = yaw * Math.PI / 180.0;
            Pitch = pitch * Math.PI / 180.0;
            Roll = roll * Math.PI / 180.0;
        }

        // 欧拉角转四元数
        public Quaternion ToQuaternion()
        {
            double cy = Math.Cos(Yaw * 0.5);
            double sy = Math.Sin(Yaw * 0.5);
            double cp = Math.Cos(Pitch * 0.5);
            double sp = Math.Sin(Pitch * 0.5);
            double cr = Math.Cos(Roll * 0.5);
            double sr = Math.Sin(Roll * 0.5);

            double w = cr * cp * cy + sr * sp * sy;
            double x = sr * cp * cy - cr * sp * sy;
            double y = cr * sp * cy + sr * cp * sy;
            double z = cr * cp * sy - sr * sp * cy;

            return new Quaternion(w, x, y, z);
        }
    }

}
