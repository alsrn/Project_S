using System.Collections.Generic;
using System.Linq;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Targeting;
using UnityEditor;

namespace ProjectS.EditorTools
{
    /// <summary>
    /// 카드 데이터 생성 (PrototypeSetup.Run 에서 호출). 출처: GameDesigns/GDD_Main/카드 디자인(동아리).xlsx
    /// 1스테이지 캐릭터(클레어·리안·에더)의 1인 대상 카드 + 1인 대상 공용 카드.
    /// 이미 있는 카드 에셋은 덮어쓰지 않는다.
    /// </summary>
    public static partial class PrototypeSetup
    {
        private enum CardTarget
        {
            Self,           // 대상 선택 없음, 효과는 사용자에게
            Enemy,          // 적 1, 사용자 공격 범위
            Ally,           // 아군 1(자신 포함), 사용자 공격 범위 (PROTOTYPE: 기획에 사거리 없음)
            EnemyUnlimited, // 적 1, 사거리 제한 없음
        }

        private static ApplyStatusEffect Status(EffectTarget target, string label, bool debuff, int rounds, StatModifiers m) =>
            new ApplyStatusEffect { target = target, label = label, isDebuff = debuff, durationRounds = rounds, modifiers = m };

        /// <summary>캐릭터 Id → 평타 외 카드(스킬, 궁극기). 1인 대상만.</summary>
        private static List<CardDefinition> CreateSkillCards(string characterId)
        {
            switch (characterId)
            {
                case "Clare":
                    return new List<CardDefinition>
                    {
                        CreateCard("Clare_Smash", "내려 찍기", 3, "현재 공격력의 x3배의 피해를 주며 적의 방어력을 무시한다",
                            CardTarget.Enemy,
                            new DamageEffect { attackMultiplier = 3f, ignoreDefense = true }),
                        CreateCard("Clare_Slaughter", "살육", 5,
                            "클레어의 공격력이 대폭 상승하며(+70 / 2턴 지속) 적에게 현재 공격력의 x5배의 피해를 준다",
                            CardTarget.Enemy,
                            Status(EffectTarget.Self, "살육", false, 2, new StatModifiers { attackAdd = 70 }),
                            new DamageEffect { attackMultiplier = 5f }),
                    };
                case "Lian":
                    return new List<CardDefinition>
                    {
                        CreateCard("Lian_ShieldUp", "방패 활성화", 2,
                            "자신에게 +20의 방어력을 부여한다. 추가로 현재 체력의 20%만큼 회복한다 (PROTOTYPE: 지속 미기재 → 1턴)",
                            CardTarget.Self,
                            Status(EffectTarget.Self, "방패 활성화", false, 1, new StatModifiers { defenseAdd = 20 }),
                            new HealEffect { target = EffectTarget.Self, percentOfCurrentHp = 0.2f }),
                        CreateCard("Lian_KnightsWill", "기사의 의지", 4,
                            "최대 체력의 80%를 즉시 회복한다. 공격력 및 방어력이 대폭 상승하며(공격력 + 60, 방어력 +30, 지속시간 2턴) " +
                            "자신에게 걸린 모든 디버프 효과를 제거한다. 추가로 이동속도도 증가한다(5x5, 지속시간 1턴)",
                            CardTarget.Self,
                            new HealEffect { target = EffectTarget.Self, percentOfMaxHp = 0.8f },
                            Status(EffectTarget.Self, "기사의 의지", false, 2, new StatModifiers { attackAdd = 60, defenseAdd = 30 }),
                            new RemoveDebuffsEffect { target = EffectTarget.Self },
                            // 기획 '이동 5x5': 리안 기본 4 → 5 (+1)
                            Status(EffectTarget.Self, "기사의 의지(이동)", false, 1, new StatModifiers { moveRangeAdd = 1 })),
                    };
                case "Ader":
                    return new List<CardDefinition>
                    {
                        CreateCard("Ader_PoisonShot", "맹독탄", 3,
                            "적에게 맹독을 발사해 중독 상태로 만든다. 중독 상태의 적은 방어력이 감소하며(현재 방어력의 50%, 지속시간 1턴) " +
                            "받는 피해가 2배가 된다(지속시간 1턴). 방어력 감소를 먼저 계산한다",
                            CardTarget.Enemy,
                            Status(EffectTarget.ChosenTarget, "중독", true, 1,
                                new StatModifiers { defenseMultiplier = 0.5f, damageTakenMultiplier = 2f })),
                        CreateCard("Ader_Serum", "혈청 투여", 4,
                            "아군 캐릭터에게 사용할 수 있다. 공격력과 방어력이 상승하며(공격력 + 40, 방어력 + 25, 지속시간 2턴) " +
                            "가하는 피해가 50% 상승한다(지속시간 2턴). 추가로 체력을 회복시켜준다(+400)",
                            CardTarget.Ally,
                            Status(EffectTarget.ChosenTarget, "혈청", false, 2,
                                new StatModifiers { attackAdd = 40, defenseAdd = 25, damageDealtMultiplier = 1.5f }),
                            new HealEffect { flatAmount = 400 }),
                    };
                default:
                    return new List<CardDefinition>(); // 2스테이지 이후 캐릭터: 미구현
            }
        }

