using UnityEngine;
using System.Collections;
/// <summary>
/// Spawner enemy.
/// Note about the EndBattleSpawner:
/// Make sure that only one spawn point per battle zone has this checked.
/// If you have multiple ones, make sure the one with the highest waveTotal
/// has this checked so that it can end the battle when the last wave ends.
/// </summary>
public class SpawnerEnemy : MonoBehaviour
{
	public GameObject[] enemyPrefabsToSpawn;
	public bool EndBattleSpawner = true; // Check summary note for this.
	public EnemySpawnType mySpawnType;
	public int myAreaNumber; // Which battle zone area is this in?
	public int maxEnemiesToSpawnPerWave = 1; // Limit how many per wave. Useful when there are multiple enemy spawn points to limit each one.
	public int waveTotal = 1;
	int _enemiesSpawned = 0;
	int _waveCurrent = 0;
	int _enemiesToSpawnPerWave = 1;
	float _spawnTimer = 0;
	float _timeToSpawn = 0;
	bool _canSpawn = false;

	void Start ()
	{
		_canSpawn = true;
		_timeToSpawn = Random.Range (2f, 4f);
		Invoke ("SetupWaveAmount", 2);
	}

	void Update ()
	{
		// Need to be in battle and in our area number.
		if(!Manager_BattleZone.instance.InBattle || Manager_BattleZone.instance.currentAreaNumber != myAreaNumber)
			return;

		if(_canSpawn)
		{
			if(_waveCurrent < waveTotal)
			{
				if(_enemiesSpawned < _enemiesToSpawnPerWave)
				{
					_spawnTimer += Time.deltaTime;
					if(_spawnTimer > _timeToSpawn)
					{
						_spawnTimer = 0;
						// A random time for spawning again.
						_timeToSpawn = Random.Range(0.5f, 3f);
						SpawnEnemy();
					}
				}
				else // Enemy spawn amount per wave reached.
				{
					_canSpawn = false;
				}
			}
			else // Wave amount reached. Battle over.
			{
				_canSpawn = false;
				if(EndBattleSpawner)
					// End the battle.
					Manager_BattleZone.instance.BattleChange(false);
				gameObject.SetActive(false); // No longer need this gameObject.
			}
		}
		else
		{
			// When this next condition is true, the wave has ended.
			if(Manager_Game.instance.enemiesAll.Count == 0 &&
			   (_enemiesSpawned == _enemiesToSpawnPerWave))
			{
				_enemiesSpawned = 0;
				_waveCurrent++; // Start next wave.
				SetupWaveAmount();
				_canSpawn = true; // Can spawn again.
			}
		}
	}

	void SetupWaveAmount()
	{
		_enemiesToSpawnPerWave = Manager_Targeting.instance.playerTargets.Count;
		if(_enemiesToSpawnPerWave > maxEnemiesToSpawnPerWave)
		   _enemiesToSpawnPerWave = maxEnemiesToSpawnPerWave;
	}
	// Create an enemy and set them up with our spawn type.
	void SpawnEnemy()
	{
		int enemyChosen = Random.Range(0, enemyPrefabsToSpawn.Length);
		GameObject enemy = Instantiate (enemyPrefabsToSpawn [enemyChosen], transform.position, transform.rotation) as GameObject;
		CharacterAI charAI = enemy.GetComponent<CharacterAI> ();
		charAI.StartCoroutine(charAI.SpawnIn(mySpawnType));
		Manager_Game.instance.enemiesAll.Add (enemy);
		_enemiesSpawned++;
	}
}