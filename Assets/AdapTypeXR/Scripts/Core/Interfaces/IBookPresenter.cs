#nullable enable
using System;
using AdapTypeXR.Core.Models;

namespace AdapTypeXR.Core.Interfaces
{
    /// <summary>
    /// Controls the 3D book object in the XR scene.
    /// Responsible for page navigation, text display, and physical
    /// book interaction. Follows the Presenter pattern — bridges
    /// domain events and scene visuals without containing logic.
    /// </summary>
    public interface IBookPresenter
    {
        /// <summary>Raised when the user turns to the next page.</summary>
        event Action<int> PageAdvanced;

        /// <summary>Raised when the user returns to the previous page.</summary>
        event Action<int> PageReturned;

        /// <summary>Raised when the book is opened.</summary>
        event Action BookOpened;

        /// <summary>Raised when the book is closed.</summary>
        event Action BookClosed;

        /// <summary>Zero-based index of the currently displayed page.</summary>
        int CurrentPageIndex { get; }

        /// <summary>Total number of pages loaded in the current passage.</summary>
        int TotalPages { get; }

        /// <summary>
        /// Loads a reading passage and populates the book with its pages.
        /// </summary>
        /// <param name="passage">The text passage to display.</param>
        void LoadPassage(ReadingPassage passage);

        /// <summary>
        /// Applies a typography configuration to all visible text on the current page.
        /// </summary>
        /// <param name="config">The typography settings to apply.</param>
        void ApplyTypography(TypographyConfig config);

        /// <summary>
        /// Navigates to a specific page by index.
        /// </summary>
        /// <param name="pageIndex">Zero-based target page index.</param>
        void GoToPage(int pageIndex);

        /// <summary>
        /// Shows or hides the book in the scene.
        /// </summary>
        void SetVisible(bool visible);
    }
}
