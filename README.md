# Rakkle - Rimvore2 (pardon fork)

Inspired by the [Nanakra's rakkle version 1.3](https://github.com/Nanakra/Rakkle-1.3),I created this fork to make the rakkle mod more compatible with the RimVore2 mod. This fork is based on the [unofficial update by Sunny](https://steamcommunity.com/sharedfiles/filedetails/?id=2584844208)

Thanks to steam user Vazrua for finding the fix for hair not showing in bed while injured and resolving the error spam.

This fork adds in that fix and makes a few other changes.

Changes in the fork:

1. Every Rakkle is forced to use `Xenotype_RakkleBase` through the shared pawn kind,
   so wild, faction and colonist Rakkles all get the same genes and appearance.
2. Vore capability is baked into the race's `statBases`:
   grapple chance +1.5, grapple attack +1.7, persuasiveness +1.3,
   digestion strength +1.2 and swallow speed +2.
3. Vore types and goals are built into the race's hediff giver set:
   oral, vaginal and (with Overstuffed) tailmaw types, plus the digest,
   convert-lube and reform goals. No surgery or gene is needed to unlock them.
4. Engineered traits are race-inherent rather than genes: food poisoning immunity,
   predatory compulsion, predation growth and predation dependency are applied to
   every Rakkle at spawn/birthday. The old `RS_Gene_*` gene defs were removed.
5. Predation mechanics:
   - Engulfing tongue: a ranged ability that drags a victim into reach.
     A Rakkle grows it after digesting 10 prey, and upgrades it at 23.
     It cannot be crafted or bought.
   - Constriction charge: each melee hit builds a charge on the attacker;
     when full, the Rakkle automatically starts a RimVore-2 grapple.
   - Venomous fangs: roughly a third of a Rakkle's melee attacks instead of a
     punch or tail swipe. Each bite injects anesthetic venom (0.6 severity for a
     body size 1 victim), so a couple of bites leave prey woozy or sedated.
   - Anesthetic needler: a craftable dart weapon that puts prey to sleep.
   - Predation dependency: a Rakkle that goes 10 days without digesting
     someone weakens in stages, with its own mood penalties. The hunger is eased
     as soon as the digestion stage finishes, by 0.5 per point of prey body size.
     At maximum hunger the Rakkle snaps into a forced mental break: 75% chance it
     hunts animals to digest, 25% chance it fatally vores a colonist.
6. A Rakkle never rolls negative vore-capability quirks - their toolkit is engineered in.
7. The Cradle slaver faction starts at -80 relations with every faction, so relations can be
   improved. Only a Rakkle player colony (the `RS_PlayerFaction` of this mod) is their permanent
   enemy, and that hostility can never be improved.
8. The Rakkle culture's default ideoligion includes RimVore-2's "Predation" (`RV2_Predation`)
   meme when Ideology is active.
9. Rakkle body size increased to 2. It was 1.
Honestly, these are lamia which are larger than humans.
Why should they have the same body size?

Compatibility:

- Requires Humanoid Alien Races, RimVore-2 and RimVore2-pardon's-Extend.
- Tailmaw vore type requires RimVore-2 (Overstuffed); it is skipped when absent.
- Combat Extended, Apparello 2 and several medieval apparel mods are patched.
- Save note: the `RS_Gene_*` defs were removed when the traits became race-inherent.
  Saves created with the gene version may log missing-def errors on affected pawns.

Only changes were made for the 1.6 version of this mod.

I make no promises that any other version will work as I have not altered or tested them.

Installation instructions.

IMPORTANT!!!

Unsubscribe from the Rakkle mods on the Steam Workshop and make sure you do not have a local copy!

---

# 拉克（RimVore-2）中文说明

本模组灵感来自 [Nanakra 的 Rakkle 1.3](https://github.com/Nanakra/Rakkle-1.3)，
目的是让拉克种族更好地兼容 RimVore-2。本分支基于
[Sunny 的非官方更新](https://steamcommunity.com/sharedfiles/filedetails/?id=2584844208)。

感谢 Steam 用户 Vazrua 找到了“受伤卧床时头发不显示”以及刷屏报错的修复方法。

本分支包含该修复，并额外做了以下改动。

## 分支改动

1. 通过共用的 PawnKind，所有拉克都被强制使用 `Xenotype_RakkleBase`，
   野生、派系与殖民者拉克的基因和外观因此保持一致。
2. 吞噬能力直接写入种族的 `statBases`：
   擒抱几率 +1.5、擒抱攻击 +1.7、说服力 +1.3、消化强度 +1.2、吞咽速度 +2。
3. 吞噬类型与目标内置于种族的 hediff 给予组：
   口腔、阴道，以及（安装 Overstuffed 时）尾喉类型，外加消化、转化润滑液与重塑目标。
   无需手术或基因即可解锁。
4. 工程化特性改为种族内置，而非基因：
   食物中毒免疫、捕食冲动、捕食成长、吞噬依赖会在生成/生日时赋予每一名拉克。
   原有的 `RS_Gene_*` 基因定义已删除。
5. 捕食机制：
   - 吞噬长舌：将受害者拽回身边的远程能力。消化 10 名猎物后长出，
     消化 23 名后升级。无法制造或购买。
   - 缠绕蓄势：每次近战命中都会为攻击者累积蓄势；蓄满后自动发起 RimVore-2 擒抱。
   - 毒牙：拉克约三分之一的近战攻击会用毒牙咬击（而非拳击或尾扫）。每次咬击注入麻醉毒液
     （体型 1 的受害者 +0.6 严重度），几口就能让猎物眩晕甚至昏睡。
   - 麻醉针枪：可制造的飞针武器，使猎物陷入沉睡。
   - 吞噬依赖：10 天未消化生物便会逐级衰弱，并附带专属心情惩罚。
     消化阶段一结束即缓解饥渴，缓解量为每点猎物体型 0.5。
     饥渴达到满值时拉克会强制精神崩溃：75% 去猎食动物，25% 致命吞噬殖民者。
6. 拉克不会随机到负面吞噬能力 quirk——她们的捕食构造是刻在身体里的。
7. 响尾蛇奴隶商派系对所有派系初始关系为 -80，可以改善。只有玩家使用本模组的
   `RS_PlayerFaction`（拉克殖民地）时才是永久敌对，且无法改善。
8. 启用意识形态时，拉克文化的默认意识形态包含 RimVore-2 的“捕食”（`RV2_Predation`）模因。
9. 拉克体型从 1 提升到 2。她们是蛇人，本就比人类更大，不应与人类同体型。

## 兼容性

- 需要 Humanoid Alien Races、RimVore-2 与 RimVore2-pardon's-Extend。
- 尾喉吞噬类型需要 RimVore-2 (Overstuffed)；未安装时会自动跳过。
- 已打补丁兼容 Combat Extended、Apparello 2 以及若干中世纪服装模组。
- 存档提示：特性改为种族内置后 `RS_Gene_*` 定义被删除，
  使用基因版本创建的存档中，受影响的小人可能报缺失定义的错误。

本分支仅针对本模组的 1.6 版本进行改动。

其他版本未作修改与测试，因此不保证可用。

## 安装说明

**重要！**

请先在 Steam 创意工坊取消订阅原版拉克模组，并确认本地没有残留副本！
