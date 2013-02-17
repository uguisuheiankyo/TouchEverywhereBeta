using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TouchEveryWhereBeta
{
    class TouchArea
    {
        public int ID { get; set; }
        public PointCollection Points { get; set; }
        public String AssignKey { get; set; }
        public Boolean Isselected { get; set; }
        public Polygon PolygonLine { get; set; }
        public Polygon PolygonFill { get; set; }

        public TouchArea()
        {
            Points = new PointCollection();
            Isselected = false;
        }

        public Boolean InsideOutsideDetection(Point p)
        {
            double sumdeg = 0.0;
            Point p1, p2;
            double cp, sp;

            for (int i = 0; i < Points.Count; i++)
            {
                p1 = new Point(Points[i].X - p.X, Points[i].Y - p.Y);
                p2 = new Point(Points[(i + 1) % 4].X - p.X, Points[(i + 1) % 4].Y - p.Y);
                cp = (double)p1.X * p2.X + p1.Y * p2.Y;
                sp = (double)p1.X * p2.Y - p1.Y * p2.X;
                sumdeg += Math.Atan2(sp, cp);
            }

            if ((int)sumdeg >= 1 || (int)sumdeg <= -1)
            {
                return true;
            }

            return false;
        }
    }
}
