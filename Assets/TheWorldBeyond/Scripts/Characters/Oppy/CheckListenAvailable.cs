// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace TheWorldBeyond.Character.Oppy
{
    public class CheckListenAvailable : StateMachineBehaviour
    {
        [SerializeField]
        private VirtualPet m_pet;
        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        //override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    if (m_pet == null) m_pet = animator.gameObject.GetComponent<VirtualPet>();
        //    if (m_pet != null) m_pet.EnterIdleMode();
        //}

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    if (m_pet == null) m_pet = animator.gameObject.GetComponent<VirtualPet>();
        //    if (m_pet != null) m_pet.EnterIdleMode();
        //}

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (m_pet == null) m_pet = animator.gameObject.GetComponent<VirtualPet>();
            if (m_pet != null) m_pet.CheckListenAvailable();
        }

        // OnStateMove is called right after Animator.OnAnimatorMove()
        //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    // Implement code that processes and affects root motion
        //}

        // OnStateIK is called right after Animator.OnAnimatorIK()
        //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    // Implement code that sets up animation IK (inverse kinematics)
        //}
    }
}
