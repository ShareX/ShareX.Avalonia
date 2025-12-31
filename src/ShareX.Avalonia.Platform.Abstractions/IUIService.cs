using System;
using System.Drawing;
using System.Threading.Tasks;

namespace ShareX.Ava.Platform.Abstractions
{
    /// <summary>
    /// Service for interacting with the main UI (e.g. navigation, showing windows)
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// Shows the image editor with the provided image
        /// </summary>
        Task ShowEditorAsync(Image image);
    }
}
