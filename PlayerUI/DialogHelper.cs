using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
//using Editor.ViewModels.Tools;

namespace PlayerUI
{
	public class DialogHelper
	{
		public static void ShowDialog<T>(params Object[] param) where T : class
		{
			var windowManager = new WindowManager();
			T viewModel = Activator.CreateInstance(typeof(T), param) as T;
			
			windowManager.ShowDialog(viewModel);
		}

		public static void ShowDialog<T>(T instance, params Object[] param) where T : class
		{
			var windowManager = new WindowManager();
			windowManager.ShowDialog(instance);
		}

		public static T ShowDialogOut<T>(params Object[] param) where T : class
		{
			var windowManager = new WindowManager();
			T viewModel = Activator.CreateInstance(typeof(T), param) as T;
			windowManager.ShowDialog(viewModel);
			return viewModel;
		}

		public static T ShowDialogOut<T>(T instance, params Object[] param) where T : class
		{
			var windowManager = new WindowManager();			
			windowManager.ShowDialog(instance);
			return instance;
		}

		public static void ShowDialogWithContext<T>(object context, params Object[] param) where T : class
		{
			var windowManager = new WindowManager();
			T viewModel = Activator.CreateInstance(typeof(T), param) as T;

			windowManager.ShowDialog(viewModel, context);
		}

		public static void ShowDialogWithContext<T>(T instance, object context, params Object[] param) where T : class
		{
			var windowManager = new WindowManager();
			windowManager.ShowDialog(instance, context);
		}

		public static void ShowWindow<T>(params Object[] param) where T : class
		{
			var windowManager = new WindowManager();
			T viewModel = Activator.CreateInstance(typeof(T), param) as T;

			windowManager.ShowWindow(viewModel);
		}

		public static void ShowWindow<T>(T instance, params Object[] param) where T : class
		{
			var windowManager = new WindowManager();
			windowManager.ShowWindow(instance);
		}

		public static void ShowWindowWithContext<T>(object context, params Object[] param) where T : class
		{
			var windowManager = new WindowManager();
			T viewModel = Activator.CreateInstance(typeof(T), param) as T;

			windowManager.ShowWindow(viewModel, context);
		}

		public static void ShowWindowWithContext<T>(T instance, object context, params Object[] param) where T : class
		{
			var windowManager = new WindowManager();
			windowManager.ShowWindow(instance, context);
		}

		
	}
}
