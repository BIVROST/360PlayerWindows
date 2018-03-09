using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace Bivrost.Bivrost360Player.WPF
{
	internal class GridLengthAnimation : AnimationTimeline
	{
		static GridLengthAnimation()
		{
			FromProperty = DependencyProperty.Register("From", typeof(GridLength),
				typeof(GridLengthAnimation));

			ToProperty = DependencyProperty.Register("To", typeof(GridLength),
				typeof(GridLengthAnimation));

			EasingFunctionProperty = DependencyProperty.Register("EasingFunction", typeof(EasingFunctionBase),
				typeof(GridLengthAnimation));
		}

		public override Type TargetPropertyType
		{
			get
			{
				return typeof(GridLength);
			}
		}

		protected override System.Windows.Freezable CreateInstanceCore()
		{
			return new GridLengthAnimation();
		}

		public static readonly DependencyProperty FromProperty;
		public GridLength From
		{
			get
			{
				return (GridLength)GetValue(GridLengthAnimation.FromProperty);
			}
			set
			{
				SetValue(GridLengthAnimation.FromProperty, value);
			}
		}

		public static readonly DependencyProperty ToProperty;
		public GridLength To
		{
			get
			{
				return (GridLength)GetValue(GridLengthAnimation.ToProperty);
			}
			set
			{
				SetValue(GridLengthAnimation.ToProperty, value);
			}
		}

		public static readonly DependencyProperty EasingFunctionProperty;
		public EasingFunctionBase EasingFunction
		{
			get
			{
				return (EasingFunctionBase)GetValue(GridLengthAnimation.EasingFunctionProperty);
			}
			set
			{
				SetValue(GridLengthAnimation.EasingFunctionProperty, value);
			}
		}


		public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
		{
			double fromVal = ((GridLength)GetValue(GridLengthAnimation.FromProperty)).Value;
			double toVal = ((GridLength)GetValue(GridLengthAnimation.ToProperty)).Value;

			bool hasEasing = EasingFunction != null;

			if (fromVal > toVal)
			{
				return new GridLength((1 - (hasEasing? EasingFunction.Ease(animationClock.CurrentProgress.Value) : animationClock.CurrentProgress.Value)) * (fromVal - toVal) + toVal, GridUnitType.Pixel);
			}
			else
				return new GridLength((hasEasing ? EasingFunction.Ease(animationClock.CurrentProgress.Value) : animationClock.CurrentProgress.Value) * (toVal - fromVal) + fromVal, GridUnitType.Pixel);
		}
	}
}
