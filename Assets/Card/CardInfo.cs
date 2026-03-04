using UnityEngine;

namespace DuelKingdom.Data
{
    public enum CardType
    {
        test,
        test_1,
        test_2,
    }

    [CreateAssetMenu(fileName = "CardInfo", menuName = "DuelKingdom/CardInfo")]
    public class CardInfo : ScriptableObject
    {
        public CardType cardType; // カードの種類
        public int value; // カードの数字
        public Sprite sprite; // 画像
    }
}
