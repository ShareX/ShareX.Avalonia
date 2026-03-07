#region License Information (GPL v3)

/*
    ShareX.ImageEditor - The UI-agnostic Editor library for ShareX
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.ImageEditor.Core.Annotations;
using ShareX.ImageEditor.Core.Editor;
using SkiaSharp;

namespace ShareX.ImageEditor.Core.History;

/// <summary>
/// Manages undo/redo history for the editor using the Memento pattern.
/// Adapted from ShareX's ImageEditorHistory implementation.
/// </summary>
internal class EditorHistory : IDisposable
{
    public bool CanUndo => _undoMementoStack.Count > 0;
    public bool CanRedo => _redoMementoStack.Count > 0;

    /// <summary>
    /// Maximum number of canvas mementos (destructive operations) to keep.
    /// ISSUE-003 mitigation: Canvas mementos contain full bitmap copies and can consume
    /// significant memory (e.g., 8MB per 4K screenshot). Limiting to 5 balances undo depth with memory.
    /// </summary>
    private const int MaxCanvasMementos = 5;

    /// <summary>
    /// Maximum number of annotation-only mementos to keep (lightweight operations).
    /// These don't store canvas bitmaps, so we can keep more.
    /// Default increased to 50 to prevent premature history loss causing perceived bugs.
    /// </summary>
    private const int MaxAnnotationMementos = 50;

    private readonly EditorCore _editorCore;
    private readonly Stack<EditorMemento> _undoMementoStack = new();
    private readonly Stack<EditorMemento> _redoMementoStack = new();

    public EditorHistory(EditorCore editorCore)
    {
        _editorCore = editorCore;
    }

    /// <summary>
    /// Add a memento to the undo stack and clear redo stack
    /// </summary>
    private void AddMemento(EditorMemento memento)
    {
        // Push the new memento first
        _undoMementoStack.Push(memento);

        // Flatten stack to apply limits linearly from Newest to Oldest
        EditorMemento[] allMementos = _undoMementoStack.ToArray();
        _undoMementoStack.Clear(); // Clear and rebuild

        var keptItems = new List<EditorMemento>();
        int keptCanvasCount = 0;

        for (int i = 0; i < allMementos.Length; i++)
        {
            var m = allMementos[i];
            bool keep = false;

            // Check Total Limit
            if (keptItems.Count < MaxAnnotationMementos)
            {
                if (m.Canvas != null)
                {
                    // Check Canvas Limit
                    if (keptCanvasCount < MaxCanvasMementos)
                    {
                        keep = true;
                        keptCanvasCount++;
                    }
                }
                else
                {
                    // Annotation memento - keep if within total limit
                    keep = true;
                }
            }

            if (keep)
            {
                keptItems.Add(m);
            }
            else
            {
                // Discard this item and ALL older items (since history is linear)
                m.Dispose();

                // Dispose the rest of the array
                for (int j = i + 1; j < allMementos.Length; j++)
                {
                    allMementos[j].Dispose();
                }
                break; // Stop processing
            }
        }

        // Rebuild stack (Reverse of keptItems to restore Oldest -> Newest)
        // keptItems[0] is Newest (Top). 
        // Stack.Push pushes to Top. 
        // So we push Oldest first.
        for (int i = keptItems.Count - 1; i >= 0; i--)
        {
            _undoMementoStack.Push(keptItems[i]);
        }

        // Clear redo stack when new action is performed
        foreach (EditorMemento redoMemento in _redoMementoStack)
        {
            redoMemento?.Dispose();
        }

        _redoMementoStack.Clear();
    }

    /// <summary>
    /// Create a memento with full canvas bitmap (for destructive operations like crop/cutout)
    /// ISSUE-010 fix: Captures selected annotation ID for restoration
    /// </summary>
    private EditorMemento GetMementoFromCanvas()
    {
        List<Annotation> annotations = _editorCore.GetAnnotationsSnapshot();
        SKBitmap? canvas = _editorCore.SourceImage?.Copy();
        Guid? selectedId = _editorCore.SelectedAnnotation?.Id;
        return new EditorMemento(annotations, _editorCore.CanvasSize, canvas, selectedId);
    }

    /// <summary>
    /// Create a memento with only annotations (for non-destructive annotation operations)
    /// ISSUE-010 fix: Captures selected annotation ID for restoration
    /// </summary>
    private EditorMemento GetMementoFromAnnotations(Annotation? excludeAnnotation = null)
    {
        List<Annotation> annotations = _editorCore.GetAnnotationsSnapshot(excludeAnnotation);
        Guid? selectedId = _editorCore.SelectedAnnotation?.Id;
        return new EditorMemento(annotations, _editorCore.CanvasSize, null, selectedId);
    }

    /// <summary>
    /// Create a canvas memento before destructive operations (crop, cutout)
    /// </summary>
    public void CreateCanvasMemento()
    {
        EditorMemento memento = GetMementoFromCanvas();
        AddMemento(memento);
    }

    /// <summary>
    /// Create an annotations-only memento for non-destructive operations.
    /// Excludes region tools (crop, cutout, spotlight) from memento creation.
    /// </summary>
    /// <param name="excludeAnnotation">Optional annotation to exclude from the memento (to capture state before it was added)</param>
    /// <param name="force">Force memento creation regardless of active tool</param>
    public void CreateAnnotationsMemento(Annotation? excludeAnnotation = null, bool force = false)
    {
        // Skip memento creation for region tools (crop, cutout, spotlight)
        // These tools execute immediately and don't need annotation history
        if (!force &&
            (_editorCore.ActiveTool == EditorTool.Crop ||
             _editorCore.ActiveTool == EditorTool.Spotlight))
        {
            return;
        }

        EditorMemento memento = GetMementoFromAnnotations(excludeAnnotation);
        AddMemento(memento);
    }

    /// <summary>
    /// Undo the last operation
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;

        EditorMemento undoMemento = _undoMementoStack.Pop();

        if (undoMemento.Annotations != null)
        {
            if (undoMemento.Canvas == null)
            {
                // Annotations-only undo: save current annotations to redo stack
                EditorMemento redoMemento = GetMementoFromAnnotations();
                _redoMementoStack.Push(redoMemento);

                _editorCore.RestoreState(undoMemento);
            }
            else
            {
                // Canvas undo: save current full state to redo stack
                EditorMemento redoMemento = GetMementoFromCanvas();
                _redoMementoStack.Push(redoMemento);

                _editorCore.RestoreState(undoMemento);
            }
        }
    }

    /// <summary>
    /// Redo the last undone operation
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;

        EditorMemento redoMemento = _redoMementoStack.Pop();

        if (redoMemento.Annotations != null)
        {
            if (redoMemento.Canvas == null)
            {
                // Annotations-only redo: save current annotations to undo stack
                EditorMemento undoMemento = GetMementoFromAnnotations();
                _undoMementoStack.Push(undoMemento);

                _editorCore.RestoreState(redoMemento);
            }
            else
            {
                // Canvas redo: save current full state to undo stack
                EditorMemento undoMemento = GetMementoFromCanvas();
                _undoMementoStack.Push(undoMemento);

                _editorCore.RestoreState(redoMemento);
            }
        }
    }

    /// <summary>
    /// Clear all history and dispose resources
    /// </summary>
    public void Dispose()
    {
        foreach (EditorMemento undoMemento in _undoMementoStack)
        {
            undoMemento?.Dispose();
        }

        _undoMementoStack.Clear();

        foreach (EditorMemento redoMemento in _redoMementoStack)
        {
            redoMemento?.Dispose();
        }

        _redoMementoStack.Clear();
    }
}
