using UnityEngine;
using System.Collections;

public class MenuModeChanger : StateMachineBehaviour {

	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	//override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
	//override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        animator.gameObject.GetComponent<PressStart>().mode = animator.GetInteger("mode");
        ParticleSystem[] poof = animator.gameObject.GetComponentsInChildren<ParticleSystem>();
        foreach (var puff in poof)
        {
            Debug.Log("Puff");
            puff.Play();
        }
        animator.SetBool("flipped", !animator.GetBool("flipped"));
		//UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject (GameObject.FindGameObjectWithTag ("UI Element"));
        //anim.SetInteger("mode", menuNumber);
    }

	// OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
	//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}
}
