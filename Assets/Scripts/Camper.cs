using Items;
using Unity.MLAgents;
using UnityEngine;

namespace Entities
{
    public class Camper : Agent
    {
        #region Vars
        [SerializeField] private float objectivePickupReward;
        [SerializeField] private float objectiveDropOffReward;
        [SerializeField] private float deathReward;
        #endregion

        #region Item holding
        private Objective heldItem;

        public Objective HeldItem => heldItem;

        public void AddItem(Objective item)
        {
            heldItem = item;
            AddReward(objectivePickupReward);
        }

        public void RemoveItem(bool success)
        {
            if (success)
                AddReward(objectiveDropOffReward);

            heldItem = null;
        }

        public void GetKilled()
        {
            AddReward(deathReward);
            RemoveItem(false);
            transform.DetachChildren();
            Destroy(this);
        }
        #endregion

        #region Agent code

        #endregion
    }
}
