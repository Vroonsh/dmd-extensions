﻿using System;
using System.IO;
using System.Reactive.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibDmd.Common;
using NLog;
using SharpAvi;
using SharpAvi.Codecs;
using SharpAvi.Output;

namespace LibDmd.Output.FileOutput
{
	public class VideoOutput : IRgb24Destination, IBitmapDestination
	{
		public string VideoPath { get; set; }

		public readonly int Width = 128;
		public readonly int Height = 32;
		public readonly uint Fps;
		public string Name { get; } = "Video Writer";
		public bool IsRgb { get; } = true;
		public bool IsAvailable { get; } = true;

		private AviWriter _writer;
		private IAviVideoStream _stream;
		private byte[] _frame;
		private IDisposable _animation;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VideoOutput(string path, uint fps = 30)
		{
			Fps = fps;
			VideoPath = Path.GetFullPath(path);
			if (!Directory.Exists(Path.GetDirectoryName(VideoPath))) {
				throw new InvalidFolderException($"Path \"{Path.GetDirectoryName(VideoPath)}\" is not a folder.");
			}
			Init();
		}

		public void Init()
		{
			_writer = new AviWriter(VideoPath) {
				FramesPerSecond = 30,
				EmitIndex1 = true
			};

			try {
				_stream = _writer.AddMpeg4VideoStream(Width, Height, Fps,
					quality: 100, 
					codec: KnownFourCCs.Codecs.X264, 
					forceSingleThreadedAccess: true
				);
				Logger.Info("X264 encoder found.");

			} catch (InvalidOperationException e) {
				Logger.Warn("Error creating X264 encoded stream: {0}.", e.Message);
			}

			try {
				if (_stream == null) {
					_stream = _writer.AddMotionJpegVideoStream(Width, Height,
						quality: 100
					);
				}
				Logger.Info("MJPEG encoder found.");

			} catch (InvalidOperationException e) {
				Logger.Warn("Error creating MJPEG encoded stream: {0}.", e.Message);
			}

			if (_stream == null) {
				Logger.Error("No encoder available, aborting.");
				return;
			}
			_animation = Observable
				.Interval(TimeSpan.FromTicks(1000 * TimeSpan.TicksPerMillisecond / Fps))
				.Subscribe(_ => {
					if (_frame != null) {
						_stream?.WriteFrame(true, _frame, 0, _frame.Length);
					}
				});
			Logger.Info("Writing video to {0}.", VideoPath);
		}

		/// <summary>
		/// Renders an image to the display.
		/// </summary>
		/// <param name="bmp">Any bitmap</param>
		public void Render(BitmapSource bmp)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			_animation.Dispose();
			_writer.Close();
		}

		public void RenderRgb24(byte[] frame)
		{
			if (frame == null) {
				return;
			}
			if (_frame == null) {
				_frame = new byte[Width * Height * 4];
			}
			ImageUtil.ConvertRgb24ToBgr32(Width, Height, frame, _frame);
		}

		public void SetColor(Color color)
		{
			throw new NotImplementedException();
		}

		public void SetPalette(Color[] colors)
		{
			throw new NotImplementedException();
		}

		public void ClearPalette()
		{
			throw new NotImplementedException();
		}

		public void ClearColor()
		{
			throw new NotImplementedException();
		}
	}

}
