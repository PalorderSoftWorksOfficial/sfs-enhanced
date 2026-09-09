using SFS.UI;
using UnityEngine;

namespace SFSEnhanced.Mod.UI
{
    internal sealed class MultiplayerScreen : BasicMenu
    {
        private MultiplayerMenu _controller;

        public void Bind(MultiplayerMenu controller)
        {
            _controller = controller;
            menuHolder = gameObject;
            closeAnyway = true;
        }

        public override void OnOpen()
        {
            menuHolder.SetActive(true);
            _controller?.BuildWindow(transform);
        }

        public override void OnClose()
        {
            _controller?.DestroyWindow();
            if (menuHolder != null) menuHolder.SetActive(false);
            _controller?.OnScreenClosed();
        }
    }
}
