using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

namespace Completed
{
    // Player inherits from MovingObject, our base class for objects that can move, Enemy also inherits from this.
    public class PlayerAgent : Agent
    {
        private Player player;
        private int lastAction = 0;
        private bool foodRewards;

        [SerializeField] private GameManager gameManager;

        // Must match constants set by the boardManager
        public int maxEnemies = 8; // No real max. Calculated as a log with base 2. Realistic max is 6, using 8 just in case.
        public int maxFood = 5;
        public int maxInnerWalls = 9;
        // Total observations is the sum of the above variables plus 2. Should be 48 by default. (Vectors count as two observations each)

        void Start()
        {
            player = GetComponent<Player>();

            // Toggle this to enable or disable food related rewards
            foodRewards = false;
        }

        public override void OnEpisodeBegin()
        {
            // TODO: Add any necessary code
        }

        public void HandleAttemptMove()
        {
            // TODO: Change the reward below as appropriate. If you want to add a cost per move, you could change the reward to -1.0f (for example).
            AddReward(-0.01f);
        }

        public void HandleFinishlevel()
        {
            // TODO: Change the reward below as appropriate.
            AddReward(1.0f);
        }

        public void HandleFoundFood()
        {
            // TODO: Change the reward below as appropriate.
            float amount = 0.0f;
            if (foodRewards)
            {
                amount = player.pointsPerFood / 100f;
            }
            AddReward(amount);
        }

        public void HandleFoundSoda()
        {
            // TODO: Change the reward below as appropriate.
            float amount = 0.0f;
            if (foodRewards)
            {
                amount = player.pointsPerSoda / 100f;
            }
            AddReward(amount);
        }
        public void HandleLoseFood(int loss)
        {
            // TODO: Change the reward below as appropriate.
            float amount = 0.0f;
            if (foodRewards)
            {
                amount = -loss / 100f;
            }
            AddReward(amount);
        }

        public void HandleLevelRestart(bool gameOver)
        {
            if (gameOver)
            {
                Debug.Log("Level Reached" + gameManager.level);
                Academy.Instance.StatsRecorder.Add("Level Reached", gameManager.level);
                EndEpisode();
            }

            // NOTE: You might also want to end the episode whenever the player successfully reaches the exit sign. You can achieve this by uncommenting the below:
            /*
            else
            {
                // Probably *is* best to consider episodes finished when the exit is reached
                EndEpisode();
            }
            */
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // Adds player position (1*2)
            Vector2 playerPos = gameManager.gridState.GetPlayerNode().ChangeToVector();
            sensor.AddObservation(playerPos);

            // Adds exit position (1*2)
            Vector2 exitPos = gameManager.gridState.GetExitNode().ChangeToVector();
            sensor.AddObservation(exitPos);

            List<Vector2> foodList = gameManager.gridState.NodesToVector2(gameManager.gridState.GetListOfNodesWithFood());
            foodList.AddRange(gameManager.gridState.NodesToVector2(gameManager.gridState.GetListOfNodesWithSoda()));

            // Adds closest food position
            sensor.AddObservation(GetClosestOfType(foodList, playerPos));

            // Adds food and soda positions (Max 5*2)
            /*
            for (int i = 0; i < maxFood; i++)
            {
                if (i < foodList.Count)
                {
                    sensor.AddObservation(foodList[i]);
                }
                else
                {
                    sensor.AddObservation(Vector2.zero);
                }
            }
            */

            List<Vector2> enemyList = gameManager.gridState.NodesToVector2(gameManager.gridState.GetListOfNodesWithEnemies());

            // Adds closest enemy (1*2)
            sensor.AddObservation(GetClosestOfType(enemyList, playerPos));

            // Adds average distance of enemies (1)
            sensor.AddObservation(GetAverageEnemyDist(enemyList, playerPos));

            // Adds enemy positions (No max; defined by a log with base 2. Realistic max is 6, using 8 incase the ai trains incredibly well)
            /*
            for (int i = 0; i < maxEnemies; i++)
            {
                if (i < enemyList.Count)
                {
                    sensor.AddObservation(enemyList[i]);
                }
                else
                {
                    sensor.AddObservation(Vector2.zero);
                }
            }
            */

            // Adds inner positions (Max 9*2)
            List<Vector2> innerWallsList = gameManager.gridState.NodesToVector2(gameManager.gridState.GetListOfNodesWithInnerWalls());

            for (int i = 0; i < maxInnerWalls; i++)
            {
                if (i < innerWallsList.Count)
                {
                    sensor.AddObservation(innerWallsList[i]);
                }
                else
                {
                    sensor.AddObservation(Vector2.zero);
                }
            }

            base.CollectObservations(sensor);
        }

