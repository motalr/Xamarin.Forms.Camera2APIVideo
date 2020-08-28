using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace BSV.Control
{
	

	public class CameraView : View
	{
		public static readonly BindableProperty CameraProperty = BindableProperty.Create(
			"Camera",
			typeof(CameraOptions),
			typeof(CameraView),
			CameraOptions.Rear);

		public CameraOptions Camera
		{
			get => (CameraOptions)GetValue(CameraProperty);
			set => SetValue(CameraProperty, value);
		}
	}


}
