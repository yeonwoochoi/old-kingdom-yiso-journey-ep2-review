# Game Design Roadmap

This document contains the game design specification for Old Kingdom Yiso Journey Episode 2. These systems are planned but not yet fully implemented.

## Table of Contents
- [Game Overview](#game-overview)
- [World Structure](#world-structure)
- [Character Progression](#character-progression)
- [Quest System](#quest-system)
- [Save/Load System](#saveload-system)
- [Economy System](#economy-system)
- [NPC System](#npc-system)
- [Infinite Dojo System](#infinite-dojo-system)
- [Portal/Teleport System](#portalteleport-system)
- [Map/UI System](#mapui-system)
- [Stage Progression](#stage-progression)

---

## Game Overview

**Old Kingdom Yiso Journey Episode 2** is a 2D top-down stage-based story RPG. The game flow follows:

```
Base Camp (중간 맵)
  ↓
Chapter Selection
  ↓
Chapter World (스테이지)
  ├─ Central Town (중심 마을)
  └─ Radial Fields (방사형 필드)
      ├─ Combat Fields
      ├─ Dungeons
      └─ Boss Area
  ↓
Complete Chapter
  ↓
Return to Base Camp
  ↓
Repeat (Next Chapter)
```

Users progress through the story by completing chapters, returning to base camp to enhance equipment and level up between stages.

---

## World Structure

### Base Camp (중간 맵/거점)

The permanent hub space connecting all chapters.

**Role**:
- Equipment enhancement
- Stat upgrades
- Stage selection
- Permanent progress storage

**Features**:
- No enemies (safe zone)
- All functional NPCs located here
- Data preserved across sessions
- Access to Infinite Dojo

**Key NPCs**:
- Blacksmith (equipment enhancement)
- Weapon Shop (buy/sell weapons)
- Armor Shop (buy/sell armor)
- General Store (consumables, teleport scrolls)
- Storage (inventory management)

---

### Chapter (스테이지/Chapter)

Independent instance world where one story episode takes place.

**Structure**:
- **Central Town (중심 마을)**: Quest NPCs, general shop, no monsters
- **Radial Fields (방사형 필드)**: Combat areas branching out from town
  - Regular fields (monster hunting)
  - Dungeons (higher difficulty)
  - Boss area (chapter finale)

**Progression Flow**:
```
1. Receive quests in Central Town
2. Travel to fields and hunt monsters
3. Return to town to report quests
4. Unlock next field/dungeon
5. Defeat final boss
6. Unlock portal to next chapter or base camp
```

**Reset Rules**:
- Chapter progress resets when returning to base camp voluntarily
- Quest progress is lost, but EXP/items/gold are kept
- Allows strategic retreat for power leveling

---

## Character Progression

### Level & Stats System

**Experience Growth**:
- Gain EXP from monster kills and quest completion
- Level up automatically when EXP threshold reached

**Automatic Stat Growth**:
- Stats increase by predetermined values on level up
- No manual stat point allocation
- Stats: HP, Attack, Defense, Speed, Crit Rate, Crit Damage

**Growth Calculation**:
```
Base Stats + (Level * Growth Rate) + Equipment Bonuses
```

**Power Sources**:
1. Base level growth
2. Equipment quality
3. Equipment enhancement

---

### Skill System

**Acquisition Method (Boss Unlock)**:
- Defeat chapter boss to unlock their signature skill
- Example:
  - Chapter 1 Boss → Unlock "Charge" skill
  - Chapter 2 Boss → Unlock "Assassination" skill
  - Chapter 3 Boss → Unlock "Whirlwind" skill

**Usage**:
- Unlocked skills are immediately usable
- Equip skills to hotbar
- Cooldown-based activation

**Future Considerations** (not in current design):
- Skill points for upgrades
- Skill level progression
- Skill customization/modifiers

---

### Equipment Enhancement System

**Location**: Only available at **Blacksmith NPC** in **Base Camp**.

**Materials**: Only requires **Gold** (in-game currency).
- No complex materials (enhancement stones, ores, etc.)
- Reduces farming stress and increases gold value

**Enhancement Structure**:
```
Equipment + Gold → Success → Stat Increase
                → Failure → No penalty (gold consumed)
```

**Success Rate**:
- Decreases as enhancement level increases
- +0 to +5: High success rate (80-90%)
- +6 to +10: Medium success rate (50-70%)
- +11 to +15: Low success rate (20-40%)

**Stat Bonuses**:
- Each enhancement level adds % bonus to base stats
- Example: +1 = +5%, +5 = +25%, +10 = +50%

---

## Quest System

### Quest Types

#### Main Quests (메인 퀘스트)
- Story-driven progression
- Required to unlock next area/boss
- Higher rewards (EXP, gold, items)
- Linear progression

#### Sub Quests (서브 퀘스트)
- Optional side missions
- Additional EXP/gold/items
- Can be completed in any order
- Provide lore and world-building

### Quest Structure

**Quest Data**:
```csharp
public class QuestData {
    public string questID;
    public string questName;
    public string description;
    public QuestType type; // Main, Sub
    public QuestObjective[] objectives;
    public QuestReward reward;
}
```

**Objective Types**:
- Kill: "Defeat X monsters"
- Collect: "Gather X items"
- Explore: "Discover location Y"
- Talk: "Speak with NPC Z"
- Escort: "Protect NPC to destination"

**Quest States**:
- NotStarted
- InProgress
- Completed
- Claimed (rewards received)

### Quest Flow

```
1. Receive quest from NPC in Central Town
2. Quest added to quest log
3. Complete objectives in fields
4. Return to quest NPC
5. Report completion
6. Receive rewards
7. Unlock next quest/area
```

**Quest Tracking**:
- Active quest marker on map
- Objective progress in HUD
- Quest log menu

---

## Save/Load System

Complex save system with different rules based on context.

### Basic Save Rules (Within Chapter)

**Real-time Save**: All data saved continuously while playing in a chapter.

**Saved Data**:
- Character position
- Experience points and level
- Inventory (items, equipment, gold)
- Quest progress (main and sub)
- Map exploration state

**Resume Support**:
- Close game and reopen → Continue from last position
- All quest progress preserved
- All collected items preserved

---

### Reset on Base Camp Return (Strategic Retreat)

When player voluntarily returns to base camp mid-chapter:

**Preserved Data** (100% kept):
- Experience points and level
- Gold
- Items and equipment
- Boss skills unlocked from previous chapters

**Reset Data**:
- Chapter quest progress (main and sub)
- Chapter map exploration
- Chapter spawn position

**Purpose**:
- Allows power leveling when underleveled
- Use Infinite Dojo to farm gold/EXP
- Enhance equipment at blacksmith
- Return to chapter and restart from beginning with better stats

**Warning Prompt**:
```
"Returning to Base Camp will reset all quest progress in this chapter.
Experience, gold, and items will be preserved.
Continue?"
[Yes] [No]
```

---

### Save File Structure

```csharp
public class SaveData {
    // Persistent Data
    public int playerLevel;
    public float currentEXP;
    public int gold;
    public List<ItemData> inventory;
    public List<EquipmentData> equippedItems;
    public List<string> unlockedSkills;
    public List<string> completedChapters;

    // Chapter-Specific Data (reset on retreat)
    public string currentChapter;
    public Vector2 playerPosition;
    public List<QuestProgress> chapterQuests;
    public List<string> exploredAreas;
}
```

---

## Economy System

### Gold Currency

**Sources**:
- Monster kills
- Quest rewards
- Item sales
- Infinite Dojo completion

**Uses**:
- Equipment enhancement (primary gold sink)
- Shop purchases (weapons, armor, consumables)
- Teleport scrolls

**Balance Goal**: Make gold valuable by limiting sources and creating meaningful sinks.

---

### Equipment Enhancement

**Location**: Blacksmith NPC (Base Camp only)

**Cost Structure**:
```
Enhancement Level | Gold Cost
+0 → +1          | 100
+1 → +2          | 200
+2 → +3          | 400
+3 → +4          | 800
+4 → +5          | 1600
... (exponential growth)
```

**No Material Requirements**: Only gold needed.

---

### Shop System

#### Weapon/Armor Shop
**Location**: Base Camp

**Functionality**:
- Buy basic equipment with gold
- Sell unwanted equipment for gold
- Equipment has rarity tiers (Common, Rare, Epic, Legendary)

**Price Calculation**:
```
Buy Price = Base Value * Rarity Multiplier
Sell Price = Buy Price * 0.5 (50% of buy price)
```

---

#### General Store
**Location**: Central Town (each chapter) + Base Camp

**Sells**:
- Town Return Scrolls (teleport to town)
- Health Potions
- Mana Potions
- Buff consumables

**Price**: Fixed prices, no bargaining

---

## NPC System

### NPC Types

#### Functional NPCs (Base Camp)
- **Blacksmith**: Equipment enhancement
- **Weapon Merchant**: Buy/sell weapons
- **Armor Merchant**: Buy/sell armor
- **General Merchant**: Consumables and scrolls
- **Storage Keeper**: Inventory management

#### Quest NPCs (Chapter Towns)
- **Main Quest Giver**: Story progression
- **Sub Quest Givers**: Side missions
- **General Merchant**: Consumables

#### Story NPCs
- **Dialogue-only**: World-building and lore
- **Scripted Events**: Cutscene triggers

---

### NPC Interaction

**Interaction Trigger**:
- Press Interact key when near NPC
- Dialogue window opens

**Dialogue System**:
- Text-based dialogue
- Branching conversations (for quest choices)
- Quest acceptance/completion prompts

**Quest Markers**:
- Yellow "!" - Available quest
- Blue "?" - Optional dialogue
- Yellow "?" - Quest in progress
- Yellow checkmark - Quest ready to complete

---

## Infinite Dojo System

**Location**: Base Camp

Session-based farming content for gold and EXP when chapter progress is blocked.

### Structure

**Entry**:
- Enter from Base Camp portal
- Separate instance map
- Spawns at designated start position

**Session**:
- One special quest/objective assigned on entry
- Complete objective → Session ends → Return to base camp
- Receive rewards upon completion

---

### Mission Types

#### Elimination Mission
**Objective**: Find and defeat the target boss within time limit

**Details**:
- Target spawns in random location on map
- Must search map while fighting regular enemies
- Time limit: 5-10 minutes
- Failure: No rewards, return to base camp

---

#### Survival Mission
**Objective**: Survive waves of enemies for time duration

**Details**:
- Continuous enemy spawns
- Increasing difficulty over time
- Time limit: 3-5 minutes
- Failure: Death → Return with partial rewards

---

#### Collection Mission
**Objective**: Collect specific items within time limit

**Details**:
- Items scattered across map
- Enemies guard item locations
- Time limit: 5-8 minutes
- Failure: Incomplete collection → Partial rewards

---

### Rewards

**Completion Rewards**:
- Large gold payout (2-3x normal field)
- Large EXP payout (2-3x normal field)
- Guaranteed rare item drop

**Drop Rate Bonuses**:
- Monster drops have 2x drop rate
- Higher gold per monster kill
- Rare material drops

**Efficiency**:
- Faster leveling than chapter fields
- Higher gold farming efficiency
- Trade-off: More challenging, time-limited

---

## Portal/Teleport System

### Town Return Scroll (마을 귀환 주문서)

**Function**: Instant teleport to chapter's Central Town

**Acquisition**:
- Purchase from General Store
- Random drop from monsters

**Usage**:
- Use from inventory during field exploration
- Teleports to town immediately
- Consumable (one-time use)

**Purpose**:
- Quick return to town for quest turn-in
- Emergency escape from dangerous areas
- Shortcut for inventory management

---

### Chapter Portals

**Location**: Appear after chapter completion

**Spawn Trigger**:
- Defeat final boss
- Complete main quest line

**Destination Options**:
- Next Chapter
- Base Camp

**Alert System**:
```
If incomplete sub-quests exist:
  "You have X sub-quests remaining. Continue to next chapter?"
  [Continue] [Cancel]
```

---

### Base Camp Return (메뉴 옵션)

**Location**: Menu → "Return to Base Camp"

**Warning Prompt**:
```
"Returning to Base Camp will reset quest progress.
EXP, items, and gold will be preserved.
Continue?"
[Yes] [No]
```

**Purpose**: Strategic retreat for power leveling

---

## Map/UI System

### Minimap (HUD)

**Location**: Screen corner (default: top-right)

**Display**:
- Current map terrain
- Player position (icon)
- Nearby monsters (red dots)
- Nearby NPCs (yellow dots)
- Quest objectives (yellow markers)

**Features**:
- Rotates with camera (optional)
- Zoom in/out
- Toggle visibility

---

### World Map (Menu)

**Access**: Menu → "Map"

**Display**:
- Full chapter structure (radial layout)
- Central Town (hub)
- Connected fields (nodes)
- Current position (highlighted)
- Quest locations (markers)

**Features**:
- Fog of war (unexplored areas hidden)
- Fast travel to discovered locations (future feature)
- Area names and level recommendations

**Map Legend**:
- Red marker: Boss area
- Yellow marker: Quest objective
- Blue marker: Discovery point
- Green marker: Town/safe zone

---

## Stage Progression

### Chapter Entry (Power Level Check)

When entering a chapter from Base Camp:

**Power Level Check**:
```
Calculate player combat power:
  Combat Power = (Attack + Defense) * Level + Equipment Score

If Combat Power < Recommended Power:
  Show warning popup
```

**Warning Popup**:
```
"Your current equipment may not be sufficient for this chapter.
Recommended Combat Power: XXXX
Your Combat Power: XXXX
Enter anyway?"
[Enter] [Cancel]
```

**Purpose**: Inform player of difficulty, but allow challenge attempts

---

### Within Chapter

**Normal Flow**:
```
1. Complete quests in Central Town
2. Travel to fields via connected paths
3. Use Town Return Scrolls for quick return
4. Progress through field unlocks
5. Defeat chapter boss
6. Portal appears
```

**Strategic Retreat**:
```
If struggling:
  Menu → "Return to Base Camp"
    → Warning prompt
    → Confirm
    → Return with EXP/items/gold
    → Farm Infinite Dojo
    → Enhance equipment
    → Return to chapter (reset)
```

---

### Chapter Completion

**Victory Conditions**:
- Defeat final boss
- Complete main quest line

**Completion Sequence**:
```
1. Boss dies
2. Victory screen (rewards)
3. Boss skill unlocked
4. Portal spawns in Central Town
5. If sub-quests remaining:
     Show alert → "X sub-quests incomplete. Continue?"
6. Enter portal:
     → Base Camp (save progress, rest)
     → Next Chapter (continue story)
```

**Completion Rewards**:
- Large gold payout
- Large EXP payout
- Guaranteed rare equipment
- Boss skill unlock

**Progress Save**:
- Chapter marked as completed
- Next chapter unlocked
- Boss skill permanently unlocked
- All chapter loot preserved

---

## Implementation Priorities

### Phase 1: Core Systems (Current)
- ✅ Character System
- ✅ FSM System
- ✅ Combat System
- ✅ Movement System

### Phase 2: Gameplay Loop (Next)
- Quest System
- NPC System
- Save/Load System
- Portal/Teleport System

### Phase 3: Progression Systems
- Level/EXP System
- Equipment System
- Enhancement System
- Economy System

### Phase 4: Content Systems
- Infinite Dojo
- Map/UI System
- Boss Skill System
- Chapter Progression

### Phase 5: Polish
- Tutorial System
- Settings Menu
- Sound/Music
- Visual Effects
- Localization

---

## Technical Considerations

### Quest System Architecture

**Recommended Pattern**:
- ScriptableObject for quest definitions
- QuestManager singleton for runtime state
- Event-driven quest progress tracking

**Example**:
```csharp
public class QuestSO : ScriptableObject {
    public string questID;
    public QuestType type;
    public QuestObjective[] objectives;
}

public class QuestManager : MonoBehaviour {
    private Dictionary<string, QuestProgress> activeQuests;

    public void StartQuest(QuestSO quest);
    public void UpdateObjective(string questID, int objectiveIndex);
    public void CompleteQuest(string questID);
}
```

---

### Save System Architecture

**Recommended Pattern**:
- JSON serialization for save files
- Async save/load to prevent frame drops
- Cloud save support (future)

**Example**:
```csharp
public class SaveManager : MonoBehaviour {
    public async Task SaveGame(SaveData data);
    public async Task<SaveData> LoadGame();
    public void DeleteSave();
}
```

---

### Portal System Architecture

**Recommended Pattern**:
- SceneManager for chapter loading
- Persistent singleton for cross-scene data
- Loading screen with async scene loading

**Example**:
```csharp
public class PortalManager : MonoBehaviour {
    public async Task LoadChapter(string chapterID);
    public async Task ReturnToBaseCamp();
    public void ShowLoadingScreen();
}
```
