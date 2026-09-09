using UnityEngine;
using SFS.World;

namespace SFSEnhanced.Mod.World
{
    internal sealed class RemoteRocketGhost : MonoBehaviour
    {
        private Rocket _rocket;

        public void Bind(Rocket rocket)
        {
            _rocket = rocket;
            if (_rocket == null) return;
            _rocket.isPlayer.Value = false;
            _rocket.hasControl.Value = false;
            _rocket.rb2d.simulated = false;
            foreach (var collider in _rocket.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        }

        private void LateUpdate()
        {
            if (_rocket == null) return;
            _rocket.isPlayer.Value = false;
            _rocket.hasControl.Value = false;
            if (_rocket.rb2d != null) _rocket.rb2d.simulated = false;
            foreach (var collider in _rocket.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        }
    }
}