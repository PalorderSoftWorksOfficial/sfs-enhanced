using SFS.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            pauseWhileOpen = SceneManager.GetActiveScene().name != "World_PC";
        }

        public override void OnOpen()
        {
            menuHolder.SetActive(true);
            _controller?.BuildWindow(transform);
            if (SFS.Input.ScreenManager.main != null)
                Debug.Log($"[SFSEnhanced] multiplayer screen opened (stack {SFS.Input.ScreenManager.main.GetStackCount()}).");
        }

        public override void Close()
        {
            if (_controller != null && _controller.Back()) return;
            base.Close();
        }

        public override void OnClose()
        {
            _controller?.DestroyWindow();
            if (menuHolder != null) menuHolder.SetActive(false);
            if (SFS.Input.ScreenManager.main != null && SFS.Input.ScreenManager.main.GetStackCount() <= 1)
                _controller?.OnScreenClosed();
        }
    }
}
