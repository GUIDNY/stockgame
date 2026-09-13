# World Bible

The closed vocabulary. The AI may reference **only** these identifiers; validators reject or auto-correct anything else.
Source of truth: `Assets/Scripts/World/WorldBible.cs`.

## Locations
`TOWN_SQUARE, TAVERN, GUARD_STATION, MARKET, RESIDENTIAL, WAREHOUSE, FOREST, RUINS, UNDERGROUND, FACTION_BASE`

Each location has a trigger zone, a search spot (evidence can be planted there), and NPC anchors. Search spots at
`TAVERN, GUARD_STATION, MARKET, RESIDENTIAL, FACTION_BASE` are private: taking things there is theft if witnessed.

## NPC slots (fixed roles, generated identities)
| Slot | Role | Workplace | Home |
|---|---|---|---|
| NPC_01 | MAYOR | TOWN_SQUARE | RESIDENTIAL |
| NPC_02 | GUARD_COMMANDER | GUARD_STATION | GUARD_STATION |
| NPC_03 | TAVERN_OWNER | TAVERN | TAVERN |
| NPC_04 | MERCHANT | MARKET | RESIDENTIAL |
| NPC_05 | DOCTOR | RESIDENTIAL | RESIDENTIAL |
| NPC_06 | CRIMINAL | UNDERGROUND | UNDERGROUND |
| NPC_07 | SMUGGLER | WAREHOUSE | WAREHOUSE |
| NPC_08 | PRIEST | RUINS | RUINS |
| NPC_09 | STRANGER | TAVERN | FOREST |
| NPC_10 | FACTION_LEADER | FACTION_BASE | FACTION_BASE |

Name, personality, speech style, faction, goal, secret, mood, relationships and knowledge are generated per game.
Default daily schedules per role live in `WorldBible.RoleSchedules`; director events can override them.

## Factions
`TOWN_GUARD, IRON_HAND, MERCHANT_CIRCLE` (+ `NONE`). Stances: `LAWFUL, CRIMINAL, NEUTRAL`. Alliances/hostilities are
generated per game (four patterns in the offline generator).

## Items
`LETTER, EVIDENCE, KEY, MEDICINE, WEAPON, COIN_PURSE, CONTRABAND, LEDGER`

## Enemies
`THUG`

## Quest actions (reusable building blocks)
`TALK, INVESTIGATE, STEAL, DELIVER, FOLLOW, FIGHT, ESCAPE, PROTECT, SEARCH, BRIBE, THREATEN`

How each completes (`QuestManager.Matches`): TALK/BRIBE/THREATEN = that intent with the target NPC; SEARCH = searching
the location; INVESTIGATE = evidence found there (or talking to the target); STEAL = taking an item there; DELIVER =
giving the item to the NPC; FOLLOW/ESCAPE = entering the location; FIGHT/PROTECT = killing an enemy there.
Objectives may carry a time window (`available_from_hour`, `available_until_hour`).

## Director event types
`FACTION_RETALIATION, RUMOR_SPREAD, NPC_RELOCATE, NPC_DEATH, ITEM_PLANTED, QUEST_OFFER, AMBUSH, ARREST_ATTEMPT,
REVELATION, PRICE_CHANGE, QUIET`

## World change types (the only ways the AI can touch state)
`REPUTATION, NPC_RELATIONSHIP, NPC_MOOD, NPC_LOCATION, NPC_KNOWLEDGE, NPC_GOAL, SPAWN_ENEMIES, SPAWN_ITEM, TENSION,
RUMOR, LOCK_LOCATION, UNLOCK_LOCATION, PRICE_MODIFIER, NPC_DEATH, NPC_HOSTILE`

## Dialogue intents
`GREET, ASK_TOPIC, ASK_RUMORS, THREATEN, BRIBE, ACCUSE, GIVE_ITEM, OFFER_HELP, LIE, LEAVE, FREE_TEXT`

## Moods
`CALM, NERVOUS, ANGRY, AFRAID, FRIENDLY, SUSPICIOUS, GRIEVING, HOPEFUL`

## Relationship types
`ALLY, RIVAL, LOVER, DEBTOR, BLACKMAIL, FAMILY, ENEMY, EMPLOYER`

## Mechanics the AI may assume exist
`WALK, RUN, INTERACT, MELEE_ATTACK, TAKE_DAMAGE, PICK_UP_ITEM, PAY_COINS, TALK, SEARCH_SPOT, WAIT_TIME, SLEEP`

## Interactive objects
`SEARCH_SPOT, DOOR (not implemented as a mechanic yet), NOTICE_BOARD, BED`