        private Vector2 GetClosestOfType(List<Vector2> items, Vector2 position)
        {
            if (items.Count < 1)
            {
                return Vector2.zero;
            }
            Vector2 closest = items[0];
            float distance = Vector2.Distance(closest, position);
            float tempDist;
            for (int i = 1; i < items.Count; i++)
            {
                tempDist = Vector2.Distance(items[i], position);
                if (tempDist < distance)
                {
                    distance = tempDist;
                    closest = items[i];
                }
            }
            return closest;
        }

        private float GetAverageEnemyDist(List<Vector2> enemies, Vector2 position)
        {
            if (enemies.Count < 1)
            {
                return 0.0f;
            };
            float distance = 0.0f;
            for (int i = 1; i < enemies.Count; i++)
            {
                distance += Vector2.Distance(enemies[i], position);
            }
            distance /= enemies.Count;
            return distance;
        }

        private bool CanMove()
        {
            return !(player.isMoving || player.levelFinished || player.gameOver || gameManager.doingSetup);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (gameManager.playerMovesSinceEnemyMove == gameManager.playerMovesPerEnemyMove && CanMove() && gameManager.playerMoving == false)
            {
                return;
            }

            gameManager.playerTurn = false;

            lastAction = (int)actions.DiscreteActions[0] + 1; // To allow standing still as an action, remove the +1 and change "Branch 0 size" to 5.

            switch (lastAction)
            {
                case 0:
                    break;
                case 1:
                    gameManager.playerMoving = true;
                    player.AttemptMove(-1, 0);
                    break;
                case 2:
                    gameManager.playerMoving = true;
                    player.AttemptMove(1, 0);
                    break;
                case 3:
                    gameManager.playerMoving = true;
                    player.AttemptMove(0, -1);
                    break;
                case 4:
                    gameManager.playerMoving = true;
                    player.AttemptMove(0, 1);
                    break;
                default:
                    break;
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            GetComponent<DecisionRequester>().DecisionPeriod = 1;
            ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;

            //If it's not the player's turn, exit the function.
            if (!CanMove())
            {
                discreteActionsOut[0] = lastAction;
                return;
            }

            int horizontal = 0;     //Used to store the horizontal move direction.
            int vertical = 0;       //Used to store the vertical move direction.

            //Check if we are running either in the Unity editor or in a standalone build.
#if UNITY_STANDALONE || UNITY_WEBPLAYER

            //Get input from the input manager, round it to an integer and store in horizontal to set x axis move direction
            horizontal = (int)(Input.GetAxisRaw("Horizontal"));

            //Get input from the input manager, round it to an integer and store in vertical to set y axis move direction
            vertical = (int)(Input.GetAxisRaw("Vertical"));

            //Check if moving horizontally, if so set vertical to zero.
            if (horizontal != 0)
            {
                vertical = 0;
            }
            //Check if we are running on iOS, Android, Windows Phone 8 or Unity iPhone
#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
			
			//Check if Input has registered more than zero touches
			if (Input.touchCount > 0)
			{
				//Store the first touch detected.
				Touch myTouch = Input.touches[0];
				
				//Check if the phase of that touch equals Began
				if (myTouch.phase == TouchPhase.Began)
				{
					//If so, set touchOrigin to the position of that touch
					touchOrigin = myTouch.position;
				}
				
				//If the touch phase is not Began, and instead is equal to Ended and the x of touchOrigin is greater or equal to zero:
				else if (myTouch.phase == TouchPhase.Ended && touchOrigin.x >= 0)
				{
					//Set touchEnd to equal the position of this touch
					Vector2 touchEnd = myTouch.position;
					
					//Calculate the difference between the beginning and end of the touch on the x axis.
					float x = touchEnd.x - touchOrigin.x;
					
					//Calculate the difference between the beginning and end of the touch on the y axis.
					float y = touchEnd.y - touchOrigin.y;
					
					//Set touchOrigin.x to -1 so that our else if statement will evaluate false and not repeat immediately.
					touchOrigin.x = -1;
					
					//Check if the difference along the x axis is greater than the difference along the y axis.
					if (Mathf.Abs(x) > Mathf.Abs(y))
						//If x is greater than zero, set horizontal to 1, otherwise set it to -1
						horizontal = x > 0 ? 1 : -1;
					else
						//If y is greater than zero, set horizontal to 1, otherwise set it to -1
						vertical = y > 0 ? 1 : -1;
				}
			}
			
#endif //End of mobile platform dependendent compilation section started above with #elif

            if (horizontal == 0 && vertical == 0)
            {
                discreteActionsOut[0] = 0;
            }
            else if (horizontal < 0)
            {
                discreteActionsOut[0] = 1;
            }
            else if (horizontal > 0)
            {
                discreteActionsOut[0] = 2;
            }
            else if (vertical < 0)
            {
                discreteActionsOut[0] = 3;
            }
            else if (vertical > 0)
            {
                discreteActionsOut[0] = 4;
            }

            discreteActionsOut[0] = discreteActionsOut[0] - 1; // TODO: Remove this line if zero movement is allowed
        }
    }
}
