using System.Runtime.Serialization;


namespace Laevo.Model
{
	/// <summary>
	///   Contains all the application settings.
	/// </summary>
	/// <author>Steven Jeuris</author>
	[DataContract]
	class Settings
	{
		/// <summary>
		///   The scale at which the time line is rendered in the WPF 3D environment.
		///   1.0 means it is rendered at full screen resolution.
		/// </summary>
		[DataMember]
		public float TimeLineRenderAtScale = 1f;

		/// <summary>
		///   Determines whether or not the attention lines are visible.
		/// </summary>
		[DataMember]
		public bool EnableAttentionLines = true;
	}
}
