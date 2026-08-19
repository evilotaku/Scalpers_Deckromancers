using UnityEngine;
using UnityEngine.AddressableAssets;

namespace csbcgf
{
    [CreateAssetMenu(fileName = "CardStaticData", menuName = "Battle Card Game Framework/Card Static Data")]
    public class CardStaticData : ScriptableObject
    {
        public string Name;
        public string Mechanic;
        public string AbilityName;
        [TextArea(3, 5)]
        public string AbilityDesc;
        [TextArea(2, 4)]
        public string Story;
        public string Type; // UNIT, SPELL, HIDDEN
        public string Class;
        public string Race;
        
        public AssetReferenceSprite CardImage;
    }
}