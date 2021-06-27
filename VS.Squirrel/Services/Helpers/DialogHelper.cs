namespace AutoSquirrel
{
    using System;
    using System.Threading.Tasks;
    using Caliburn.Micro;

    /// <summary>
    /// Dialog Helper
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// Shows the window.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="param">The parameter.</param>
        public static async Task ShowWindowAsync<T>(params object[] param) where T : class
        {
            var windowManager = new WindowManager();
            var viewModel = Activator.CreateInstance(typeof(T), param) as T;
            await windowManager.ShowWindowAsync(viewModel);
        }
    }
}
