namespace ShareX.ImageEditor.Presentation.Views.Dialogs
{
    /// <summary>
    /// Interface for effect dialog controls that provide Apply, Preview, and Cancel functionality.
    /// This replaces dynamic type usage which can fail in cross-thread hosting scenarios.
    /// </summary>
    public interface IEffectDialog
    {
        event EventHandler<EffectEventArgs>? ApplyRequested;
        event EventHandler<EffectEventArgs>? PreviewRequested;
        event EventHandler? CancelRequested;
    }
}
