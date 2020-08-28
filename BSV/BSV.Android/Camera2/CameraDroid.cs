using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using BSV.Control;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Forms;
using Size = Android.Util.Size;
using Java.IO;
using Java.Util.Concurrent;

namespace BSV.Droid.Camera2
{
    public class CameraDroid : FrameLayout, TextureView.ISurfaceTextureListener
	{
		private static readonly SparseIntArray Orientations = new SparseIntArray();

		public event EventHandler<ImageSource> Photo;

		public bool OpeningCamera { private get; set; }

		public CameraDevice CameraDevice;

		private readonly CameraStateListener _mStateListener;
		private CaptureRequest.Builder _previewBuilder;
		private CameraCaptureSession _previewSession;
		private SurfaceTexture _viewSurface;
		private readonly TextureView _cameraTexture;
		private Size _previewSize;
		private readonly Context _context;
		private CameraManager _manager;


		private const string TAG = "Camera2VideoFragment";
		public Semaphore cameraOpenCloseLock = new Semaphore(1);

		public MediaRecorder mediaRecorder;

		public CaptureRequest.Builder builder;
		//private CaptureRequest.Builder previewBuilder;

		private Size previewSize;

		private HandlerThread backgroundThread;
		private Handler backgroundHandler;

		public CameraDroid(Context context) : base(context)
		{
			_context = context;

			var inflater = LayoutInflater.FromContext(context);

			if (inflater == null) return;
			var view = inflater.Inflate(Resource.Layout.CameraLayout, this);

			_cameraTexture = view.FindViewById<TextureView>(Resource.Id.cameraTexture);

            MessagingCenter.Subscribe<System.Object>(this, "start_recording", (sender) =>
            {
                StartRecordingVideo();
            });

            MessagingCenter.Subscribe<System.Object>(this, "stop_recording", (sender) =>
            {
				StopRecordingVideo();
            });

            _cameraTexture.SurfaceTextureListener = this;

			_mStateListener = new CameraStateListener { Camera = this };

			StartBackgroundThread();
		}

