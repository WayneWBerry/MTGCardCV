using Emgu.CV;

namespace CardImaging
{
    /// <summary>
    /// Cached Image Structure
    /// </summary>
    internal struct CachedRotatedImage
    {
        internal Mat image;
        internal double angle;
    }
}