        /// <summary>공용 카드(사용자 없음). 1인 대상만. TODO: sp 회복, 추가 지원(드로우) — 대상 없음 / 포격 — 광역</summary>
        private static List<CardDefinition> CreateCommonCards() => new List<CardDefinition>
        {
            CreateCard("Common_SupportFire", "지원 사격", 2,
                "사거리 제한 없이 적에게 방어력을 무시하는 탄환을 발사한다(고정 피해 20)",
                CardTarget.EnemyUnlimited,
                new DamageEffect { attackMultiplier = 0f, flatDamage = 20, ignoreDefense = true }),
        };

        private static CardDefinition CreateCard(string file, string name, int cost, string description, CardTarget target,
            params CardEffect[] effects) =>
            LoadOrCreate<CardDefinition>($"{CardsFolder}/Card_{file}.asset", so =>
            {
                so.FindProperty("displayName").stringValue = name;
                so.FindProperty("baseCost").intValue = cost;
                so.FindProperty("description").stringValue = description;

                var requirements = so.FindProperty("targetRequirements");
                requirements.arraySize = target == CardTarget.Self ? 0 : 1;
                if (target != CardTarget.Self)
                {
                    var req = requirements.GetArrayElementAtIndex(0);
                    req.FindPropertyRelative("kind").enumValueIndex = (int)TargetKind.Unit;
                    req.FindPropertyRelative("relation").enumValueIndex =
                        (int)(target == CardTarget.Ally ? TargetRelation.Ally : TargetRelation.Enemy);
                    req.FindPropertyRelative("requireAlive").boolValue = true;
                    req.FindPropertyRelative("rangeSource").enumValueIndex =
                        (int)(target == CardTarget.EnemyUnlimited ? RangeSource.Unlimited : RangeSource.ActorAttackRange);
                    req.FindPropertyRelative("fixedRange").intValue = 0;
                    req.FindPropertyRelative("requireLineOfSight").boolValue = false;
                }

                var list = so.FindProperty("effects");
                list.arraySize = effects.Length;
                for (int i = 0; i < effects.Length; i++)
                    list.GetArrayElementAtIndex(i).managedReferenceValue = effects[i];
            });

        /// <summary>목록에 없는 카드만 뒤에 추가한다(기존 순서·수동 추가분 유지). 첫 카드는 평타로 유지(적 AI·밸런스 테스트가 사용).</summary>
        private static void EnsureCards(UnitDefinition unit, IEnumerable<CardDefinition> cards) =>
            EnsureObjectList(unit, "cards", cards);

        private static void EnsureCommonCards(PrototypeRoster roster, IEnumerable<CardDefinition> cards) =>
            EnsureObjectList(roster, "commonCards", cards);

        private static void EnsureObjectList(UnityEngine.Object owner, string property, IEnumerable<UnityEngine.Object> items)
        {
            var so = new SerializedObject(owner);
            var list = so.FindProperty(property);
            var existing = Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue).ToList();
            foreach (var item in items)
            {
                if (item == null || existing.Contains(item)) continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
                existing.Add(item);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