		public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
		{
			_viewSurface = surface;

			ConfigureTransform(width, height);
			StartPreview();
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
		{
			return true;
		}

		public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
		{
		}

		public void OnSurfaceTextureUpdated(SurfaceTexture surface)
		{
		}

		public void OpenCamera(CameraOptions options)
		{
			if (_context == null || OpeningCamera)
			{
				return;
			}

			OpeningCamera = true;

			_manager = (CameraManager)_context.GetSystemService(Context.CameraService);

			var cameraId = _manager.GetCameraIdList()[(int)options];

			var characteristics = _manager.GetCameraCharacteristics(cameraId);
			var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);

			//[0] == 640x480
			//[1] == 352x288
			//[2] == 340x220
			//[3] == 176x144
			//[4] == 1028x720
			//[5] == 1280x960
			_previewSize = map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture)))[4];

			_manager.OpenCamera(cameraId, _mStateListener, null);


			//for video
			mediaRecorder = new MediaRecorder();

			SetUpMediaRecorder();

		}


		public void StartPreview()
		{
			if (CameraDevice == null || !_cameraTexture.IsAvailable || _previewSize == null)
				return;


			var texture = _cameraTexture.SurfaceTexture;

			texture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);
			_previewBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.Record);
			var surfaces = new List<Surface>();
			var previewSurface = new Surface(texture);
			surfaces.Add(previewSurface);
			_previewBuilder.AddTarget(previewSurface);


			var recorderSurface = mediaRecorder.Surface;
			surfaces.Add(recorderSurface);
			_previewBuilder.AddTarget(recorderSurface);


			CameraDevice.CreateCaptureSession(new List<Surface>(surfaces),
                new CameraCaptureStateListener
                {
                    OnConfigureFailedAction = session =>
                    {
                    },
                    OnConfiguredAction = session =>
                    {
                        _previewSession = session;
                        UpdatePreview();
                    }
                },
                null);
        }

        private void SetUpMediaRecorder()
		{
			mediaRecorder.SetAudioSource(AudioSource.Mic);
			mediaRecorder.SetVideoSource(VideoSource.Surface);
			mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
			mediaRecorder.SetOutputFile(GetVideoFile());
			mediaRecorder.SetVideoEncodingBitRate(10000000);
			mediaRecorder.SetVideoFrameRate(30);
			mediaRecorder.SetVideoSize(_previewSize.Width, _previewSize.Height);
			mediaRecorder.SetVideoEncoder(VideoEncoder.H264);
			mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
			//rotate 90 deg
			mediaRecorder.SetOrientationHint(90);
			mediaRecorder.Prepare();
		}

		private string GetVideoFile()
		{
			string fileName = DateTime.Now.ToString("yymmddhhmmss") + ".mp4"; //new filename based on date time

			var root = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath; //storage/emulated/0/
			string folder = System.IO.Path.Combine(root, "YourGallery");
			if (!System.IO.Directory.Exists(folder.ToString()))
			{
				Directory.CreateDirectory(folder);
			}
			var finalPath = System.IO.Path.Combine(folder, fileName);

			return finalPath;
		}

		private void ConfigureTransform(int viewWidth, int viewHeight)
		{
			if (_viewSurface == null || _previewSize == null || _context == null) return;

			var windowManager = _context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

			var rotation = windowManager.DefaultDisplay.Rotation;
			var matrix = new Matrix();
			var viewRect = new RectF(0, 0, viewWidth, viewHeight);
			var bufferRect = new RectF(0, 0, _previewSize.Width, _previewSize.Height);

			var centerX = viewRect.CenterX();
			var centerY = viewRect.CenterY();

			if (rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270)
			{
				bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
				matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);

				matrix.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
			}

            matrix.PreRotate(0, centerX, centerY);

			_cameraTexture.SetTransform(matrix);
		}

		private void UpdatePreview()
		{
			if (null == CameraDevice)
				return;
			try
			{
				SetUpCaptureRequestBuilder(_previewBuilder);
				HandlerThread thread = new HandlerThread("CameraPreview");
				thread.Start();
				_previewSession.SetRepeatingRequest(_previewBuilder.Build(), null, backgroundHandler);
			}
			catch (CameraAccessException e)
			{
				e.PrintStackTrace();
			}

		}

		private void SetUpCaptureRequestBuilder(CaptureRequest.Builder builder)
		{
			builder.Set(CaptureRequest.ControlMode, new Java.Lang.Integer((int)ControlMode.Auto));
		}

		private void StartBackgroundThread()
		{
			backgroundThread = new HandlerThread("CameraBackground");
			backgroundThread.Start();
			backgroundHandler = new Handler(backgroundThread.Looper);
		}

		private void StopBackgroundThread()
		{
			backgroundThread.QuitSafely();
			try
			{
				backgroundThread.Join();
				backgroundThread = null;
				backgroundHandler = null;
			}
			catch (InterruptedException e)
			{
				e.PrintStackTrace();
			}
		}

		private void StartRecordingVideo()
		{
			try
			{
				




				//Start recording
				mediaRecorder.Start();
				Toast.MakeText(_context, "Recording Started.", ToastLength.Long).Show();
			}
			catch (IllegalStateException e)
			{
				e.PrintStackTrace();
			}
		}

		public void StopRecordingVideo()
		{
            //Stop recording
            mediaRecorder.Stop();
            mediaRecorder.Reset();
			//StartPreview();

			Toast.MakeText(_context, "Recording Stopped.", ToastLength.Long).Show();

			// Workaround for https://github.com/googlesamples/android-Camera2Video/issues/2
			CloseCamera();
			OpenCamera(CameraOptions.Rear);
		}

		private void CloseCamera()
		{
			try
			{
				cameraOpenCloseLock.Acquire();
				if (null != CameraDevice)
				{
					CameraDevice.Close();
					CameraDevice = null;
				}
				if (null != mediaRecorder)
				{
					mediaRecorder.Release();
					mediaRecorder = null;
				}
			}
			catch (InterruptedException e)
			{
				throw new RuntimeException("Interrupted while trying to lock camera closing.");
			}
			finally
			{
				cameraOpenCloseLock.Release();
			}
		}

	}
}