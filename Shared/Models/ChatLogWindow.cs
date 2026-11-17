namespace SFSEnhanced.Shared.Models
{
    public static class ChatLogWindow
    {
        public static int Start(int count, int visible)
        {
            if (visible <= 0) return count;
            return count > visible ? count - visible : 0;
        }

        public static int Slot(int index, int start) => index - start;

        public static bool NeedsRedraw(int renderedStart, int start, int renderedCount, int count, long renderedTailSequence, long tailSequence)
        {
            if (renderedStart != start) return true;
            if (count < renderedCount) return true;
            if (renderedCount < start) return true;
            if (count == renderedCount && renderedCount > 0 && renderedTailSequence != tailSequence) return true;
            return false;
        }
    }
}
