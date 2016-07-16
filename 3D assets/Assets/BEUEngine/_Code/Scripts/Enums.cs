/// <summary>
/// I place most of my enums here for easier access. I like to have them in one
/// spot. There are a few that aren't here that I liked having in their desired
/// script such as ItemTypes inside of Base_Item.
/// </summary>

// The names of the different playable characters.
public enum PlayerCharacters
{
	Ethan = 0, Dude = 1, Ethan_Twin = 2, Dude_Twin = 3
}
// All the names of the different enemy characters.
public enum EnemyCharacters
{
	Robot_Kyle = 0
}
// An easier way to label each level and find out which scene we are in.
public enum CurrentArea
{
	Main_Menu = 0, Canyon_0 = 1, Canyon_1 = 2, Demo_Menu = 3, Demo = 4
}

public enum GameDifficulty
{
	Easy = 0, Normal = 1, Hard = 2
}
// Used for various options.
public enum AmountRating
{
	None = 0, Very_Low = 1, Low = 2, Medium = 3, High = 4, Very_High = 5
}
// All of the different particles. Make sure to place the particles on the
// Manager_Particle's particles list in the order they are listed here.
public enum ParticleTypes
{
	HitSpark_Normal = 0, HitSpark_Guard = 1, Sparkles_Heal = 2,
	HitSpark_Throwable = 3, HitSpark_Weapon = 4, Dust_Land = 5, Dust_Run = 6,
	Dizzy = 7
}
// The different states the AI can go into.
public enum AIStates
{
	Spawn = 0, Wander = 1, Pursue = 2, Retreat = 3, Pursue_Item = 4
}
// The detection radius uses this to determine which character type it
// should find.
public enum SearchForTypes
{
	Player = 0, Enemy = 1
}
// All of the different cutscenes.
public enum CutsceneTypes
{
	None = 0, Run_In = 0, Run_Out = 1, Stage_Clear = 2
}
// Normal just makes the enemy move in the direction they are spawned
// until exiting the SpawnEnd trigger gameObject. Find_Way makes them use
// their NavMeshAgent in which they find a player, and that helps them get into
// the battle area.
public enum EnemySpawnType
{
	Normal = 0, Find_Way = 1
}
// Different colors to change to based on if vulnerable or being healed. You
// could add more for say, elements for example. Turn red when burned.
public enum ColorChangeTypes
{
	Is_Vulnerable = 0, Healed = 1
}
// Flashing types. Color_Change makes the character flash a certain color.
// Renderer_Disable makes the character visible and then not visible repeatedly.
public enum FlashType
{
	None = 0, Color_Change = 0, Renderer_Disable = 1
}
// Different transitions between scenes. I only use the fading one for this example project.
public enum TransitionTypes
{
	Fading = 0, Circle = 1
}