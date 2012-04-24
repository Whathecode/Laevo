﻿using System;
using System.Windows;
using System.Windows.Media.Animation;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;


namespace Laevo.View.ActivityOverview
{
	[WpfControl( typeof( Properties ))]
	public class VisibleIntervalAnimation : AnimationTimeline
	{
		[Flags]
		public enum Properties
		{
			StartVelocity,
			ConstantDeceleration
		}


		public TimeLineControl TimeLine { get; set; }


		[DependencyProperty( Properties.StartVelocity )]
		public long? StartVelocity { get; set; }

		[DependencyProperty( Properties.ConstantDeceleration )]
		public double? ConstantDeceleration { get; set; }

		[DependencyPropertyChanged( Properties.ConstantDeceleration )]
		public static void OnConstantDecelerationChanged( DependencyObject o, DependencyPropertyChangedEventArgs args )
		{
			((VisibleIntervalAnimation)o).SetDuration();
		}

		[DependencyPropertyChanged( Properties.StartVelocity )]
		public static void OnFromChanged( DependencyObject o, DependencyPropertyChangedEventArgs args )
		{
			((VisibleIntervalAnimation)o).SetDuration();
		}

		void SetDuration()
		{
			if ( ConstantDeceleration != null && StartVelocity != null )
			{
				if ( StartVelocity.Value == 0 )
				{
					Duration = new Duration( TimeSpan.Zero );
				}
				else
				{
					long ticks = (long)Math.Abs( StartVelocity.Value / ConstantDeceleration.Value );
					Duration = new Duration( TimeSpan.FromTicks( ticks ) );
				}
			}
		}


		protected override Freezable CreateInstanceCore()
		{
			return new VisibleIntervalAnimation();
		}

		public override Type TargetPropertyType
		{
			get { return typeof( Interval<DateTime> ); }
		}

		public override object GetCurrentValue( object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock )
		{
			var from = (Interval<DateTime>)defaultOriginValue;
			if ( animationClock.CurrentTime == null || ConstantDeceleration == null || StartVelocity == null )
			{
				return from;
			}
			
			long time = animationClock.CurrentTime.Value.Ticks;
			double currentVelocity = StartVelocity.Value - (ConstantDeceleration.Value * time);
			long displacement = (long)(((StartVelocity.Value + currentVelocity) * time) / 2.0);

			if ( from.Start.Ticks - displacement > DateTime.MinValue.Ticks && from.End.Ticks - displacement < DateTime.MaxValue.Ticks )
			{
				return new Interval<DateTime>(
					new DateTime( from.Start.Ticks + displacement ),
					new DateTime( from.End.Ticks + displacement ) );
			}

			return from;
		}
	}
}