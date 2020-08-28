using Android;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using BSV.Control;
using BSV.Droid.Camera2;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Path = System.IO.Path;

[assembly: ExportRenderer(typeof(CameraView), typeof(CameraViewServiceRenderer))]
namespace BSV.Droid.Camera2
{
    public class CameraViewServiceRenderer : ViewRenderer<CameraView, CameraDroid>
	{
		private CameraDroid _camera;
		private readonly Context _context;

		public CameraViewServiceRenderer(Context context) : base(context)
		{
			_context = context;
		}



		protected override void OnElementChanged(ElementChangedEventArgs<CameraView> e)
		{
			base.OnElementChanged(e);

			var permissions = CameraPermissions();
			_camera = new CameraDroid(Context);

			CameraOptions CameraOption = e.NewElement?.Camera ?? CameraOptions.Rear;
			if (Control == null)
			{
				if (permissions)
				{
					_camera.OpenCamera(CameraOption);

					SetNativeControl(_camera);
				}
				else
				{
					MainActivity.CameraPermissionGranted += (sender, args) =>
					{
						_camera.OpenCamera(CameraOption);

						SetNativeControl(_camera);
					};
				}
			}

			if (e.NewElement != null && _camera != null)
			{
				_camera.Photo += OnPhoto;
			}
		}

		private async void OnPhoto(object sender, ImageSource imgSource)
		{
			var imageData = await RotateImageToPortrait(imgSource);

			//Device.BeginInvokeOnMainThread(() =>
			//{
			//	CameraPage.OnPhotoCaptured(imageData);
			//});
		}

		protected override void Dispose(bool disposing)
		{
			_camera.Photo -= OnPhoto;

			base.Dispose(disposing);
		}

		private bool CameraPermissions()
		{
			const string permission = Manifest.Permission.Camera;
			

			if ((int)Build.VERSION.SdkInt < 23 || ContextCompat.CheckSelfPermission(Android.App.Application.Context, permission) == Permission.Granted)
			{
				return true;
			}

			ActivityCompat.RequestPermissions((MainActivity)_context, MainActivity.CameraPermissions, MainActivity.CameraPermissionsCode);

			return false;
		}

		// ReSharper disable once UnusedMember.Local
		private async Task<ImageSource> RotateImageToPortrait(ImageSource imgSource)
		{
			byte[] imageBytes;
			var imagesourceHandler = new StreamImagesourceHandler();
			var photoTask = imagesourceHandler.LoadImageAsync(imgSource, _context);

			var photo = await photoTask;

			var matrix = new Matrix();

			matrix.PreRotate(90);
			photo = Bitmap.CreateBitmap(photo, 0, 0, photo.Width, photo.Height, matrix, false);
			matrix.Dispose();

			//version
    //        using (var imageStream = new MemoryStream())
    //        {
				//photo.Compress(Bitmap.CompressFormat.Jpeg, 50, imageStream);
				//imageStream.Seek(0L, SeekOrigin.Begin);
				//imageBytes = imageStream.ToArray();
    //        }

            //original
            var stream = new MemoryStream();
            photo.Compress(Bitmap.CompressFormat.Jpeg, 100, stream); 

			stream.Seek(0L, SeekOrigin.Begin);
			imageBytes = stream.ToArray();


   //         var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
   //         string name = $"LTM_{ timestamp }.jpg";

   //         //save to directory
   //         string root = "/storage/emulated/0/DCIM";
   //         //var imagePath = System.IO.Path.Combine(Environment.GetExternalStoragePublicDirectory(Xamarin.Forms.PlatformConfiguration.Android.OS.Environment.DirectoryPictures).ToString(), "MyFolderName");
   //         string folder = Path.Combine(root, "BlackCamera");
   //         if (!System.IO.Directory.Exists(folder.ToString()))
   //         {
   //             Directory.CreateDirectory(folder);
   //         }
   //         var finalPath = Path.Combine(folder, name);
   //         System.IO.File.WriteAllBytes(finalPath, imageBytes);

			//// Use default vibration length
			//Vibration.Vibrate();
			//// Or use specified time
			//var duration = TimeSpan.FromSeconds(.3);
			//Vibration.Vibrate(duration);

			//ExifInterface newExif = new ExifInterface(finalPath);
			//newExif.SetAttribute(ExifInterface.TagDatetime, DateTime.Now.ToString());
			//newExif.SetAttribute(ExifInterface.TagFileSource, finalPath);
			//newExif.SaveAttributes();

			return ImageSource.FromStream(() => stream);
		}
	}
}