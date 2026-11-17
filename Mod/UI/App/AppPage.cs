using UnityEngine;

namespace SFSEnhanced.Mod.UI.App
{
    internal abstract class AppPage
    {
        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string Subtitle { get; }

        public abstract void Build(IAppActions app, Transform host);

        public virtual bool NeedsRebuild(IAppActions app) => false;

        public virtual void Update(IAppActions app) { }
    }
}
