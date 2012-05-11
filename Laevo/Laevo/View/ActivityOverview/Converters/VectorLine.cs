using System.Windows;


namespace Laevo.View.ActivityOverview.Converters
{
	/// <summary>
	///   A helper class to perform line intersections.
	///   TODO: Create a reuseable line class in Whathecode.System.
	/// </summary>
	class VectorLine
	{
		readonly Vector _from;
		readonly Vector _to;


		/// <summary>
		///   Create a new object representing a line going through the given two points.
		/// </summary>
		public VectorLine( Vector from, Vector to )
		{
			_from = from;
			_to = to;
		}

		/// <summary>
		///   Get the intersection with the other line. (This assumes there always is an intersection!)
		/// </summary>
		/// <param name = "line">The line to find the intersection with.</param>
		/// <returns>The point at which the two lines intersect.</returns>
		public Vector Intersection( VectorLine line )
		{
			Vector p1 = _from;
			Vector p2 = _to;
			Vector p3 = line._from;
			Vector p4 = line._to;

			double denominator = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);
			return new Vector(
				( (p1.X * p2.Y - p1.Y * p2.X) * (p3.X - p4.X) - (p1.X - p2.X) * (p3.X * p4.Y - p3.Y * p4.X) ) / denominator,
				( (p1.X * p2.Y - p1.Y * p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X * p4.Y - p3.Y * p4.X) ) / denominator );			
		}
	}
}
