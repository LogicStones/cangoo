using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class PolygonService
    {
		public static string ConvertLatLonObjectsArrayToPolygonString(string rawLatLongArray)
		{
			//rawLatLongArray = "[{\"lat\":32.16015325573281,\"lng\":74.18658540625006},{\"lat\":32.16596596093692,\"lng\":74.20890138525397},{\"lat\":32.153468186339545,\"lng\":74.21199129003912},{\"lat\":32.149980129391864,\"lng\":74.1886453427735}]";
			return rawLatLongArray.Replace("\"lat\":", "").Replace("\"lng\":", "").Replace("[{", "").Replace("}]", "").Replace("},{", "|");
		}

		public static bool IsLatLonExistsInPolygon(string PolygonPoints, string lat, string lng)
		{
			List<Loc> objList = new List<Loc>();
			// sample string should be like this strlatlng = "39.11495,-76.873259|39.114588,-76.872808|39.112921,-76.870373|";
			string[] arr = PolygonPoints.Split('|');
			for (int i = 0; i <= arr.Length - 1; i++)
			{
				string latlng = arr[i];
				string[] arrlatlng = latlng.Split(',');

				Loc er = new Loc(Convert.ToDouble(arrlatlng[0]), Convert.ToDouble(arrlatlng[1]));
				objList.Add(er);
			}
			Loc pt = new Loc(Convert.ToDouble(lat), Convert.ToDouble(lng));

			return IsPointInPolygon(objList, pt);
		}

		private static bool IsPointInPolygon(List<Loc> poly, Loc point)
		{
			int i, j;
			bool c = false;
			for (i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
			{
				if ((((poly[i].Lt <= point.Lt) && (point.Lt < poly[j].Lt)) |
					((poly[j].Lt <= point.Lt) && (point.Lt < poly[i].Lt))) &&
					(point.Lg < (poly[j].Lg - poly[i].Lg) * (point.Lt - poly[i].Lt) / (poly[j].Lt - poly[i].Lt) + poly[i].Lg))
					c = !c;
			}
			return c;
		}

		public class Loc
		{
			private double lt;
			private double lg;

			public double Lg
			{
				get { return lg; }
				set { lg = value; }
			}

			public double Lt
			{
				get { return lt; }
				set { lt = value; }
			}

			public Loc(double lt, double lg)
			{
				this.lt = lt;
				this.lg = lg;
			}
		}
	}
}
