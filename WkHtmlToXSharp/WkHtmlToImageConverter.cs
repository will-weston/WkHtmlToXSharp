using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;

using SysConvert = System.Convert;

namespace WkHtmlToXSharp
{
	/// <summary>
	/// Plain wrapper around wkhtmltox API library.
	/// </summary>
	/// <remarks>
	/// WARNING: Due to underlaying's API restrictions all calls to
	/// instances of this class should be made from within the same thread!!
	/// See MultiplexingConverter for an interim & transparent solution.
	/// </remarks>
    public sealed class WkHtmlToImageConverter : IHtmlToImageConverter
	{
		#region private fields

        private ImageGlobalSettings _globalSettings = new ImageGlobalSettings();
		private StringBuilder _errorString = null;
		private int _currentPhase = 0;
		private bool _disposed = false;
		#endregion

		#region Properties
        public ImageGlobalSettings GlobalSettings { get { return _globalSettings; } }
		#endregion

		#region Events
		public event EventHandler<EventArgs<int>> Begin = delegate { };
		public event EventHandler<EventArgs<int, string>> PhaseChanged = delegate { };
		public event EventHandler<EventArgs<int, string>> ProgressChanged = delegate { };
		public event EventHandler<EventArgs<bool>> Finished = delegate { };
		public event EventHandler<EventArgs<string>> Error = delegate { };
		public event EventHandler<EventArgs<string>> Warning = delegate { };
		#endregion

		#region .ctors
        public WkHtmlToImageConverter(bool useX11 = false)
		{			
			// Try to deploy native libraries bundles.
			WkHtmlToXLibrariesManager.InitializeNativeLibrary();

			var version = NativeCalls.wkhtmltoimage_version();

			if (NativeCalls.wkhtmltoimage_init(useX11 ? 1 : 0) == 0)
				throw new InvalidOperationException(string.Format("wkhtmltoimage_init failed! (version: {0}, useX11 = {1})", version, useX11));

			Logger.DebugFormat("Initialized new converter instance (Version: {0}, UseX11 = {1})", version, useX11);
		}
		#endregion

		#region Global settings code..
		private string GetStringValue(object value)
		{
			var tmp = value is string ? value as string : SysConvert.ToString(value, CultureInfo.InvariantCulture);
			// Correct for differences between C booleans and C# booleans
			tmp = tmp == "True" ? "true" : tmp;
			tmp = tmp == "False" ? "false" : tmp;
			return tmp;
		}

		private IDictionary<string, object> GetProperties(string prefix, object instance)
		{
			var dict = new Dictionary<string, object>();
			var type = instance.GetType();
			var properties = type.GetProperties();

			foreach (var property in properties)
			{
				var ptype = property.PropertyType;
				var name = property.Name;
				var obj = property.GetValue(instance, null);
				
				// Fix camel casing as used by wkhtmltopdf property names.
				name = Char.ToLower(name[0]) + name.Substring(1);

				// Prepend prefix (if any).
				name = prefix + name;

				if (ptype.IsValueType || ptype == typeof(string))
				{
                    dict.Add(name, obj); 
				}
				else
				{
					foreach (var item in GetProperties(name + ".", obj))
						dict.Add(item.Key, item.Value);
				}
			}

			return dict;
		}

		#region GlobalSettings
		private void _SetGlobalSetting(ImageGlobalSettingHandle settings, string name, object value)
		{
			var tmp = GetStringValue(value);

			if (NativeCalls.wkhtmltoimage_set_global_setting(settings, name, tmp) == 0)
			{
				var msg = string.Format("Set GlobalSetting '{0}' as '{1}': operation failed!", name, tmp);
				throw new ApplicationException(msg);
			}
		}

        private ImageGlobalSettingHandle _BuildGlobalSettings()
		{
			var ptr = NativeCalls.wkhtmltoimage_create_global_settings();

			foreach (var item in GetProperties(null, GlobalSettings))
				_SetGlobalSetting(ptr, item.Key, item.Value);

			return ptr;
		}
		#endregion

		#endregion
		
