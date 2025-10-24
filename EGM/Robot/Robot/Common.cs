using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForceControlOf_EGM
{
    class Common
    {
        public class FTModel
        {
            //力
            private double ft_Fx = 0;
            public double FT_Fx
            {
                get { return ft_Fx; }
                set { ft_Fx = value; }
            }
            private double ft_Fy;
            public double FT_Fy
            {
                get { return ft_Fy; }
                set { ft_Fy = value; }
            }
            private double ft_Fz;
            public double FT_Fz
            {
                get { return ft_Fz; }
                set { ft_Fz = value; }
            }
            //力矩
            private double ft_Mx;
            public double FT_Mx
            {
                get { return ft_Mx; }
                set { ft_Mx = value; }
            }
            private double ft_My;
            public double FT_My
            {
                get { return ft_My; }
                set { ft_My = value; }
            }
            private double ft_Mz;
            public double FT_Mz
            {
                get { return ft_Mz; }
                set { ft_Mz = value; }
            }
        }

        public class ABBRobotPos
        {
            private double x;
            private double y;
            private double z;
            private double rx;
            private double ry;
            private double rz;
            private double q1;
            private double q2;
            private double q3;
            private double q4;
            private double a1;
            private double a2;
            private double a3;
            private double a4;
            private double a5;
            private double a6;

            public double X
            {
                get { return x; }
                set { x = value; }
            }

            public double Y
            {
                get { return y; }
                set { y = value; }
            }

            public double Z
            {
                get { return z; }
                set { z = value; }
            }
            public double Rx
            {
                get { return rx; }
                set { rx = value; }
            }
            public double Ry
            {
                get { return ry; }
                set { ry = value; }
            }
            public double Rz
            {
                get { return rz; }
                set { rz = value; }
            }
            public double Q1
            {
                get { return q1; }
                set { q1 = value; }
            }
            public double Q2
            {
                get { return q2; }
                set { q2 = value; }
            }
            public double Q3
            {
                get { return q3; }
                set { q3 = value; }
            }
            public double Q4
            {
                get { return q4; }
                set { q4 = value; }
            }
            public double A1
            {
                get { return a1; }
                set { a1 = value; }
            }

            public double A2
            {
                get { return a2; }
                set { a2 = value; }
            }
            public double A3
            {
                get { return a3; }
                set { a3 = value; }
            }
            public double A4
            {
                get { return a4; }
                set { a4 = value; }
            }
            public double A5
            {
                get { return a5; }
                set { a5 = value; }
            }
            public double A6
            {
                get { return a6; }
                set { a6 = value; }
            }
        }

        internal struct CartesianCorrection
        {
            public void Reset()
            {
                this.X = (this.Y = (this.Z = (this.Q1 = (this.Q2 = (this.Q3 = (this.Q4 =(this.Rx=(this.Ry=(this.Rz= (this.A1 = (this.A2 = (this.A3 = (this.A4 = (this.A5 = (this.A6 = 0.0)))))))))))))));
            }
            public double Q1;
            public double Q2;
            public double Q3;
            public double Q4;
            public double X;
            public double Y;
            public double Z;
            public double Rx;
            public double Ry;
            public double Rz;
            public double A1;
            public double A2;
            public double A3;
            public double A4;
            public double A5;
            public double A6;
        }
    }
}
