using UnityEngine;

namespace DragonBattle.Core
{
    public class DragonIdentity : MonoBehaviour
    {
        [SerializeField] private string displayName = "Dragon";
        [SerializeField] private Color accentColor = Color.white;

        public string DisplayName => displayName;
        public Color AccentColor => accentColor;
    }
}