		#region Event dispatchers.
		private void OnBegin(int expectedPhases)
		{
			try
			{
				Begin(this, new EventArgs<int>(expectedPhases));
			}
			catch (Exception ex)
			{
				Logger.Error("Begin event handler failed.", ex);
			}
		}
        private void OnError(ImageConverterHandle ptr, string error)
		{
			//var error = Marshaler.GetInstance(null).MarshalNativeToManaged(errorPtr) as string;
			_errorString.AppendFormat("{0}{1}", error, Environment.NewLine);

			try
			{
				Error(this, new EventArgs<string>(error));
			}
			catch (Exception ex)
			{
				Logger.Error("Error event handler failed.", ex);
			}
		}
        private void OnWarning(ImageConverterHandle ptr, string warn)
		{
			try
			{
				//var warn = Marshaler.Instance.MarshalNativeToManaged(warnPtr) as string;
				Warning(this, new EventArgs<string>(warn));
			}
			catch (Exception ex)
			{
				Logger.Error("Warning event handler failed.", ex);
			}
		}
		private void OnPhaseChanged(ImageConverterHandle converter)
		{
			var tmp = NativeCalls.wkhtmltoimage_phase_description(converter, _currentPhase);
			var str = Marshal.PtrToStringAnsi(tmp);

			try
			{
				PhaseChanged(this, new EventArgs<int, string>(++_currentPhase, str));
			}
			catch (Exception ex)
			{
				Logger.Error("PhaseChanged event handler failed.", ex);
			}
		}
        private void OnProgressChanged(ImageConverterHandle converter, int progress)
		{
			var tmp = NativeCalls.wkhtmltoimage_progress_string(converter);
			var str = Marshaler.GetInstance(null).MarshalNativeToManaged(tmp) as string;

			try
			{
				ProgressChanged(this, new EventArgs<int, string>(progress, str));
			}
			catch (Exception ex)
			{
				Logger.Error("ProgressChanged event handler failed.", ex);
			}
		}
        private void OnFinished(ImageConverterHandle converter, bool success)
		{
			try
			{
				Finished(this, new EventArgs<bool>(success));
			}
			catch (Exception ex)
			{
				Logger.Error("Finished event handler failed.", ex);
			}
		}
		#endregion

		#region Convertion methods
		private ImageConverterHandle _BuildConverter(ImageGlobalSettingHandle globalSettings, string inputHtml)
		{
			var converter = NativeCalls.wkhtmltoimage_create_converter(globalSettings);

			return converter;
		}

		private byte[] _Convert(string inputHtml)
		{
            var converter = ImageConverterHandle.Zero;
			var errorCb = new NativeCalls.wkhtmltoimage_str_callback(OnError);
			var warnCb = new NativeCalls.wkhtmltoimage_str_callback(OnWarning);
            var phaseCb = new NativeCalls.wkhtmltoimage_void_callback(OnPhaseChanged);
            var progressCb = new NativeCalls.wkhtmltoimage_int_callback(OnProgressChanged);
            var finishCb = new NativeCalls.wkhtmltoimage_bool_callback(OnFinished);

			try
			{
				var gSettings = _BuildGlobalSettings();

				converter = _BuildConverter(gSettings, inputHtml);

				_errorString = new StringBuilder();

                NativeCalls.wkhtmltoimage_set_error_callback(converter, errorCb);
                NativeCalls.wkhtmltoimage_set_warning_callback(converter, warnCb);
                NativeCalls.wkhtmltoimage_set_phase_changed_callback(converter, phaseCb);
                NativeCalls.wkhtmltoimage_set_progress_changed_callback(converter, progressCb);
                NativeCalls.wkhtmltoimage_set_finished_callback(converter, finishCb);

                OnBegin(NativeCalls.wkhtmltoimage_phase_count(converter));

                if (NativeCalls.wkhtmltoimage_convert(converter) == 0)
				{
					var msg = string.Format("HtmlToPdf conversion failed: {0}", _errorString.ToString());
					throw new ConverterException(msg);
				}

				if (!string.IsNullOrEmpty(GlobalSettings.Out))
					return null;

				Logger.Debug("CONVERSION DONE.. getting output.");

				// Get output from internal buffer..

				IntPtr tmp = IntPtr.Zero;
                var ret = NativeCalls.wkhtmltoimage_get_output(converter, out tmp);
				var output = new byte[ret.ToInt32()];
				Marshal.Copy(tmp, output, 0, output.Length);

				return output;
			}
			finally
			{
                if (converter.Value != ImageConverterHandle.Zero.Value)
				{
                    NativeCalls.wkhtmltoimage_set_error_callback(converter, null);
                    NativeCalls.wkhtmltoimage_set_warning_callback(converter, null);
                    NativeCalls.wkhtmltoimage_set_phase_changed_callback(converter, null);
                    NativeCalls.wkhtmltoimage_set_progress_changed_callback(converter, null);
                    NativeCalls.wkhtmltoimage_set_finished_callback(converter, null);
                    NativeCalls.wkhtmltoimage_destroy_converter(converter);
                    converter = ImageConverterHandle.Zero;
				}
			}
		}

		public byte[] Convert()
		{
			return _Convert(null);
		}

		public byte[] Convert(string inputHtml)
		{
			if (inputHtml == null)
				throw new ArgumentNullException("inputHtml");

			return _Convert(inputHtml);
		}
		#endregion

		#region IDisposable
		private void Dispose(bool disposing)
		{
			if (_disposed)
			{
				Logger.Warn("Disposed was called more than once?!");
				return;
			}

			if (disposing)
			{
				// Dispose managed resources..
				Begin = null;
				PhaseChanged = null;
				ProgressChanged = null;
				Finished = null;
				Error = null;
				Warning = null;
			}

			// Dispose un-managed resources..
			try {
				NativeCalls.wkhtmltoimage_deinit();
			}
			catch (DllNotFoundException) {
				// We may not be initialized yet
			}

			_disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

        ~WkHtmlToImageConverter()
		{
			Dispose(false);
		}
		#endregion
	}
}
