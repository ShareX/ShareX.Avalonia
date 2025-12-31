using System.Threading.Tasks;
using System.Drawing;

namespace ShareX.Ava.Platform.Abstractions
{
    public interface IScreenCaptureService
    {
        /// <summary>
        /// Captures a region of the screen.
        /// </summary>
        /// <returns>System.Drawing.Image if successful, null otherwise.</returns>
        Task<System.Drawing.Image?> CaptureRegionAsync();

        /// <summary>
        /// Captures a specific region of the screen
        /// </summary>
        Task<System.Drawing.Image?> CaptureRectAsync(Rectangle rect);
        
        /// <summary>
        /// Captures the full screen
        /// </summary>
        Task<System.Drawing.Image?> CaptureFullScreenAsync();
        
        /// <summary>
        /// Captures the active window
        /// </summary>
        Task<System.Drawing.Image?> CaptureActiveWindowAsync(IWindowService windowService);
    }
}
